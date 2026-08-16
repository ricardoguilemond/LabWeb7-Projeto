using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.Controllers
{
    /// <summary>
    /// Tela de Manutenção de Faturamento — busca exames liberados e não baixados,
    /// permite edição inline de ValorItem em ItensExamesRealizados.
    /// Inclui mecanismo de segurança via flag Faturado (acionado pelo Relatório de Faturamento).
    /// </summary>
    public class ManutencaoFaturamentoController : BaseController
    {
        public ManutencaoFaturamentoController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ManutencaoFaturamento")]
        public async Task<IActionResult> Index()
        {
            var instituicoes = await _db.Instituicao
                .AsNoTracking()
                .Where(i => !string.IsNullOrEmpty(i.Sigla))
                .OrderBy(i => i.Sigla)
                .Select(i => new SelectListItem
                {
                    Value = i.Sigla,
                    Text = $"{i.Sigla} - {i.Nome}"
                })
                .ToListAsync();

            var model = new vmManutencaoFaturamento
            {
                Instituicoes = instituicoes
            };

            ViewBag.TextoMenu = new object[] { "Manutenção de Faturamento", false };
            return View(model);
        }

        /// <summary>
        /// Busca exame liberado e não baixado, retorna dados + flag Faturado + itens.
        /// Campos de busca: instituicao + sequencial OU codigoExame (mutuamente exclusivos).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ManutencaoFaturamento/BuscarExame")]
        public async Task<IActionResult> BuscarExame(string? instituicao, string? sequencial, int? codigoExame)
        {
            try
            {
                // Validação: pelo menos um critério de busca deve ser informado
                bool buscaPorSequencial = !string.IsNullOrEmpty(instituicao) && !string.IsNullOrEmpty(sequencial);
                bool buscaPorCodigo = codigoExame.HasValue && codigoExame.Value > 0;

                if (!buscaPorSequencial && !buscaPorCodigo)
                    return Json(new { sucesso = false, mensagem = "Informe Instituição + Sequencial ou Código do Exame." });

                var query = _db.ExamesRealizados
                    .AsNoTracking()
                    .Include(e => e.Pacientes)
                    .Include(e => e.Instituicao)
                    .Include(e => e.TabelaExames)
                    .Include(e => e.Medicos)
                    .Where(e => e.Situacao >= 1 && e.Liberacao == 1 && e.Baixado != 1)
                    .AsQueryable();

                if (buscaPorCodigo)
                {
                    query = query.Where(e => e.Id == codigoExame!.Value);
                }
                else
                {
                    query = query.Where(e => e.Instituicao!.Sigla == instituicao
                                          && e.Sequencial.ToString() == sequencial);
                }

                var exame = await query.FirstOrDefaultAsync();

                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado ou não atende aos critérios (liberado e não baixado)." });

                // Carregar itens do exame
                var itens = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => i.ExameRealizadoId == exame.Id)
                    .OrderBy(i => i.OrdemItem)
                    .Select(i => new
                    {
                        i.Id,
                        i.RefItem,
                        i.Descricao,
                        i.RefExame,
                        i.OrdemItem,
                        i.ContaExame,
                        ValorItem = i.ValorItem.HasValue ? i.ValorItem.Value.ToString("N2") : "0,00",
                        EhFolha = i.ContaExame.Length >= 7 && i.ContaExame.Substring(i.ContaExame.Length - 7) == "0000000",
                        EhPrincipal = i.ContaExame.Length >= 4 && i.ContaExame.Substring(i.ContaExame.Length - 4) == "0000"
                    })
                    .ToListAsync();

                // Calcular idade/nascimento formatado
                string nascimentoIdade = "";
                if (exame.Pacientes?.Nascimento != null)
                {
                    var dataNasc = exame.Pacientes.Nascimento;
                    int idade = DateTime.Now.Year - dataNasc.Year;
                    if (dataNasc > DateTime.Now.AddYears(-idade)) idade--;
                    nascimentoIdade = $"{dataNasc:dd/MM/yyyy} ({idade} anos)";
                }

                return Json(new
                {
                    sucesso = true,
                    exame = new
                    {
                        codigoExameResultado = exame.Id,
                        codigoCliente = exame.PacienteId,
                        nomeCliente = exame.Pacientes?.NomePaciente ?? "",
                        cpf = exame.Pacientes?.CPF ?? "",
                        nascimentoIdade,
                        instituicaoId = exame.InstituicaoId,
                        instituicaoSigla = exame.Instituicao?.Sigla ?? "",
                        instituicaoNome = exame.Instituicao?.Nome ?? "",
                        siglaTabela = exame.TabelaExames?.SiglaTabela ?? "",
                        tabelaNome = exame.TabelaExames?.NomeTabela ?? "",
                        sequencialFormatado = exame.Sequencial.ToString(),
                        crm = exame.Medicos?.CRM ?? "",
                        medicoNome = exame.Medicos?.NomeMedico ?? "",
                        dataExame = exame.DataExame?.ToString("dd/MM/yyyy") ?? "",
                        faturado = exame.Faturado,
                        emCatalogoRecebimentos = exame.EmCatalogoRecebimentos
                    },
                    itens
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ManutencaoFaturamento] BuscarExame - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao buscar exame." });
            }
        }

        /// <summary>
        /// Atualiza ValorItem de um item de exame.
        /// Rejeita a operação se o exame estiver com flag Faturado=true.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ManutencaoFaturamento/EditarValorItem")]
        public async Task<IActionResult> EditarValorItem(int codigo, decimal valorItem)
        {
            try
            {
                // Localizar o item
                var item = await _db.ItensExamesRealizados
                    .FirstOrDefaultAsync(i => i.Id == codigo);

                if (item == null)
                    return Json(new { sucesso = false, mensagem = "Item não encontrado." });

                // Verificar flag Faturado do exame pai
                var exame = await _db.ExamesRealizados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == item.ExameRealizadoId && e.Situacao >= 1);

                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado." });

                if (exame.Faturado)
                    return Json(new { sucesso = false, mensagem = "Exame faturado. Desmarque o flag Faturado antes de editar valores." });

                if (exame.EmCatalogoRecebimentos)
                    return Json(new { sucesso = false, mensagem = "Exame consta no Catálogo de Recebimentos. Não é permitido alterar valores." });

                item.ValorItem = valorItem;
                await _db.SaveChangesAsync();

                _eventLogHelper.LogEventViewer(
                    $"[ManutencaoFaturamento] EditarValorItem - Item Id={codigo}, ExameRealizadoId={item.ExameRealizadoId}, NovoValor={valorItem:N4}",
                    "wInformation");

                return Json(new { sucesso = true, mensagem = "Valor atualizado com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ManutencaoFaturamento] EditarValorItem - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao atualizar valor do item." });
            }
        }

        /// <summary>
        /// Altera o flag Faturado via checkbox da tela de Manutenção.
        /// Rejeita se o exame estiver baixado (arquivo-morto).
        /// Ao desmarcar (faturado=false), registra log de auditoria.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ManutencaoFaturamento/AlterarFlagFaturado")]
        public async Task<IActionResult> AlterarFlagFaturado(int codigoExame, bool faturado)
        {
            try
            {
                var exame = await _db.ExamesRealizados
                    .FirstOrDefaultAsync(e => e.Id == codigoExame && e.Situacao >= 1);

                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado." });

                // Arquivo-morto é definitivo: não permite alteração do flag
                if (exame.Baixado == 1)
                    return Json(new { sucesso = false, mensagem = "Exame está no arquivo-morto. Flag não pode ser alterado." });

                bool anterior = exame.Faturado;
                exame.Faturado = faturado;
                await _db.SaveChangesAsync();

                // Log de auditoria quando desbloquear (faturado: true → false)
                if (anterior && !faturado)
                {
                    _eventLogHelper.LogEventViewer(
                        $"[ManutencaoFaturamento] DESBLOQUEIO DE EXAME FATURADO - CodigoExame={codigoExame}, Sequencial={exame.Sequencial}, Data/Hora={DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                        "wWarning");
                }

                return Json(new { sucesso = true, faturado = exame.Faturado });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ManutencaoFaturamento] AlterarFlagFaturado - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao alterar flag." });
            }
        }

        /// <summary>
        /// Retorna itens do exame para DataTables server-side.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ManutencaoFaturamento/CarregarItens")]
        public async Task<IActionResult> CarregarItens(int codigoExame)
        {
            try
            {
                var itens = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => i.ExameRealizadoId == codigoExame)
                    .OrderBy(i => i.OrdemItem)
                    .Select(i => new
                    {
                        i.Id,
                        i.RefItem,
                        i.Descricao,
                        i.RefExame,
                        i.OrdemItem,
                        i.ContaExame,
                        ValorItem = i.ValorItem.HasValue ? i.ValorItem.Value.ToString("N2") : "0,00",
                        EhFolha = i.ContaExame.Length >= 7 && i.ContaExame.Substring(i.ContaExame.Length - 7) == "0000000",
                        EhPrincipal = i.ContaExame.Length >= 4 && i.ContaExame.Substring(i.ContaExame.Length - 4) == "0000"
                    })
                    .ToListAsync();

                return Json(new { sucesso = true, itens });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ManutencaoFaturamento] CarregarItens - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, itens = new List<object>() });
            }
        }
    }
}
