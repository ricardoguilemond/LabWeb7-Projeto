using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    //Feito pelo Kiro em 06/06/2026
    public class ResultadoExamesController : BaseController
    {
        public ResultadoExamesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        { }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ResultadoExames")]
        public async Task<IActionResult> Index(
            string? dataInicial,
            string? dataFinal,
            string? nomePaciente,
            int? codigoExame,
            string? siglaInstituicao)
        {
            var query = _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.Postos)
                .Include(e => e.Pacientes)
                .Include(e => e.Medicos)
                .Include(e => e.TabelaExames)
                .Where(e => e.Liberacao == 0 && e.Baixado == 0)
                .AsQueryable();

            bool temFiltro = !string.IsNullOrEmpty(dataInicial)
                          || !string.IsNullOrEmpty(dataFinal)
                          || !string.IsNullOrEmpty(nomePaciente)
                          || codigoExame.HasValue
                          || !string.IsNullOrEmpty(siglaInstituicao);

            // Filtros backend
            if (!string.IsNullOrEmpty(dataInicial))
            {
                DateTime dataParsed = dataInicial.Trim().FormataData("dd/MM/yyyy", true);
                var (inicioUtc, _) = _geralController.ConverterDataLocalParaRangeUtc(dataParsed);
                query = query.Where(e => e.DataIni >= inicioUtc);
            }

            if (!string.IsNullOrEmpty(dataFinal))
            {
                DateTime dataParsed = dataFinal.Trim().FormataData("dd/MM/yyyy", true);
                var (_, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataParsed);
                query = query.Where(e => e.DataIni <= fimUtc);
            }

            if (!string.IsNullOrEmpty(nomePaciente))
                query = query.Where(e => e.Pacientes.NomePaciente
                    .ToLower().Contains(nomePaciente.Trim().ToLower()));

            if (codigoExame.HasValue)
                query = query.Where(e => e.Id == codigoExame.Value);

            if (!string.IsNullOrEmpty(siglaInstituicao))
                query = query.Where(e => e.Instituicao.Sigla
                    .ToLower().Contains(siglaInstituicao.Trim().ToLower()));

            // Sem filtros: limitar a 100 registros
            if (!temFiltro)
                query = query.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id).Take(100);
            else
                query = query.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id);

            var dados = await query.ToListAsync();

            ICollection<dynamic> listaGrid = [];
            foreach (var item in dados)
            {
                listaGrid.Add(new
                {
                    Id = item.Id,
                    NomePaciente = item.Pacientes?.NomePaciente ?? "",
                    SiglaInstituicao = item.Instituicao?.Sigla ?? "",
                    SiglaTabela = item.TabelaExames?.SiglaTabela ?? "",
                    NomePosto = item.Postos != null
                        ? (item.Postos.SiglaPosto ?? "") + "-" + (item.Postos.NomePosto ?? "")
                        : "",
                    Sequencial = item.Sequencial,
                    DataFim = item.DataFim,
                    NomeMedico = item.Medicos?.NomeMedico ?? "",
                    CRM = item.Medicos?.CRM ?? "",
                    Situacao = item.Situacao
                });
            }

            ViewBag.TextoMenu = new object[] { "Resultado de Exames", false };
            ViewBag.TotalRegistros = listaGrid.Count.ToString();
            ViewBag.TotalTabela = (await _db.ExamesRealizados.Where(e => e.Liberacao == 0 && e.Baixado == 0).CountAsync()).ToString();
            ViewBag.ListaDados = listaGrid.Cast<dynamic>().ToList();

            return View();
        }

        //Feito pelo Kiro em 06/06/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ResultadoExames/ObterItensExame")]
        public async Task<IActionResult> ObterItensExame(int exameRealizadoId)
        {
            try
            {
                // Buscar header do exame para painel informativo
                var exame = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Include(e => e.Pacientes)
                    .Include(e => e.Medicos)
                    .Include(e => e.Instituicao)
                    .Include(e => e.Postos)
                    .Include(e => e.TabelaExames)
                    .Where(e => e.Id == exameRealizadoId)
                    .FirstOrDefaultAsync();

                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado." });

                // Buscar itens ordenados por ContaExame
                // Filtro: exclui Folha geral (posições 5-11 = "0000000") — mesmo filtro do Delphi
                var itens = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Include(i => i.ClasseExames)
                    .Where(i => i.ExameRealizadoId == exameRealizadoId
                             && i.ContaExame.Substring(4, 7) != "0000000")
                    .OrderBy(i => i.ContaExame)
                    .Select(i => new
                    {
                        i.Id,
                        Folha = i.ClasseExames != null ? i.ClasseExames.RefExame : "",
                        i.ContaExame,
                        i.Descricao,
                        i.Resultado,
                        i.UnidadeMedida,
                        i.Referencia,
                        // Principal: últimos 4 dígitos = "0000" E posições 5-7 > "000"
                        EhPrincipal = i.ContaExame.Substring(i.ContaExame.Length - 4) == "0000"
                                   && i.ContaExame.Substring(4, 3) != "000"
                    })
                    .ToListAsync();

                // Montar info do paciente para o painel
                var info = new
                {
                    ExameId = exame.Id,
                    NomePaciente = exame.Pacientes?.NomePaciente ?? "",
                    PacienteId = exame.PacienteId,
                    Nascimento = exame.Pacientes?.Nascimento.ToLocalString("dd/MM/yyyy") ?? "",
                    CPF = exame.Pacientes?.CPF ?? "",
                    NomeMedico = exame.Medicos?.NomeMedico ?? "",
                    CRM = exame.Medicos?.CRM ?? "",
                    SiglaInstituicao = exame.Instituicao?.Sigla ?? "",
                    NomeInstituicao = exame.Instituicao?.Nome ?? "",
                    SiglaTabela = exame.TabelaExames?.SiglaTabela ?? "",
                    NomePosto = exame.Postos != null
                        ? (exame.Postos.SiglaPosto ?? "") + "-" + (exame.Postos.NomePosto ?? "")
                        : "",
                    Sequencial = exame.Sequencial,
                    DataIni = exame.DataIni.ToLocalString("dd/MM/yyyy"),
                    DataFim = exame.DataFim?.ToLocalString("dd/MM/yyyy") ?? "",
                    Situacao = exame.Situacao,
                    TotalImpresso = exame.TotalImpresso
                };

                return Json(new { sucesso = true, info, itens });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ResultadoExames] ObterItensExame - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao obter itens do exame." });
            }
        }
        //..Kiro

        //Feito pelo Kiro em 06/06/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ResultadoExames/SalvarResultado")]
        public async Task<IActionResult> SalvarResultado(int itemId, string? resultado)
        {
            try
            {
                var item = await _db.ItensExamesRealizados.FindAsync(itemId);
                if (item == null)
                    return Json(new { sucesso = false, mensagem = "Item não encontrado." });

                item.Resultado = resultado?.Trim();
                
                // Melhoria sobre Delphi: marcar "Em Análise" (Situacao=1) ao primeiro lançamento
                if (!string.IsNullOrEmpty(resultado))
                {
                    var exame = await _db.ExamesRealizados.FindAsync(item.ExameRealizadoId);
                    if (exame != null && exame.Situacao == 0)
                    {
                        exame.Situacao = 1; // Em Análise
                    }
                }

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Resultado salvo." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ResultadoExames] SalvarResultado - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar resultado." });
            }
        }
        //..Kiro
    }
    //..Kiro
}
