using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Impressoras;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Globalization;
using System.Text;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    //Feito pelo Qoder em 23/08/2026
    // Controller único dos Mapas de Trabalho (portabilidade do sistema Delphi).
    // Fase 1: Mapa Eletrônico (FProducao.pas) — lançamento de resultados em tela
    // única por conta de exame + impressão térmica da Lista de Coletas (40 colunas).
    public class MapasTrabalhoController : BaseController
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGeralService _geralService;

        public MapasTrabalhoController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IServiceProvider serviceProvider,
            IGeralService geralService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _serviceProvider = serviceProvider;
            _geralService = geralService;
        }

        #region Mapa Eletrônico

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/MapaEletronico")]
        public IActionResult MapaEletronico()
        {
            ViewBag.TextoMenu = new object[] { "Mapa Eletrônico", false };
            return View();
        }

        /// <summary>
        /// Monta a lista de contas de exame pendentes do Mapa Eletrônico.
        /// Regras portadas do FProducao.AbreExamesRealizados (Delphi) e adaptadas
        /// ao fluxo atual em 23/08/2026: no Delphi antigo a Liberação (Liberacao=1)
        /// ocorria ANTES dos resultados (entrega do exame à rotina técnica); no fluxo
        /// .NET atual a Liberação ocorre DEPOIS (fecha o ciclo p/ faturamento).
        /// Como os mapas são ferramenta de produção, loteiam o trabalho em andamento:
        /// - somente exames NÃO liberados (Liberacao=0) e não baixados no período;
        /// - somente itens de folhas com ClasseExames.TipoMapa = 'E';
        /// - somente subitens (conta principal '0000' excluída) sem resultado;
        /// - deduplicação pelos últimos 9 caracteres da ContaExame;
        /// - conta principal só é removida se existirem subitens no mesmo grupo.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ObterItensMapaEletronico")]
        public async Task<IActionResult> ObterItensMapaEletronico(string? dataInicial, string? dataFinal)
        {
            try
            {
                if (!TryParsePeriodo(dataInicial, dataFinal, out var dataIni, out var dataFim))
                    return Json(new { sucesso = false, mensagem = "Informe o período em dd/MM/yyyy (data final maior ou igual à inicial)." });

                var idsClassesEletronicas = await _db.ClasseExames
                    .AsNoTracking()
                    .Where(c => c.TipoMapa == "E")
                    .Select(c => c.Id)
                    .ToListAsync();

                if (idsClassesEletronicas.Count == 0)
                    return Json(new { sucesso = true, itens = Array.Empty<object>(), mensagem = "Nenhuma folha de exames está configurada com TipoMapa 'E' (Mapa Eletrônico)." });

                var idsExames = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.Liberacao == 0
                             && e.Baixado != 1
                             && e.DataIni >= dataIni
                             && e.DataIni <= dataFim)
                    .Select(e => e.Id)
                    .ToListAsync();

                if (idsExames.Count == 0)
                    return Json(new { sucesso = true, itens = Array.Empty<object>(), mensagem = "Nenhum exame pendente (não liberado) no período informado." });

                // Projeção enxuta: a deduplicação é feita em memória, como no Delphi.
                var brutos = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => idsExames.Contains(i.ExameRealizadoId)
                             && idsClassesEletronicas.Contains(i.ClasseExamesId)
                             && i.Liberado == 0
                             && i.Baixado != 1
                             && i.Descricao != null
                             && i.Descricao.ToUpper() != "."
                             && i.ContaExame.Substring(7, 4) != "0000"
                             && (i.Resultado == null || i.Resultado == ""))
                    .OrderBy(i => i.ControleApoio)
                    .ThenBy(i => i.ContaExame)
                    .Select(i => new { i.ControleApoio, i.ContaExame, i.Descricao, i.RefExame })
                    .ToListAsync();

                var porConta = brutos
                    .GroupBy(b => b.ContaExame.Length >= 9 ? b.ContaExame.Substring(b.ContaExame.Length - 9) : b.ContaExame)
                    .Select(g => new { ChaveConta = g.Key, Primeiro = g.First(), Contagem = g.Count() })
                    .ToList();

                // Conta principal (terminada em 0000) permanece somente quando não há subitens no grupo.
                var itens = porConta
                    .Where(x => !x.ChaveConta.EndsWith("0000") || x.Contagem == 1)
                    .Select(x => new
                    {
                        contaChave = x.ChaveConta,
                        contaMascara = FormatarContaMascarada(x.ChaveConta),
                        descricao = x.Primeiro.Descricao,
                        refExame = x.Primeiro.RefExame,
                        pendentes = x.Contagem
                    })
                    .OrderBy(x => x.descricao)
                    .ToList();

                return Json(new { sucesso = true, itens, mensagem = itens.Count == 0 ? "Não encontrei Exames que façam parte do Mapa Eletrônico!" : "" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ObterItensMapaEletronico - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao montar o Mapa Eletrônico." });
            }
        }

        /// <summary>
        /// Itens pendentes de resultado para lançamento (FProducao.AbreResultados):
        /// ContaExame = '11' + conta de 9 posições, período pelo ControleApoio e Resultado nulo.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ObterResultadosPendentes")]
        public async Task<IActionResult> ObterResultadosPendentes(string contaChave, string? dataInicial, string? dataFinal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contaChave))
                    return Json(new { sucesso = false, mensagem = "Conta de exame não informada." });

                if (!TryParsePeriodo(dataInicial, dataFinal, out var dataIni, out var dataFim))
                    return Json(new { sucesso = false, mensagem = "Período inválido." });

                string contaExame = "11" + contaChave;
                string controleIni = dataIni.ToString("yyyyMMdd");
                string controleFim = dataFim.ToString("yyyyMMdd");

                var itens = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => i.ContaExame == contaExame
                             && i.ControleApoio != null
                             && i.ControleApoio != ""
                             && i.Descricao != null
                             && i.Descricao.ToUpper() != "."
                             && i.Liberado == 0
                             && i.Baixado != 1
                             && i.Resultado == null
                             && i.ControleApoio.CompareTo(controleIni) >= 0
                             && i.ControleApoio.CompareTo(controleFim) <= 0)
                    .OrderBy(i => i.ControleApoio)
                    .Select(i => new
                    {
                        id = i.Id,
                        coleta = i.ControleApoio,
                        pacienteId = i.PacienteId,
                        nomePaciente = i.Pacientes.NomePaciente,
                        descricao = i.Descricao,
                        unidadeMedida = i.UnidadeMedida,
                        referencia = i.Referencia
                    })
                    .ToListAsync();

                return Json(new { sucesso = true, itens, total = itens.Count });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ObterResultadosPendentes - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao obter os itens pendentes." });
            }
        }

        /// <summary>
        /// Grava o resultado digitado no Mapa Eletrônico, replicando as validações
        /// de mínimo/máximo do Plano de Exames e o ajuste de situação do exame.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/SalvarResultadoMapa")]
        public async Task<IActionResult> SalvarResultadoMapa(int itemId, string? resultado)
        {
            try
            {
                var item = await _db.ItensExamesRealizados.FindAsync(itemId);
                if (item == null)
                    return Json(new { sucesso = false, mensagem = "Item não encontrado." });

                var exame = await _db.ExamesRealizados.FindAsync(item.ExameRealizadoId);

                // Validação de intervalo numérico configurado no Plano de Exames.
                if (exame != null && !string.IsNullOrWhiteSpace(resultado))
                {
                    var plano = await _db.PlanoExames
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ContaExame == item.ContaExame
                                               && p.TabelaExamesId == exame.TabelaExamesId);

                    if (plano != null && (plano.ResultadoMinimo.HasValue || plano.ResultadoMaximo.HasValue))
                    {
                        var valorResultado = ParseValorResultado(resultado);
                        if (valorResultado.HasValue)
                        {
                            if (plano.ResultadoMinimo.HasValue && valorResultado.Value < plano.ResultadoMinimo.Value)
                                return Json(new { sucesso = false, mensagem = $"O resultado {resultado} está abaixo do mínimo permitido ({plano.ResultadoMinimo.Value})." });

                            if (plano.ResultadoMaximo.HasValue && valorResultado.Value > plano.ResultadoMaximo.Value)
                                return Json(new { sucesso = false, mensagem = $"O resultado {resultado} está acima do máximo permitido ({plano.ResultadoMaximo.Value})." });
                        }
                    }
                }

                item.Resultado = string.IsNullOrWhiteSpace(resultado) ? null : resultado.Trim();

                if (exame != null)
                {
                    if (!string.IsNullOrWhiteSpace(resultado))
                    {
                        if (exame.Situacao == 0)
                            exame.Situacao = 1; // Em Análise
                    }
                    else
                    {
                        if (exame.Situacao == 3)
                            exame.Situacao = 1; // resultado apagado após impressão
                    }
                }

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Resultado salvo." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] SalvarResultadoMapa - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar resultado." });
            }
        }

        /// <summary>
        /// Impressão térmica (40 colunas) da Lista de Coletas da conta selecionada.
        /// Portaria do FProducao.spdImprimeListaClick: linhas AAAA.MM.DD-NNNN com o
        /// resultado ou 25 sublinhados quando vazio. Saída via ServicoImpressaoCupom.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ImprimirListaColetas")]
        public IActionResult ImprimirListaColetas(string contaChave, string? dataInicial, string? dataFinal)
        {
            if (string.IsNullOrWhiteSpace(contaChave))
                return Json(new { titulo = "Erro", mensagem = "Conta de exame não informada.", sucesso = false });

            if (!TryParsePeriodo(dataInicial, dataFinal, out var dataIni, out var dataFim))
                return Json(new { titulo = "Erro", mensagem = "Período inválido para a impressão.", sucesso = false });

            string contaExame = "11" + contaChave;
            string controleIni = dataIni.ToString("yyyyMMdd");
            string controleFim = dataFim.ToString("yyyyMMdd");

            var itens = _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ContaExame == contaExame
                         && i.ControleApoio != null
                         && i.ControleApoio != ""
                         && i.Descricao != null
                         && i.Descricao.ToUpper() != "."
                         && i.Liberado == 0
                         && i.Baixado != 1
                         && i.ControleApoio.CompareTo(controleIni) >= 0
                         && i.ControleApoio.CompareTo(controleFim) <= 0)
                .OrderBy(i => i.ControleApoio)
                .Select(i => new { i.ControleApoio, i.Resultado, i.RefExame, i.Descricao })
                .ToList();

            if (itens.Count == 0)
                return Json(new { titulo = "Aviso", mensagem = "Não há itens pendentes (não liberados) para esta conta no período informado.", sucesso = false });

            var empresa = _db.Empresa.FirstOrDefault();
            string nomeLaboratorioTitulo = empresa?.TituloEmpresa ?? "LABORATÓRIO";

            var primeiro = itens.First();

            var sb = new StringBuilder();
            AppendTextoQuebrado(sb, nomeLaboratorioTitulo);
            AppendTextoQuebrado(sb, "-");
            AppendTextoQuebrado(sb, "NOME DA FOLHA E ITEM DE EXAME:");
            AppendTextoQuebrado(sb, (primeiro.RefExame ?? "").Trim());
            AppendTextoQuebrado(sb, (primeiro.Descricao ?? "").Trim());
            AppendTextoQuebrado(sb, "-");
            sb.AppendLine("");

            foreach (var item in itens)
            {
                string controle = item.ControleApoio ?? "";
                string dataFormatada = controle.Length >= 8
                    ? $"{controle.Substring(0, 4)}.{controle.Substring(4, 2)}.{controle.Substring(6, 2)}-{(controle.Length >= 12 ? controle.Substring(8, 4) : controle.Substring(8))}"
                    : controle;

                if (string.IsNullOrWhiteSpace(item.Resultado))
                    sb.AppendLine(dataFormatada + new string('_', 25));
                else
                    sb.AppendLine(dataFormatada + ": " + item.Resultado.Trim());
            }

            AppendTextoQuebrado(sb, "-");
            sb.AppendLine("");
            sb.AppendLine("");
            sb.AppendLine("");

            try
            {
                var servico = ActivatorUtilities.CreateInstance<ServicoImpressaoCupom>(_serviceProvider, sb.ToString(), _db);
                var resultado = servico.Executar(contaChave);
                return Json(new
                {
                    titulo = resultado.Sucesso ? "Sucesso" : "Erro",
                    mensagem = resultado.Mensagem,
                    sucesso = resultado.Sucesso
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ImprimirListaColetas - Erro: " + ex.Message, "wError");
                return Json(new { titulo = "Erro", mensagem = "Erro ao imprimir a Lista de Coletas: " + ex.Message, sucesso = false });
            }
        }
        //..Qoder

        #endregion

        #region Mapa Planilhado

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/MapaPlanilhado")]
        public IActionResult MapaPlanilhado()
        {
            ViewBag.TextoMenu = new object[] { "Mapa Planilhado (Excel)", false };
            return View();
        }

        /// <summary>
        /// Folhas de exame disponíveis para marcação (ClasseExames), com o estado
        /// atual do marcador "Planilha". A "FOLHA EM BRANCO" fica de fora (padrão Delphi).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ObterFolhasPlanilha")]
        public async Task<IActionResult> ObterFolhasPlanilha()
        {
            try
            {
                var folhas = await _db.ClasseExames
                    .AsNoTracking()
                    .Where(c => c.RefExame != "FOLHA EM BRANCO")
                    .OrderBy(c => c.Id)
                    .Select(c => new { id = c.Id, refExame = c.RefExame, planilha = c.Planilha })
                    .ToListAsync();

                return Json(new { sucesso = true, folhas });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ObterFolhasPlanilha - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao listar as folhas de exame." });
            }
        }

        /// <summary>
        /// Liga/desliga o marcador "Planilha" da folha (equivale ao Marcador do dbgFolhas do Delphi).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/AlternarPlanilha")]
        public async Task<IActionResult> AlternarPlanilha(int classeExamesId)
        {
            try
            {
                var classe = await _db.ClasseExames.FindAsync(classeExamesId);
                if (classe == null)
                    return Json(new { sucesso = false, mensagem = "Folha de exame não encontrada." });

                classe.Planilha = classe.Planilha == 1 ? 0 : 1;
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, planilha = classe.Planilha });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] AlternarPlanilha - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao alternar o marcador." });
            }
        }

        /// <summary>
        /// Gera um novo lote do Mapa Planilhado (portabilidade do FMapaExcel.Gera_Mapa):
        /// - limpeza dos lotes com mais de 30 dias (SinalizaLotesAnteriores + ZeraFicha);
        /// - somente exames NÃO liberados (Liberacao=0, trabalho em andamento no fluxo
        ///   atual) e não baixados da data informada;
        /// - deduplicação por ITEM (par ExamesRealizadosId+ContaExame): um exame já loteado
        ///   pode voltar em lote seguinte com os itens de outras folhas (Lote 1 = Bioquímica,
        ///   Lote 2 = Hemograma dos mesmos pacientes), mas o mesmo item nunca entra em 2 lotes;
        /// - subitens (conta principal excluída) das folhas marcadas com Planilha = 1;
        ///   se o exame só tiver a conta principal (final '0000'), as colunas são sintetizadas
        ///   dos itens técnicos do catálogo PlanoExames (S1, validada com o usuário);
        /// - Lote = MAX(Lote) + 1 da data.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/GerarLotePlanilhado")]
        public async Task<IActionResult> GerarLotePlanilhado(string? data, bool somenteSemResultados = true)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data do mapa em dd/MM/yyyy." });

                var idsFolhasMarcadas = await _db.ClasseExames
                    .Where(c => c.Planilha == 1)
                    .Select(c => c.Id)
                    .ToListAsync();

                if (idsFolhasMarcadas.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Pelo menos um item de Exame deve ser marcado." });

                // Limpeza: sinaliza (LiberadoExclusao = 'S') e apaga fichas com mais de 30 dias.
                var limiteLimpeza = dataExame.AddDays(-30);
                var antigas = await _db.FichasPlanilhas
                    .Where(f => f.DataExame <= limiteLimpeza)
                    .ToListAsync();

                if (antigas.Count > 0)
                {
                    foreach (var antiga in antigas)
                        antiga.LiberadoExclusao = "S";

                    var sinalizadas = await _db.FichasPlanilhas
                        .Where(f => f.LiberadoExclusao == "S")
                        .ToListAsync();
                    _db.FichasPlanilhas.RemoveRange(sinalizadas);
                    await _db.SaveChangesAsync();
                }

                //Feito pelo Qoder em 23/08/2026 — deduplicação por ITEM (no Delphi era por exame
                // inteiro, o que impedia um 2º lote com outra folha dos mesmos pacientes):
                // um exame já loteado volta em lote seguinte com os itens ainda não loteados.
                var paresJaLoteados = await _db.FichasPlanilhas
                    .Select(f => new { f.ExamesRealizadosId, f.ContaExame })
                    .Distinct()
                    .ToListAsync();
                var chavesJaLoteadas = new HashSet<string>(
                    paresJaLoteados.Select(p => p.ExamesRealizadosId + "|" + p.ContaExame));

                //Feito pelo Qoder em 23/08/2026 — S1: cabeçalhos do dia em memória para poder
                // sintetizar colunas a partir do catálogo (folhas só com conta principal).
                var examesDoDia = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.Liberacao == 0
                             && e.Baixado != 1
                             && e.DataIni == dataExame)
                    .ToListAsync();

                if (examesDoDia.Count == 0)
                    return Json(new { sucesso = false, mensagem = "No momento não há Exames disponíveis para serem loteados em novos Mapas Planilhados." });

                var idsExamesDoDia = examesDoDia.Select(e => e.Id).ToList();

                //Feito pelo Qoder em 23/08/2026 — o filtro de resultados saiu da query e virou
                // etapa em memória: assim, quando não houver nada a lotear, a mensagem indica o
                // MOTIVO (itens já loteados vs. itens já com resultados) em vez do aviso genérico.
                var itensBrutos = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => idsExamesDoDia.Contains(i.ExameRealizadoId)
                             && idsFolhasMarcadas.Contains(i.ClasseExamesId))
                    .OrderBy(i => i.ClasseExamesId)
                    .ThenBy(i => i.ExameRealizadoId)
                    .ThenBy(i => i.ContaExame)
                    .Select(i => new
                    {
                        i.Id,
                        i.ClasseExamesId,
                        i.ContaExame,
                        i.Descricao,
                        i.Resultado,
                        i.RefExame,
                        i.ControleApoio,
                        i.PacienteId,
                        i.TabelaExamesId,
                        i.ExameRealizadoId,
                        Exame = i.ExamesRealizados
                    })
                    .ToListAsync();

                //Feito pelo Qoder em 23/08/2026 — S1 (validada com o usuário): subitens reais da
                // folha viram colunas (regra Delphi, final <> '0000'); quando o exame só tem a
                // conta principal (HEMOGRAMA COMPLETO da carga de dados, 11170000000), as
                // colunas são sintetizadas dos itens técnicos do catálogo PlanoExames (final
                // <> '0000' e NaoMostrar = 0 — os constituintes de Eritrograma, Leucograma e
                // Plaquetas), pois o mapa é a folha de trabalho para anotar resultados que
                // ainda não existem. Sem técnicos no catálogo, a principal vira coluna única.
                var grupos = itensBrutos
                    .GroupBy(i => new { i.ExameRealizadoId, i.ClasseExamesId })
                    .ToList();

                var itensDaFolha = grupos
                    .Where(g => g.Any(i => !EhContaPrincipal(i.ContaExame)))
                    .SelectMany(g => g.Where(i => !EhContaPrincipal(i.ContaExame)))
                    .ToList();

                var somentePrincipal = grupos
                    .Where(g => g.All(i => EhContaPrincipal(i.ContaExame)))
                    .ToList();

                if (somentePrincipal.Count > 0)
                {
                    var examesPorId = examesDoDia.ToDictionary(e => e.Id);
                    var classesSemSubitens = somentePrincipal
                        .Select(g => g.Key.ClasseExamesId).Distinct().ToList();

                    var tecnicosPorPar = (await _db.PlanoExames
                        .AsNoTracking()
                        .Where(p => classesSemSubitens.Contains(p.ClasseExamesId) && p.NaoMostrar == 0)
                        .OrderBy(p => p.ContaExame)
                        .ToListAsync())
                        .Where(p => !EhContaPrincipal(p.ContaExame))
                        .GroupBy(p => new { p.ClasseExamesId, p.TabelaExamesId })
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var g in somentePrincipal)
                    {
                        var exame = examesPorId[g.Key.ExameRealizadoId];

                        if (!tecnicosPorPar.TryGetValue(new { ClasseExamesId = g.Key.ClasseExamesId, TabelaExamesId = exame.TabelaExamesId }, out var catalogo))
                        {
                            itensDaFolha.AddRange(g); // fallback: coluna única
                            continue;
                        }

                        foreach (var p in catalogo)
                        {
                            itensDaFolha.Add(new
                            {
                                Id = 0,
                                ClasseExamesId = p.ClasseExamesId,
                                ContaExame = p.ContaExame,
                                Descricao = (string?)p.Descricao,
                                Resultado = (string?)null,
                                RefExame = p.RefExame,
                                ControleApoio = (string?)null,
                                PacienteId = exame.PacienteId,
                                TabelaExamesId = exame.TabelaExamesId,
                                ExameRealizadoId = exame.Id,
                                Exame = exame
                            });
                        }
                    }
                }

                // Exclui apenas os itens já loteados (pelos pares acima), não o exame inteiro.
                var itensNaoLoteados = itensDaFolha
                    .Where(i => !chavesJaLoteadas.Contains(i.ExameRealizadoId + "|" + i.ContaExame))
                    .ToList();

                if (itensNaoLoteados.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Todos os itens das folhas marcadas neste dia já foram loteados em Mapas Planilhados anteriores." });

                var itens = somenteSemResultados
                    ? itensNaoLoteados.Where(i => string.IsNullOrEmpty(i.Resultado)).ToList()
                    : itensNaoLoteados;

                if (itens.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Os itens das folhas marcadas já estão com resultados lançados. Em 'Resultados', escolha 'Todos (com e sem)' e gere o lote novamente." });

                // Abreviações MapaHorizontal do Plano de Exames (por conta + tabela).
                var contas = itens.Select(i => i.ContaExame).Distinct().ToList();
                var planos = await _db.PlanoExames
                    .AsNoTracking()
                    .Where(p => contas.Contains(p.ContaExame) && p.MapaHorizontal != null)
                    .Select(p => new { p.ContaExame, p.TabelaExamesId, p.MapaHorizontal })
                    .ToListAsync();

                var mapaPorConta = planos
                    .GroupBy(p => new { p.ContaExame, p.TabelaExamesId })
                    .ToDictionary(g => (g.Key.ContaExame, g.Key.TabelaExamesId), g => g.First().MapaHorizontal);

                int lote = (await _db.FichasPlanilhas
                    .Where(f => f.DataExame == dataExame)
                    .MaxAsync(f => (int?)f.Lote)) ?? 0;
                lote++;

                foreach (var item in itens)
                {
                    mapaPorConta.TryGetValue((item.ContaExame, item.TabelaExamesId), out var abreviacao);

                    _db.FichasPlanilhas.Add(new FichasPlanilhas
                    {
                        NomeFicha = item.RefExame,
                        ContaExame = item.ContaExame,
                        Descricao = item.Descricao,
                        Resultado = item.Resultado,
                        MapaHorizontal = abreviacao,
                        ExamesRealizadosId = item.ExameRealizadoId,
                        PacienteId = item.PacienteId,
                        MedicoId = item.Exame.MedicoId,
                        InstituicaoId = item.Exame.InstituicaoId,
                        TabelaExamesId = item.TabelaExamesId,
                        DataExame = item.Exame.DataIni,
                        //Feito pelo Qoder em 23/08/2026 — fallback no cabeçalho: itens antigos da
                        // carga de dados têm ControleApoio vazio; sem ele o XLSX agrupa todos os
                        // pacientes numa única linha "=" (o Delphi exporta uma linha por ControleApoio).
                        ControleApoio = string.IsNullOrEmpty(item.ControleApoio) ? item.Exame.ControleApoio : item.ControleApoio,
                        Sequencial = item.Exame.Sequencial,
                        HistoricoClinico = item.Exame.HistoricoClinico,
                        DataIni = item.Exame.DataIni,
                        DataFim = item.Exame.DataFim,
                        Lote = lote,
                        LiberadoExclusao = "N"
                    });
                }

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, lote, total = itens.Count, mensagem = "OK: Lote do Mapa foi gerado. Escolha os itens para a Planilha..." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] GerarLotePlanilhado - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao gerar o lote do Mapa Planilhado." });
            }
        }

        /// <summary>
        /// Enumera os lotes existentes para a data (BoxLote do Delphi).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ListarLotesPlanilha")]
        public async Task<IActionResult> ListarLotesPlanilha(string? data)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data em dd/MM/yyyy." });

                var lotes = await _db.FichasPlanilhas
                    .AsNoTracking()
                    .Where(f => f.DataExame == dataExame)
                    .Select(f => f.Lote)
                    .Distinct()
                    .OrderByDescending(l => l)
                    .ToListAsync();

                return Json(new { sucesso = true, lotes });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ListarLotesPlanilha - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao listar os lotes." });
            }
        }

        /// <summary>
        /// Espelho do lote: descrições distintas para escolha das colunas da planilha.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ObterEspelhoLotePlanilha")]
        public async Task<IActionResult> ObterEspelhoLotePlanilha(string? data, int lote)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data em dd/MM/yyyy." });

                var espelho = await _db.FichasPlanilhas
                    .AsNoTracking()
                    .Where(f => f.DataIni == dataExame && f.Lote == lote)
                    .GroupBy(f => f.Descricao)
                    .Select(g => new
                    {
                        descricao = g.Key,
                        abreviacao = g.Select(f => f.MapaHorizontal).FirstOrDefault(),
                        quantidade = g.Count()
                    })
                    .OrderBy(x => x.descricao)
                    .ToListAsync();

                return Json(new { sucesso = true, itens = espelho });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ObterEspelhoLotePlanilha - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao montar o espelho do lote." });
            }
        }

        /// <summary>
        /// Exporta o lote para XLSX (portabilidade do Exportar_Excel).
        /// Recebe as descrições escolhidas separadas por "|"; se vazio, exporta todas.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ExportarMapaPlanilhado")]
        public async Task<IActionResult> ExportarMapaPlanilhado(string? data, int lote, string? descricoes)
        {
            if (!TryParseDataUnica(data, out var dataExame))
                return BadRequest("Data do mapa inválida.");

            var fichas = await _db.FichasPlanilhas
                .AsNoTracking()
                .Where(f => f.DataIni == dataExame && f.Lote == lote)
                .OrderBy(f => f.ControleApoio)
                .ThenBy(f => f.Descricao)
                .ToListAsync();

            if (fichas.Count == 0)
                return NotFound("Nenhuma ficha encontrada para este lote.");

            //Feito pelo Qoder em 23/08/2026 — snapshots gerados antes do fallback podem estar
            // com ControleApoio vazio (itens antigos); resolve pelo cabeçalho do exame para que
            // cada paciente/coleta volte a ter sua linha "NNNN=" no XLSX, como no Delphi.
            var semControle = fichas
                .Where(f => string.IsNullOrEmpty(f.ControleApoio))
                .Select(f => f.ExamesRealizadosId)
                .Distinct()
                .ToList();
            if (semControle.Count > 0)
            {
                var controlesCabecalho = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => semControle.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id, e => e.ControleApoio ?? "");
                foreach (var f in fichas)
                    if (string.IsNullOrEmpty(f.ControleApoio) && controlesCabecalho.TryGetValue(f.ExamesRealizadosId, out var controle))
                        f.ControleApoio = controle;
            }

            //Feito pelo Qoder em 23/08/2026 — último nível de identificação: se nem o item
            // nem o cabeçalho tiverem o código (dados degradados), usa o Sequencial do exame
            // para que nenhuma linha de paciente desapareça da planilha — no Delphi de
            // produção o código sempre existia (copiado do cabeçalho na liberação).
            foreach (var f in fichas)
                if (string.IsNullOrEmpty(f.ControleApoio))
                    f.ControleApoio = f.Sequencial.ToString().PadLeft(4, '0');

            var listaDescricoes = string.IsNullOrWhiteSpace(descricoes)
                ? fichas.Select(f => f.Descricao ?? "").Distinct().OrderBy(d => d).ToList()
                : descricoes.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToList();

            var empresa = _db.Empresa.FirstOrDefault();
            string tituloEmpresa = empresa?.TituloEmpresa ?? "LABORATÓRIO";

            var gerador = new GeradorXlsxMapaPlanilhado();
            byte[] arquivo = gerador.Gerar(fichas, listaDescricoes, tituloEmpresa, dataExame, lote);

            string nomeArquivo = $"MapaPlanilhado_{dataExame:yyyy-MM-dd}_Lote{lote}.xlsx";
            return File(arquivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeArquivo);
        }

        /// <summary>
        /// Elimina um lote do Mapa Planilhado (FichasPlanilhas por DataIni + Lote).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ExcluirLotePlanilha")]
        public async Task<IActionResult> ExcluirLotePlanilha(string? data, int lote)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data em dd/MM/yyyy." });

                var fichas = await _db.FichasPlanilhas
                    .Where(f => f.DataIni == dataExame && f.Lote == lote)
                    .ToListAsync();

                if (fichas.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Nenhuma ficha encontrada para este lote." });

                _db.FichasPlanilhas.RemoveRange(fichas);
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Lote foi Eliminado!" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ExcluirLotePlanilha - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Não foi possível Deletar o Lote neste momento." });
            }
        }
        //..Qoder

        #endregion

        #region Mapa Agrupado

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/MapaAgrupado")]
        public IActionResult MapaAgrupado()
        {
            ViewBag.TextoMenu = new object[] { "Mapa Agrupado", false };
            return View();
        }

        /// <summary>
        /// Folhas de exame com o estado do marcador "Marcado" — compartilhado pelos
        /// Mapas Agrupado, Horizontal e Meia-Folha (mesma coluna persistida do Delphi).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ObterFolhasAgrupadas")]
        public async Task<IActionResult> ObterFolhasAgrupadas()
        {
            try
            {
                var folhas = await _db.ClasseExames
                    .AsNoTracking()
                    .Where(c => c.RefExame != "FOLHA EM BRANCO")
                    .OrderBy(c => c.Id)
                    .Select(c => new { id = c.Id, refExame = c.RefExame, marcado = c.Marcado })
                    .ToListAsync();

                return Json(new { sucesso = true, folhas });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ObterFolhasAgrupadas - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao listar as folhas de exame." });
            }
        }

        /// <summary>
        /// Liga/desliga o marcador "Marcado" da folha (Mapas Agrupado, Horizontal e Meia-Folha).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/AlternarMarcado")]
        public async Task<IActionResult> AlternarMarcado(int classeExamesId)
        {
            try
            {
                var classe = await _db.ClasseExames.FindAsync(classeExamesId);
                if (classe == null)
                    return Json(new { sucesso = false, mensagem = "Folha de exame não encontrada." });

                classe.Marcado = classe.Marcado == 1 ? 0 : 1;
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, marcado = classe.Marcado });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] AlternarMarcado - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao alternar o marcador." });
            }
        }

        /// <summary>
        /// Gera um novo lote do Mapa Agrupado (portabilidade do FFichaAgrupada.Gera_Mapa):
        /// mesmas regras do Planilhado — deduplicação por ITEM (par ExamesRealizadosId+
        /// ContaExame), marcador Marcado — e destino FichasLotes.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/GerarLoteAgrupado")]
        public async Task<IActionResult> GerarLoteAgrupado(string? data, bool somenteSemResultados = true)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data do mapa em dd/MM/yyyy." });

                var idsFolhasMarcadas = await _db.ClasseExames
                    .Where(c => c.Marcado == 1)
                    .Select(c => c.Id)
                    .ToListAsync();

                if (idsFolhasMarcadas.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Pelo menos um item de Exame deve ser marcado." });

                // Limpeza: sinaliza e apaga fichas com mais de 30 dias.
                var limiteLimpeza = dataExame.AddDays(-30);
                var antigas = await _db.FichasLotes
                    .Where(f => f.DataExame <= limiteLimpeza)
                    .ToListAsync();

                if (antigas.Count > 0)
                {
                    foreach (var antiga in antigas)
                        antiga.LiberadoExclusao = "S";

                    var sinalizadas = await _db.FichasLotes
                        .Where(f => f.LiberadoExclusao == "S")
                        .ToListAsync();
                    _db.FichasLotes.RemoveRange(sinalizadas);
                    await _db.SaveChangesAsync();
                }

                //Feito pelo Qoder em 23/08/2026 — deduplicação por ITEM (mesma regra do
                // Planilhado): um exame já loteado volta em lote seguinte com os itens ainda
                // não loteados (Lote 1 = Bioquímica, Lote 2 = Hemograma dos mesmos pacientes).
                var paresJaLoteados = await _db.FichasLotes
                    .Select(f => new { f.ExamesRealizadosId, f.ContaExame })
                    .Distinct()
                    .ToListAsync();
                var chavesJaLoteadas = new HashSet<string>(
                    paresJaLoteados.Select(p => p.ExamesRealizadosId + "|" + p.ContaExame));

                //Feito pelo Qoder em 23/08/2026 — S1: cabeçalhos do dia em memória (mesma regra
                // do Planilhado) para sintetizar colunas a partir do catálogo.
                var examesDoDia = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.Liberacao == 0
                             && e.Baixado != 1
                             && e.DataIni == dataExame)
                    .ToListAsync();

                if (examesDoDia.Count == 0)
                    return Json(new { sucesso = false, mensagem = "No momento não há Exames disponíveis para serem loteados em novos Mapas Agrupados." });

                var idsExamesDoDia = examesDoDia.Select(e => e.Id).ToList();

                var itensBrutos = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => idsExamesDoDia.Contains(i.ExameRealizadoId)
                             && idsFolhasMarcadas.Contains(i.ClasseExamesId))
                    .OrderBy(i => i.ClasseExamesId)
                    .ThenBy(i => i.ExameRealizadoId)
                    .ThenBy(i => i.ContaExame)
                    .Select(i => new
                    {
                        i.ClasseExamesId,
                        i.ContaExame,
                        i.Descricao,
                        i.Resultado,
                        i.RefExame,
                        i.ControleApoio,
                        i.PacienteId,
                        i.TabelaExamesId,
                        i.ExameRealizadoId,
                        Exame = i.ExamesRealizados
                    })
                    .ToListAsync();

                //Feito pelo Qoder em 23/08/2026 — S1 (mesma regra do Planilhado): subitens reais
                // viram colunas; exame só com conta principal recebe colunas sintetizadas dos
                // itens técnicos do catálogo PlanoExames (final <> '0000' e NaoMostrar = 0).
                var grupos = itensBrutos
                    .GroupBy(i => new { i.ExameRealizadoId, i.ClasseExamesId })
                    .ToList();

                var itensDaFolha = grupos
                    .Where(g => g.Any(i => !EhContaPrincipal(i.ContaExame)))
                    .SelectMany(g => g.Where(i => !EhContaPrincipal(i.ContaExame)))
                    .ToList();

                var somentePrincipal = grupos
                    .Where(g => g.All(i => EhContaPrincipal(i.ContaExame)))
                    .ToList();

                if (somentePrincipal.Count > 0)
                {
                    var examesPorId = examesDoDia.ToDictionary(e => e.Id);
                    var classesSemSubitens = somentePrincipal
                        .Select(g => g.Key.ClasseExamesId).Distinct().ToList();

                    var tecnicosPorPar = (await _db.PlanoExames
                        .AsNoTracking()
                        .Where(p => classesSemSubitens.Contains(p.ClasseExamesId) && p.NaoMostrar == 0)
                        .OrderBy(p => p.ContaExame)
                        .ToListAsync())
                        .Where(p => !EhContaPrincipal(p.ContaExame))
                        .GroupBy(p => new { p.ClasseExamesId, p.TabelaExamesId })
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var g in somentePrincipal)
                    {
                        var exame = examesPorId[g.Key.ExameRealizadoId];

                        if (!tecnicosPorPar.TryGetValue(new { ClasseExamesId = g.Key.ClasseExamesId, TabelaExamesId = exame.TabelaExamesId }, out var catalogo))
                        {
                            itensDaFolha.AddRange(g); // fallback: coluna única
                            continue;
                        }

                        foreach (var p in catalogo)
                        {
                            itensDaFolha.Add(new
                            {
                                ClasseExamesId = p.ClasseExamesId,
                                ContaExame = p.ContaExame,
                                Descricao = (string?)p.Descricao,
                                Resultado = (string?)null,
                                RefExame = p.RefExame,
                                ControleApoio = (string?)null,
                                PacienteId = exame.PacienteId,
                                TabelaExamesId = exame.TabelaExamesId,
                                ExameRealizadoId = exame.Id,
                                Exame = exame
                            });
                        }
                    }
                }

                var itensNaoLoteados = itensDaFolha
                    .Where(i => !chavesJaLoteadas.Contains(i.ExameRealizadoId + "|" + i.ContaExame))
                    .ToList();

                if (itensNaoLoteados.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Todos os itens das folhas marcadas neste dia já foram loteados em Mapas Agrupados anteriores." });

                var itens = somenteSemResultados
                    ? itensNaoLoteados.Where(i => string.IsNullOrEmpty(i.Resultado)).ToList()
                    : itensNaoLoteados;

                if (itens.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Os itens das folhas marcadas já estão com resultados lançados. Em 'Resultados', escolha 'Todos (com e sem)' e gere o lote novamente." });

                var contas = itens.Select(i => i.ContaExame).Distinct().ToList();
                var planos = await _db.PlanoExames
                    .AsNoTracking()
                    .Where(p => contas.Contains(p.ContaExame) && p.MapaHorizontal != null)
                    .Select(p => new { p.ContaExame, p.TabelaExamesId, p.MapaHorizontal })
                    .ToListAsync();

                var mapaPorConta = planos
                    .GroupBy(p => new { p.ContaExame, p.TabelaExamesId })
                    .ToDictionary(g => (g.Key.ContaExame, g.Key.TabelaExamesId), g => g.First().MapaHorizontal);

                int lote = (await _db.FichasLotes
                    .Where(f => f.DataExame == dataExame)
                    .MaxAsync(f => (int?)f.Lote)) ?? 0;
                lote++;

                foreach (var item in itens)
                {
                    mapaPorConta.TryGetValue((item.ContaExame, item.TabelaExamesId), out var abreviacao);

                    _db.FichasLotes.Add(new FichasLotes
                    {
                        NomeFicha = item.RefExame,
                        ContaExame = item.ContaExame,
                        Descricao = item.Descricao,
                        Resultado = item.Resultado,
                        MapaHorizontal = abreviacao,
                        ExamesRealizadosId = item.ExameRealizadoId,
                        PacienteId = item.PacienteId,
                        MedicoId = item.Exame.MedicoId,
                        InstituicaoId = item.Exame.InstituicaoId,
                        TabelaExamesId = item.TabelaExamesId,
                        DataExame = item.Exame.DataIni,
                        //Feito pelo Qoder em 23/08/2026 — fallback no cabeçalho (mesma regra do Planilhado).
                        ControleApoio = string.IsNullOrEmpty(item.ControleApoio) ? item.Exame.ControleApoio : item.ControleApoio,
                        Sequencial = item.Exame.Sequencial,
                        HistoricoClinico = item.Exame.HistoricoClinico,
                        DataIni = item.Exame.DataIni,
                        DataFim = item.Exame.DataFim,
                        Lote = lote,
                        LiberadoExclusao = "N"
                    });
                }

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, lote, total = itens.Count, mensagem = "OK: Lote do Mapa foi gerado - pode Imprimir!" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] GerarLoteAgrupado - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao gerar o lote do Mapa Agrupado." });
            }
        }

        /// <summary>
        /// Enumera os lotes agrupados existentes para a data.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ListarLotesAgrupados")]
        public async Task<IActionResult> ListarLotesAgrupados(string? data)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data em dd/MM/yyyy." });

                var lotes = await _db.FichasLotes
                    .AsNoTracking()
                    .Where(f => f.DataExame == dataExame)
                    .Select(f => f.Lote)
                    .Distinct()
                    .OrderByDescending(l => l)
                    .ToListAsync();

                return Json(new { sucesso = true, lotes });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ListarLotesAgrupados - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao listar os lotes." });
            }
        }

        /// <summary>
        /// Elimina um lote do Mapa Agrupado (FichasLotes por DataIni + Lote).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ExcluirLoteAgrupado")]
        public async Task<IActionResult> ExcluirLoteAgrupado(string? data, int lote)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data em dd/MM/yyyy." });

                var fichas = await _db.FichasLotes
                    .Where(f => f.DataIni == dataExame && f.Lote == lote)
                    .ToListAsync();

                if (fichas.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Nenhuma ficha encontrada para este lote." });

                _db.FichasLotes.RemoveRange(fichas);
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Lote foi Eliminado!" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ExcluirLoteAgrupado - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Não foi possível Deletar o Lote neste momento." });
            }
        }

        /// <summary>
        /// Imprime (PDF) o Mapa Agrupado do lote (portabilidade do FRelMapaAgrupado):
        /// uma seção por folha (NomeFicha); quando há mais de uma folha, os PDFs são
        /// juntados num único documento. O parâmetro nomeFolha restringe a uma folha.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ImprimirMapaAgrupado")]
        public async Task<IActionResult> ImprimirMapaAgrupado(string? data, int lote, string? nomeFolha)
        {
            if (!TryParseDataUnica(data, out var dataExame))
                return BadRequest("Data do mapa inválida.");

            var fichas = await _db.FichasLotes
                .AsNoTracking()
                .Where(f => f.DataIni == dataExame && f.Lote == lote)
                .OrderBy(f => f.NomeFicha)
                .ThenBy(f => f.ExamesRealizadosId)
                .ThenBy(f => f.ContaExame)
                .Select(f => new
                {
                    f.NomeFicha,
                    f.ExamesRealizadosId,
                    f.ContaExame,
                    f.Descricao,
                    f.MapaHorizontal,
                    //Feito pelo Qoder em 23/08/2026 — fallback no cabeçalho para snapshots antigos.
                    ControleApoio = string.IsNullOrEmpty(f.ControleApoio) ? f.ExamesRealizados.ControleApoio : f.ControleApoio,
                    f.HistoricoClinico,
                    f.Sequencial,
                    PacienteId = f.PacienteId,
                    NomePaciente = f.Pacientes.NomePaciente,
                    Nascimento = f.Pacientes.Nascimento,
                    Sexo = f.Pacientes.Sexo,
                    NomeMedico = f.Medicos.NomeMedico,
                    CRM = f.Medicos.CRM,
                    SiglaInstituicao = f.Instituicao.Sigla,
                    SiglaTabela = f.TabelaExames.SiglaTabela
                })
                .ToListAsync();

            if (fichas.Count == 0)
                return NotFound("Nenhuma ficha encontrada para este lote.");

            if (!string.IsNullOrWhiteSpace(nomeFolha))
                fichas = fichas.Where(f => f.NomeFicha == nomeFolha).ToList();

            if (fichas.Count == 0)
                return NotFound("Nenhuma ficha encontrada para esta folha.");

            // Linha "Exames:" — todos os itens dos exames do lote (pela tabela SUS, como no Delphi).
            var idsExames = fichas.Select(f => f.ExamesRealizadosId).Distinct().ToList();
            var itensTodos = await _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => idsExames.Contains(i.ExameRealizadoId)
                         && i.ContaExame.Substring(7, 4) != "0000")
                .OrderBy(i => i.ExameRealizadoId)
                .ThenBy(i => i.ContaExame)
                .Select(i => new { i.ExameRealizadoId, i.ContaExame, i.Descricao })
                .ToListAsync();

            var contasTodos = itensTodos.Select(i => i.ContaExame).Distinct().ToList();
            var idsTabelaSus = await _db.TabelaExames
                .AsNoTracking()
                .Where(t => t.SiglaTabela == "SUS")
                .Select(t => t.Id)
                .ToListAsync();

            var abreviacoesSus = await _db.PlanoExames
                .AsNoTracking()
                .Where(p => contasTodos.Contains(p.ContaExame)
                         && idsTabelaSus.Contains(p.TabelaExamesId)
                         && p.MapaHorizontal != null)
                .Select(p => new { p.ContaExame, p.MapaHorizontal })
                .ToListAsync();

            var abrevPorConta = abreviacoesSus
                .GroupBy(p => p.ContaExame)
                .ToDictionary(g => g.Key, g => g.First().MapaHorizontal);

            var examesPorExame = itensTodos
                .GroupBy(i => i.ExameRealizadoId)
                .ToDictionary(g => g.Key, g => g.Select(i => RotuloItemAgrupado(
                    abrevPorConta.TryGetValue(i.ContaExame, out var abrev) ? abrev : null, i.Descricao)).ToList());

            var empresa = _db.Empresa.FirstOrDefault();
            var gerador = new GeradorPdfMapaAgrupado();

            var folhasDoLote = fichas.Select(f => f.NomeFicha ?? "").Distinct().OrderBy(n => n).ToList();
            var pdfs = new List<byte[]>();

            foreach (var folha in folhasDoLote)
            {
                var exames = fichas
                    .Where(f => f.NomeFicha == folha)
                    .GroupBy(f => f.ExamesRealizadosId)
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        var primeira = g.First();
                        return new ExameMapaAgrupadoDto
                        {
                            ExameRealizadoId = g.Key,
                            PacienteId = primeira.PacienteId,
                            NomePaciente = primeira.NomePaciente,
                            Nascimento = primeira.Nascimento,
                            Sexo = primeira.Sexo,
                            SiglaInstituicao = primeira.SiglaInstituicao,
                            Sequencial = primeira.Sequencial,
                            SiglaTabela = primeira.SiglaTabela,
                            NomeMedico = primeira.NomeMedico,
                            CRM = primeira.CRM,
                            ControleApoio = primeira.ControleApoio,
                            HistoricoClinico = primeira.HistoricoClinico,
                            Itens = g.Select(f => RotuloItemAgrupado(f.MapaHorizontal, f.Descricao)).ToList(),
                            ExamesDoPaciente = examesPorExame.TryGetValue(g.Key, out var lista) ? lista : new List<string>()
                        };
                    })
                    .ToList();

                pdfs.Add(gerador.Gerar(folha, lote, dataExame, exames, empresa));
            }

            byte[] arquivo;
            if (pdfs.Count == 1)
            {
                arquivo = pdfs[0];
            }
            else
            {
                // Junta as seções (uma por folha) num único PDF.
                using var saida = new PdfDocument();
                foreach (var pdf in pdfs)
                {
                    using var entrada = PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import);
                    for (int i = 0; i < entrada.PageCount; i++)
                        saida.AddPage(entrada.Pages[i]);
                }
                using var streamSaida = new MemoryStream();
                saida.Save(streamSaida, false);
                arquivo = streamSaida.ToArray();
            }

            string nomeArquivo = $"MapaAgrupado_{dataExame:yyyy-MM-dd}_Lote{lote}.pdf";
            return File(arquivo, "application/pdf", nomeArquivo);
        }
        //..Qoder

        #endregion

        #region Mapa Horizontal

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/MapaHorizontal")]
        public IActionResult MapaHorizontal()
        {
            ViewBag.TextoMenu = new object[] { "Mapa Horizontal", false };
            return View();
        }

        /// <summary>
        /// Gera o Mapa Horizontal na tabela efêmera FichasInternas (portabilidade do
        /// FFichaHorizontal.Gera_Mapa): ZeraFicha (DELETE total), regra hematológica
        /// (HEMOGRAMA→HEMATOLOGIA/HEMO, ERITROGRAMA→ERITR, LEUCOGRAMA→LEUCO com conta
        /// da HEMATOLOGIA via PlanoExames SUS), MHI incorporando itens de outras folhas
        /// e abreviação MapaHorizontal do PlanoExames (tabela SUS).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/GerarMapaHorizontal")]
        public async Task<IActionResult> GerarMapaHorizontal(string? data, bool somenteSemResultados = true)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data do mapa em dd/MM/yyyy." });

                var idsFolhasMarcadas = await _db.ClasseExames
                    .Where(c => c.Marcado == 1 && c.RefExame != "FOLHA EM BRANCO")
                    .Select(c => c.Id)
                    .ToListAsync();

                if (idsFolhasMarcadas.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Pelo menos um item de Exame deve ser marcado." });

                var idsExamesDoDia = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.Liberacao == 0 && e.Baixado != 1 && e.DataIni == dataExame)
                    .Select(e => e.Id)
                    .ToListAsync();

                if (idsExamesDoDia.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Não há exames pendentes (não liberados) na data informada. Verifique!" });

                // Ordem do Delphi: CodigoCliente, ContaExame, CodigoExame.
                var itens = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => idsExamesDoDia.Contains(i.ExameRealizadoId)
                             && idsFolhasMarcadas.Contains(i.ClasseExamesId)
                             && i.ContaExame.Substring(7, 4) != "0000"
                             && (!somenteSemResultados || i.Resultado == null || i.Resultado == ""))
                    .OrderBy(i => i.PacienteId)
                    .ThenBy(i => i.ContaExame)
                    .ThenBy(i => i.ExameRealizadoId)
                    .Select(i => new
                    {
                        i.Id,
                        i.ContaExame,
                        i.Descricao,
                        i.RefExame,
                        i.ControleApoio,
                        i.PacienteId,
                        i.ExameRealizadoId,
                        i.Sequencial,
                        Exame = i.ExamesRealizados
                    })
                    .ToListAsync();

                if (itens.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Não há exames pendentes (não liberados) na data informada. Verifique!" });

                // Abreviações MapaHorizontal do PlanoExames padrão (tabela SUS).
                var idsTabelaSus = await _db.TabelaExames
                    .Where(t => t.SiglaTabela == "SUS")
                    .Select(t => t.Id)
                    .ToListAsync();

                var contas = itens.Select(i => i.ContaExame).Distinct().ToList();
                var abrevPorConta = await _db.PlanoExames
                    .AsNoTracking()
                    .Where(p => idsTabelaSus.Contains(p.TabelaExamesId) && contas.Contains(p.ContaExame))
                    .GroupBy(p => p.ContaExame)
                    .ToDictionaryAsync(g => g.Key, g => g.First().MapaHorizontal);

                // Conta principal da HEMATOLOGIA (Retorna_ContaExame do FRotinas).
                string contaHematologia = await _db.PlanoExames
                    .AsNoTracking()
                    .Where(p => idsTabelaSus.Contains(p.TabelaExamesId) && p.Descricao == "HEMATOLOGIA")
                    .Select(p => p.ContaExame)
                    .FirstOrDefaultAsync() ?? "";

                var classes = await _db.ClasseExames.AsNoTracking().ToListAsync();

                // ZeraFicha: FichasInternas é efêmera — apaga tudo antes de regenerar.
                var existentes = await _db.FichasInternas.ToListAsync();
                if (existentes.Count > 0)
                    _db.FichasInternas.RemoveRange(existentes);

                var novas = new List<FichasInternas>();

                for (int idx = 0; idx < itens.Count; idx++)
                {
                    var item = itens[idx];
                    string refExame = item.RefExame.Trim();

                    // Regra hematológica: não transcreve os itens (as máquinas já emitem
                    // tickets); lança apenas um registro marcador na folha correspondente.
                    if (refExame == "HEMOGRAMA" || refExame == "HEMOGRAMA COMPLETO"
                        || refExame == "ERITROGRAMA" || refExame == "LEUCOGRAMA")
                    {
                        string nomeFicha = refExame == "HEMOGRAMA" || refExame == "HEMOGRAMA COMPLETO" ? "HEMATOLOGIA" : refExame;
                        string descricao = refExame == "HEMOGRAMA" || refExame == "HEMOGRAMA COMPLETO" ? "HEMO"
                            : refExame == "ERITROGRAMA" ? "ERITR" : "LEUCO";

                        novas.Add(MontarFichaInterna(nomeFicha, contaHematologia, descricao, null, item.Exame, item.PacienteId, dataExame));

                        // Pula os demais itens consecutivos da mesma folha hematológica.
                        while (idx + 1 < itens.Count && itens[idx + 1].RefExame.Trim() == refExame)
                            idx++;

                        continue;
                    }

                    // MHI (Mapa Horizontal Informado): incorpora itens de outras folhas.
                    bool processouMhi = false;
                    if (item.ContaExame.Length >= 4
                        && int.TryParse(item.ContaExame.Substring(2, 2), out int codigoClasse))
                    {
                        var classeAtual = classes.FirstOrDefault(c => c.Id == codigoClasse);
                        if (classeAtual != null && classeAtual.MHI != codigoClasse)
                        {
                            var folhaMhi = classes.FirstOrDefault(c => c.Id == classeAtual.MHI);
                            if (folhaMhi != null)
                            {
                                abrevPorConta.TryGetValue(item.ContaExame, out var abrevMhi);
                                string descricaoMhi = !string.IsNullOrWhiteSpace(abrevMhi) && abrevMhi != "0"
                                    ? abrevMhi : item.Descricao ?? "";

                                novas.Add(MontarFichaInterna(folhaMhi.RefExame ?? "", item.ContaExame, descricaoMhi, abrevMhi, item.Exame, item.PacienteId, dataExame));
                                processouMhi = true;
                            }
                        }
                    }

                    if (processouMhi) continue;

                    abrevPorConta.TryGetValue(item.ContaExame, out var abrev);
                    string descricaoNormal = !string.IsNullOrWhiteSpace(abrev) && abrev != "0"
                        ? abrev : item.Descricao ?? "";

                    novas.Add(MontarFichaInterna(item.RefExame, item.ContaExame, descricaoNormal, abrev, item.Exame, item.PacienteId, dataExame));
                }

                if (novas.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Não há exames pendentes (não liberados) na data informada. Verifique!" });

                _db.FichasInternas.AddRange(novas);
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, total = novas.Count, mensagem = "OK: Mapa Gerado - Escolha seu Modelo e Imprima..." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] GerarMapaHorizontal - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao gerar o Mapa Horizontal." });
            }
        }

        /// <summary>
        /// Imprime o Mapa Horizontal em PDF paisagem (portabilidade do FRelMapaHorizontal).
        /// ordem: 0 = Código do Exame, 1 = Controle de Coleta, 2 = Sequencial Instituição.
        /// modelo: 1 = paginado (folha por página), 2 = não paginado.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ImprimirMapaHorizontal")]
        public async Task<IActionResult> ImprimirMapaHorizontal(string? data, int ordem = 0, int modelo = 1)
        {
            if (!TryParseDataUnica(data, out var dataExame))
                return BadRequest("Data do mapa inválida.");

            var registros = await _db.FichasInternas.AsNoTracking().ToListAsync();
            if (registros.Count == 0)
                return NotFound("Mapa não está gerado. Gere o Mapa Horizontal antes de imprimir.");

            IEnumerable<FichasInternas> ordenados = ordem switch
            {
                1 => registros.OrderBy(f => f.NomeFicha).ThenBy(f => f.ControleApoio).ThenBy(f => f.ContaExame),
                2 => registros.OrderBy(f => f.NomeFicha).ThenBy(f => f.InstituicaoId).ThenBy(f => f.Sequencial).ThenBy(f => f.ContaExame),
                _ => registros.OrderBy(f => f.NomeFicha).ThenBy(f => f.ExamesRealizadosId).ThenBy(f => f.ContaExame)
            };

            var idsPacientes = registros.Select(f => f.PacienteId).Distinct().ToList();
            var pacientes = await _db.Pacientes.AsNoTracking()
                .Where(p => idsPacientes.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var idsInstituicoes = registros.Select(f => f.InstituicaoId).Distinct().ToList();
            var siglasInstituicao = await _db.Instituicao.AsNoTracking()
                .Where(i => idsInstituicoes.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Sigla);

            var fichas = ordenados
                .GroupBy(f => new { f.NomeFicha, f.ExamesRealizadosId })
                .Select(g =>
                {
                    var primeira = g.First();
                    return new FichaMapaHorizontalDto
                    {
                        NomeFicha = primeira.NomeFicha ?? "",
                        ExamesRealizadosId = g.Key.ExamesRealizadosId,
                        ControleApoio = primeira.ControleApoio,
                        SiglaInstituicao = siglasInstituicao.TryGetValue(primeira.InstituicaoId, out var sigla) ? sigla : "",
                        Sequencial = primeira.Sequencial,
                        NomePaciente = pacientes.TryGetValue(primeira.PacienteId, out var pac) ? pac.NomePaciente : "",
                        Nascimento = pacientes.TryGetValue(primeira.PacienteId, out var pacNasc) ? pacNasc.Nascimento : DateTime.MinValue,
                        Descricoes = g.Select(f => f.Descricao ?? "").ToList()
                    };
                })
                .ToList();

            var empresa = _db.Empresa.FirstOrDefault();
            var gerador = new GeradorPdfMapaHorizontal();
            byte[] arquivo = gerador.Gerar(fichas, modelo, dataExame, empresa);

            string nomeArquivo = $"MapaHorizontal_{dataExame:yyyy-MM-dd}.pdf";
            return File(arquivo, "application/pdf", nomeArquivo);
        }
        //..Qoder

        #endregion

        #region Mapa Meia-Folha

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/MapaMeiaFolha")]
        public IActionResult MapaMeiaFolha()
        {
            ViewBag.TextoMenu = new object[] { "Mapa Meia-Folha", false };
            return View();
        }

        /// <summary>
        /// Gera o Mapa Meia-Folha em FichasInternas (portabilidade do FFichaInterna.Gera_Mapa):
        /// inclui contas principais e resultados, paginação por exame novo / mais de 48 itens /
        /// por folha (paginadoPorFolha) e remoção final das fichas com somente a conta principal
        /// solitária (itens já com resultados).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/GerarMapaMeiaFolha")]
        public async Task<IActionResult> GerarMapaMeiaFolha(string? data, bool somenteSemResultados = true, int ordem = 0, bool paginadoPorFolha = false)
        {
            try
            {
                if (!TryParseDataUnica(data, out var dataExame))
                    return Json(new { sucesso = false, mensagem = "Informe a data do mapa em dd/MM/yyyy." });

                var idsFolhasMarcadas = await _db.ClasseExames
                    .Where(c => c.Marcado == 1 && c.RefExame != "FOLHA EM BRANCO")
                    .Select(c => c.Id)
                    .ToListAsync();

                if (idsFolhasMarcadas.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Pelo menos um item de Exame deve ser marcado." });

                var idsExamesDoDia = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.Liberacao == 0 && e.Baixado != 1 && e.DataIni == dataExame)
                    .Select(e => e.Id)
                    .ToListAsync();

                if (idsExamesDoDia.Count == 0)
                    return Json(new { sucesso = false, mensagem = "Não há exames pendentes (não liberados) na data informada. Verifique!" });

                // Meia-Folha inclui as contas principais (ao contrário dos demais mapas).
                var itens = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => idsExamesDoDia.Contains(i.ExameRealizadoId)
                             && idsFolhasMarcadas.Contains(i.ClasseExamesId)
                             && (!somenteSemResultados || i.Resultado == null || i.Resultado == ""))
                    .Select(i => new
                    {
                        i.Id,
                        i.ContaExame,
                        i.Descricao,
                        i.Resultado,
                        i.RefExame,
                        i.ControleApoio,
                        i.PacienteId,
                        i.ExameRealizadoId,
                        i.Sequencial,
                        i.ClasseExamesId,
                        Exame = i.ExamesRealizados
                    })
                    .ToListAsync();

                var ordenados = ordem switch
                {
                    1 => itens.OrderBy(i => i.PacienteId).ThenBy(i => i.ExameRealizadoId).ThenBy(i => i.ContaExame).ToList(),
                    2 => itens.OrderBy(i => i.Exame.InstituicaoId).ThenBy(i => i.ExameRealizadoId).ThenBy(i => i.ContaExame).ToList(),
                    _ => itens.OrderBy(i => i.ExameRealizadoId).ThenBy(i => i.ContaExame).ToList()
                };

                // Paginação: cada exame novo inicia página; mais de 48 itens quebra a página;
                // quando paginado por folha, cada folha de exame também inicia página nova.
                var novas = new List<FichasInternas>();
                int pagina = 0;
                int quantidade = 0;
                int auxExame = -1;
                int auxFolha = -1;

                foreach (var item in ordenados)
                {
                    if (item.ExameRealizadoId == auxExame)
                    {
                        if (quantidade > 48)
                        {
                            pagina++;
                            quantidade = 0;
                        }
                    }
                    else
                    {
                        pagina++;
                        quantidade = 0;
                    }

                    if (paginadoPorFolha && item.ClasseExamesId != auxFolha)
                    {
                        pagina++;
                        quantidade = 0;
                    }

                    quantidade++;
                    auxExame = item.ExameRealizadoId;
                    auxFolha = item.ClasseExamesId;

                    var ficha = MontarFichaInterna(item.RefExame, item.ContaExame, item.Descricao ?? "", null, item.Exame, item.PacienteId, dataExame);
                    ficha.Resultado = item.Resultado;
                    ficha.Pagina = pagina;
                    novas.Add(ficha);
                }

                // Retira as fichas que ficaram somente com a conta principal solitária
                // (grupo ContaExame[0..7] + exame com um único registro).
                var solitarias = novas
                    .GroupBy(f => new { Grupo = f.ContaExame != null && f.ContaExame.Length >= 7 ? f.ContaExame.Substring(0, 7) : "", f.ExamesRealizadosId })
                    .Where(g => g.Count() == 1)
                    .SelectMany(g => g)
                    .ToList();

                foreach (var solitaria in solitarias)
                    novas.Remove(solitaria);

                // ZeraFicha: FichasInternas é efêmera — apaga tudo antes de regenerar.
                var existentes = await _db.FichasInternas.ToListAsync();
                if (existentes.Count > 0)
                    _db.FichasInternas.RemoveRange(existentes);

                if (novas.Count == 0)
                {
                    await _db.SaveChangesAsync();
                    return Json(new { sucesso = false, mensagem = "Não há exames pendentes (não liberados) na data informada. Verifique!" });
                }

                _db.FichasInternas.AddRange(novas);
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, total = novas.Count, mensagem = "OK: Mapas de Corte A4 foram gerados - pode Imprimir!" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] GerarMapaMeiaFolha - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao gerar o Mapa Meia-Folha." });
            }
        }

        /// <summary>
        /// Imprime o Mapa Meia-Folha em PDF retrato (portabilidade do FRelMapaMeiaFolha):
        /// duas fichas por página, agrupadas por Página + exame do FichasInternas.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ImprimirMapaMeiaFolha")]
        public async Task<IActionResult> ImprimirMapaMeiaFolha(string? data)
        {
            if (!TryParseDataUnica(data, out var dataExame))
                return BadRequest("Data do mapa inválida.");

            var registros = await _db.FichasInternas.AsNoTracking().ToListAsync();
            if (registros.Count == 0)
                return NotFound("Mapa não está gerado. Gere o Mapa Meia-Folha antes de imprimir.");

            var ordenados = registros
                .OrderBy(f => f.Pagina)
                .ThenBy(f => f.ExamesRealizadosId)
                .ThenBy(f => f.ContaExame)
                .ToList();

            var idsPacientes = registros.Select(f => f.PacienteId).Distinct().ToList();
            var pacientes = await _db.Pacientes.AsNoTracking()
                .Where(p => idsPacientes.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var idsInstituicoes = registros.Select(f => f.InstituicaoId).Distinct().ToList();
            var siglasInstituicao = await _db.Instituicao.AsNoTracking()
                .Where(i => idsInstituicoes.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Sigla);

            var idsMedicos = registros.Select(f => f.MedicoId).Distinct().ToList();
            var medicos = await _db.Medicos.AsNoTracking()
                .Where(m => idsMedicos.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.NomeMedico);

            var fichas = ordenados
                .GroupBy(f => new { f.Pagina, f.ExamesRealizadosId })
                .Select(g =>
                {
                    var primeira = g.First();
                    pacientes.TryGetValue(primeira.PacienteId, out var pac);
                    return new FichaMeiaFolhaDto
                    {
                        Pagina = g.Key.Pagina,
                        ExamesRealizadosId = g.Key.ExamesRealizadosId,
                        PacienteId = primeira.PacienteId,
                        NomePaciente = pac?.NomePaciente ?? "",
                        Nascimento = pac?.Nascimento ?? DateTime.MinValue,
                        DataIni = primeira.DataIni,
                        ControleApoio = primeira.ControleApoio,
                        SiglaInstituicao = siglasInstituicao.TryGetValue(primeira.InstituicaoId, out var sigla) ? sigla : "",
                        Sequencial = primeira.Sequencial,
                        NomeMedico = medicos.TryGetValue(primeira.MedicoId, out var nomeMedico) ? nomeMedico : "",
                        HistoricoClinico = primeira.HistoricoClinico,
                        Folhas = g.Select(f => f.NomeFicha ?? "").Where(n => n != "").Distinct().ToList(),
                        Linhas = g.Select(f => new LinhaGradeMeiaFolha
                        {
                            ContaPrincipal = f.ContaExame != null && f.ContaExame.Length >= 11 && f.ContaExame.Substring(7, 4) == "0000",
                            Descricao = f.Descricao ?? "",
                            Resultado = f.Resultado
                        }).ToList()
                    };
                })
                .ToList();

            var empresa = _db.Empresa.FirstOrDefault();
            var gerador = new GeradorPdfMapaMeiaFolha();
            byte[] arquivo = gerador.Gerar(fichas, dataExame, empresa);

            string nomeArquivo = $"MapaMeiaFolha_{dataExame:yyyy-MM-dd}.pdf";
            return File(arquivo, "application/pdf", nomeArquivo);
        }
        //..Qoder

        #endregion

        #region Etiquetas

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/Etiquetas")]
        public IActionResult Etiquetas()
        {
            ViewBag.TextoMenu = new object[] { "Etiquetas", false };
            return View();
        }

        /// <summary>
        /// Imprime o PDF de etiquetas dos exames do mapa (portabilidade do
        /// FEtiquetasHemograma): uma etiqueta por exame presente em FichasInternas,
        /// com marcadores VHS / Fator RH / Grupo Sanguíneo detectados nos itens.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ImprimirEtiquetas")]
        public async Task<IActionResult> ImprimirEtiquetas(string? data)
        {
            if (!TryParseDataUnica(data, out var dataExame))
                return BadRequest("Data do mapa inválida.");

            var registros = await _db.FichasInternas.AsNoTracking().ToListAsync();
            if (registros.Count == 0)
                return NotFound("Mapa não está gerado. Gere o Mapa Horizontal ou Meia-Folha antes de imprimir as etiquetas.");

            var idsExames = registros.Select(f => f.ExamesRealizadosId).Distinct().ToList();

            // Marcadores por exame (LIKE '%VHS%', '%FATOR RH%' e '%GRUPO SANGU%' do Delphi).
            var itensExames = await _db.ItensExamesRealizados.AsNoTracking()
                .Where(i => idsExames.Contains(i.ExameRealizadoId) && i.Descricao != null)
                .Select(i => new { i.ExameRealizadoId, i.Descricao })
                .ToListAsync();

            var marcadores = itensExames
                .GroupBy(i => i.ExameRealizadoId)
                .ToDictionary(g => g.Key, g => new
                {
                    Vhs = g.Any(i => (i.Descricao ?? "").ToUpper().Contains("VHS")),
                    FatorRh = g.Any(i => (i.Descricao ?? "").ToUpper().Contains("FATOR RH")),
                    GrupoSanguineo = g.Any(i => (i.Descricao ?? "").ToUpper().Contains("GRUPO SANGU"))
                });

            var idsPacientes = registros.Select(f => f.PacienteId).Distinct().ToList();
            var pacientes = await _db.Pacientes.AsNoTracking()
                .Where(p => idsPacientes.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var idsInstituicoes = registros.Select(f => f.InstituicaoId).Distinct().ToList();
            var siglasInstituicao = await _db.Instituicao.AsNoTracking()
                .Where(i => idsInstituicoes.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Sigla);

            var idsMedicos = registros.Select(f => f.MedicoId).Distinct().ToList();
            var medicos = await _db.Medicos.AsNoTracking()
                .Where(m => idsMedicos.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.NomeMedico);

            var etiquetas = registros
                .OrderBy(f => f.PacienteId)
                .ThenBy(f => f.ExamesRealizadosId)
                .GroupBy(f => new { f.PacienteId, f.ExamesRealizadosId })
                .Select(g =>
                {
                    var primeira = g.First();
                    pacientes.TryGetValue(primeira.PacienteId, out var pac);
                    marcadores.TryGetValue(g.Key.ExamesRealizadosId, out var marca);
                    return new EtiquetaExameDto
                    {
                        ExamesRealizadosId = g.Key.ExamesRealizadosId,
                        PacienteId = g.Key.PacienteId,
                        NomePaciente = pac?.NomePaciente ?? "",
                        Nascimento = pac?.Nascimento ?? DateTime.MinValue,
                        Sexo = pac?.Sexo ?? "",
                        DataIni = primeira.DataIni,
                        SiglaInstituicao = siglasInstituicao.TryGetValue(primeira.InstituicaoId, out var sigla) ? sigla : "",
                        Sequencial = primeira.Sequencial,
                        NomeMedico = medicos.TryGetValue(primeira.MedicoId, out var nomeMedico) ? nomeMedico : "",
                        HistoricoClinico = primeira.HistoricoClinico,
                        TemVhs = marca?.Vhs ?? false,
                        TemFatorRh = marca?.FatorRh ?? false,
                        TemGrupoSanguineo = marca?.GrupoSanguineo ?? false
                    };
                })
                .ToList();

            var empresa = _db.Empresa.FirstOrDefault();
            var gerador = new GeradorPdfEtiquetas();
            byte[] arquivo = gerador.Gerar(etiquetas, dataExame, empresa);

            string nomeArquivo = $"Etiquetas_{dataExame:yyyy-MM-dd}.pdf";
            return File(arquivo, "application/pdf", nomeArquivo);
        }
        //..Qoder

        #endregion

        #region Ficha 40 Colunas

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/FichaQuarentaColunas")]
        public IActionResult FichaQuarentaColunas()
        {
            ViewBag.TextoMenu = new object[] { "Ficha 40 Colunas", false };
            return View();
        }

        /// <summary>
        /// Lista as folhas de exame com coletas pendentes no período (1º passo do
        /// FFicha40Colunas.AbreExamesRealizados): itens não liberados (pendentes) dos
        /// exames do período, deduplicados por folha (NomeCabecalhoFolha → RefExame).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ObterFolhasFicha40")]
        public async Task<IActionResult> ObterFolhasFicha40(string? dataInicial, string? dataFinal, bool somenteSemResultados = true)
        {
            try
            {
                if (!TryParsePeriodo(dataInicial, dataFinal, out var dataIni, out var dataFim))
                    return Json(new { sucesso = false, mensagem = "Informe o período em dd/MM/yyyy (data final maior ou igual à inicial)." });

                var idsExames = await _db.ExamesRealizados
                    .AsNoTracking()
                    .Where(e => e.Liberacao == 0 && e.Baixado != 1 && e.DataIni >= dataIni && e.DataIni <= dataFim)
                    .Select(e => e.Id)
                    .ToListAsync();

                if (idsExames.Count == 0)
                    return Json(new { sucesso = true, folhas = Array.Empty<object>(), mensagem = "Nenhum exame pendente (não liberado) no período informado." });

                var idsClasses = await _db.ClasseExames.AsNoTracking().Select(c => c.Id).ToListAsync();

                var itens = await _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => idsExames.Contains(i.ExameRealizadoId)
                             && i.Liberado == 0
                             && i.Descricao != "."
                             && (!somenteSemResultados || i.Resultado == null || i.Resultado == ""))
                    .OrderBy(i => i.ExameRealizadoId)
                    .ThenBy(i => i.ContaExame)
                    //Feito pelo Qoder em 23/08/2026 — registros antigos (carga de dados) podem
                    // ter o ControleApoio vazio no item; o valor vive no cabeçalho do exame
                    // (mesmo fallback já usado no ConsultarExamesController).
                    .Select(i => new { i.RefExame, i.ContaExame,
                                       ControleApoio = i.ControleApoio != null && i.ControleApoio != ""
                                                       ? i.ControleApoio
                                                       : i.ExamesRealizados.ControleApoio })
                    .ToListAsync();

                // Somente folhas cuja classe existe (Locate do QueryClasse no Delphi);
                // deduplicação final deixa apenas uma linha por folha.
                var folhas = itens
                    .Where(i => i.ContaExame != null
                             && i.ContaExame.Length >= 4
                             && int.TryParse(i.ContaExame.Substring(2, 2), out int codigoClasse)
                             && idsClasses.Contains(codigoClasse))
                    .GroupBy(i => (i.RefExame ?? "").Trim())
                    .Where(g => g.Key != "")
                    .Select(g =>
                    {
                        var ultimo = g.Last();
                        string conta = ultimo.ContaExame ?? "";
                        return new
                        {
                            nomeFolha = g.Key,
                            controleApoio = ultimo.ControleApoio ?? "",
                            contaExame = conta.Length >= 9 ? conta.Substring(conta.Length - 9) : conta
                        };
                    })
                    .OrderBy(f => f.nomeFolha)
                    .ToList();

                return Json(new { sucesso = true, folhas, mensagem = "" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ObterFolhasFicha40 - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao filtrar as folhas de exame." });
            }
        }

        /// <summary>
        /// Lista as coletas de uma folha no período (2º passo, AbreResultados):
        /// uma linha por cliente + exame + controle de apoio (papel do MIN(Codigo)).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("MapasTrabalho/ObterColetasFicha40")]
        public async Task<IActionResult> ObterColetasFicha40(string? nomeFolha, string? dataInicial, string? dataFinal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nomeFolha))
                    return Json(new { sucesso = false, mensagem = "Selecione a folha de exame." });

                if (!TryParsePeriodo(dataInicial, dataFinal, out var dataIni, out var dataFim))
                    return Json(new { sucesso = false, mensagem = "Informe o período em dd/MM/yyyy (data final maior ou igual à inicial)." });

                var coletas = await ObterColetasDaFolhaAsync(nomeFolha, dataIni, dataFim);

                return Json(new
                {
                    sucesso = true,
                    coletas = coletas.Select(c => new
                    {
                        controleApoio = c.ControleApoio,
                        coletaFormatada = FormatarControleColeta(c.ControleApoio),
                        prefixoConta = c.PrefixoConta,
                        examesRealizadosId = c.ExamesRealizadosId,
                        pacienteId = c.PacienteId,
                        nomePaciente = c.NomePaciente,
                        instituicaoSequencial = $"{c.SiglaInstituicao}-{c.Sequencial:000\\.000}"
                    }),
                    mensagem = ""
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ObterColetasFicha40 - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao listar as coletas da folha." });
            }
        }

        /// <summary>
        /// Impressão térmica (40 colunas) da Ficha de Trabalho de uma coleta
        /// (portabilidade do FFicha40Colunas.Imprime_Ficha via ServicoImpressaoCupom).
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ImprimirFichaQuarentaColunas")]
        public IActionResult ImprimirFichaQuarentaColunas(string? controleApoio, string? prefixoConta)
        {
            if (string.IsNullOrWhiteSpace(controleApoio) || string.IsNullOrWhiteSpace(prefixoConta))
                return Json(new { titulo = "Erro", mensagem = "Coleta não informada para a impressão.", sucesso = false });

            var sb = new StringBuilder();
            string? aviso = MontarFichaQuarentaColunas(sb, controleApoio, prefixoConta);
            if (aviso != null)
                return Json(new { titulo = "Aviso", mensagem = aviso, sucesso = false });

            try
            {
                var servico = ActivatorUtilities.CreateInstance<ServicoImpressaoCupom>(_serviceProvider, sb.ToString(), _db);
                var resultado = servico.Executar(controleApoio);
                return Json(new
                {
                    titulo = resultado.Sucesso ? "Sucesso" : "Erro",
                    mensagem = resultado.Mensagem,
                    sucesso = resultado.Sucesso
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ImprimirFichaQuarentaColunas - Erro: " + ex.Message, "wError");
                return Json(new { titulo = "Erro", mensagem = "Erro ao imprimir a Ficha 40 Colunas: " + ex.Message, sucesso = false });
            }
        }

        /// <summary>
        /// Impressão térmica de todas as Fichas da folha no período
        /// (portabilidade do spdImprimeFichaGeralClick): um único cupom por folha.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("MapasTrabalho/ImprimirTodasFichasQuarentaColunas")]
        public async Task<IActionResult> ImprimirTodasFichasQuarentaColunas(string? nomeFolha, string? dataInicial, string? dataFinal)
        {
            if (string.IsNullOrWhiteSpace(nomeFolha))
                return Json(new { titulo = "Erro", mensagem = "Selecione a folha de exame.", sucesso = false });

            if (!TryParsePeriodo(dataInicial, dataFinal, out var dataIni, out var dataFim))
                return Json(new { titulo = "Erro", mensagem = "Período inválido para a impressão.", sucesso = false });

            var coletas = await ObterColetasDaFolhaAsync(nomeFolha, dataIni, dataFim);
            if (coletas.Count == 0)
                return Json(new { titulo = "Aviso", mensagem = "Não há coletas liberadas para esta folha no período informado.", sucesso = false });

            var sb = new StringBuilder();
            foreach (var coleta in coletas)
                MontarFichaQuarentaColunas(sb, coleta.ControleApoio, coleta.PrefixoConta);

            try
            {
                var servico = ActivatorUtilities.CreateInstance<ServicoImpressaoCupom>(_serviceProvider, sb.ToString(), _db);
                var resultado = servico.Executar(nomeFolha.Trim());
                return Json(new
                {
                    titulo = resultado.Sucesso ? "Sucesso" : "Erro",
                    mensagem = resultado.Mensagem,
                    sucesso = resultado.Sucesso
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[MapasTrabalho] ImprimirTodasFichasQuarentaColunas - Erro: " + ex.Message, "wError");
                return Json(new { titulo = "Erro", mensagem = "Erro ao imprimir as Fichas 40 Colunas: " + ex.Message, sucesso = false });
            }
        }

        /// <summary>
        /// Coletas da folha no período: itens não liberados (pendentes) e não baixados com o
        /// ControleApoio dentro do período, deduplicados por cliente + exame +
        /// controle (papel do MIN(Codigo) do AbreResultados).
        /// </summary>
        private async Task<List<ColetaFicha40>> ObterColetasDaFolhaAsync(string nomeFolha, DateTime dataIni, DateTime dataFim)
        {
            var idsExames = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.Liberacao == 0 && e.Baixado != 1 && e.DataIni >= dataIni && e.DataIni <= dataFim)
                .Select(e => e.Id)
                .ToListAsync();

            if (idsExames.Count == 0)
                return new List<ColetaFicha40>();

            string controleIni = dataIni.ToString("yyyyMMdd");
            string controleFim = dataFim.ToString("yyyyMMdd");
            string folha = nomeFolha.Trim().ToUpper();

            //Feito pelo Qoder em 23/08/2026 — registros antigos (carga de dados) podem ter o
            // ControleApoio vazio no item; o valor vive no cabeçalho do exame (mesmo fallback
            // já usado no ConsultarExamesController). O código efetivo da coleta é resolvido
            // em memória e o período é aplicado sobre ele (papel do SUBSTRING do Delphi).
            var itens = await _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => idsExames.Contains(i.ExameRealizadoId)
                         && i.Liberado == 0
                         && i.Baixado != 1
                         && i.Descricao != null
                         && i.Descricao != "."
                         && i.RefExame.ToUpper() == folha)
                .Select(i => new { i.ControleApoio, ControleHeader = i.ExamesRealizados.ControleApoio, i.ContaExame, i.PacienteId, i.ExameRealizadoId, Exame = i.ExamesRealizados })
                .ToListAsync();

            var coletas = itens
                .Select(i => new { i.ContaExame, i.PacienteId, i.ExameRealizadoId, i.Exame,
                                   ControleApoio = string.IsNullOrEmpty(i.ControleApoio) ? i.ControleHeader : i.ControleApoio })
                .Where(i => i.ControleApoio != null
                         && i.ControleApoio.Length >= 8
                         && string.CompareOrdinal(i.ControleApoio.Substring(0, 8), controleIni) >= 0
                         && string.CompareOrdinal(i.ControleApoio.Substring(0, 8), controleFim) <= 0)
                .OrderBy(i => i.ExameRealizadoId)
                .ThenBy(i => i.ControleApoio)
                .GroupBy(i => new { i.PacienteId, i.ExameRealizadoId, i.ControleApoio })
                .Select(g => g.First())
                .ToList();

            var idsPacientes = coletas.Select(c => c.PacienteId).Distinct().ToList();
            var pacientes = await _db.Pacientes.AsNoTracking()
                .Where(p => idsPacientes.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.NomePaciente);

            var idsInstituicoes = coletas.Select(c => c.Exame.InstituicaoId).Distinct().ToList();
            var siglasInstituicao = await _db.Instituicao.AsNoTracking()
                .Where(i => idsInstituicoes.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Sigla);

            return coletas.Select(c => new ColetaFicha40
            {
                ControleApoio = c.ControleApoio ?? "",
                PrefixoConta = c.ContaExame != null && c.ContaExame.Length >= 4 ? c.ContaExame.Substring(0, 4) : "",
                ExamesRealizadosId = c.ExameRealizadoId,
                PacienteId = c.PacienteId,
                NomePaciente = pacientes.TryGetValue(c.PacienteId, out var nome) ? nome : "",
                SiglaInstituicao = siglasInstituicao.TryGetValue(c.Exame.InstituicaoId, out var sigla) ? sigla : "",
                Sequencial = c.Exame.Sequencial
            }).ToList();
        }

        /// <summary>
        /// Monta o texto de uma Ficha 40 Colunas (portabilidade do Imprime_Ficha):
        /// cabeçalho cliente/exame, conta principal centralizada "{ DESC }",
        /// subitens com sublinhados até a coluna 40 e entrelinhas para folhas da
        /// família FEZES (exceto cultura). Retorna aviso quando a coleta não
        /// existe, ou null quando montou com sucesso.
        /// </summary>
        private string? MontarFichaQuarentaColunas(StringBuilder sb, string controleApoio, string prefixoConta)
        {
            var coleta = _db.ItensExamesRealizados
                .AsNoTracking()
                //Feito pelo Qoder em 23/08/2026 — fallback do ControleApoio no cabeçalho
                // (registros antigos com o item vazio, como no ConsultarExamesController).
                .Where(i => (i.ControleApoio == controleApoio
                             || ((i.ControleApoio == null || i.ControleApoio == "") && i.ExamesRealizados.ControleApoio == controleApoio))
                         && i.Liberado == 0 && i.Baixado != 1)
                .OrderBy(i => i.ContaExame)
                .Select(i => new { i.RefExame, i.PacienteId, i.ExameRealizadoId })
                .FirstOrDefault();

            if (coleta == null)
                return "Coleta " + controleApoio + " não encontrada ou já baixada.";

            var exame = _db.ExamesRealizados.AsNoTracking().FirstOrDefault(e => e.Id == coleta.ExameRealizadoId);
            var paciente = _db.Pacientes.AsNoTracking().FirstOrDefault(p => p.Id == coleta.PacienteId);
            var instituicao = exame != null ? _db.Instituicao.AsNoTracking().FirstOrDefault(i => i.Id == exame.InstituicaoId) : null;
            var empresa = _db.Empresa.FirstOrDefault();

            string linhaDivisoria = new string('-', 40);
            string linhaEmBranco = new string('_', 40);

            sb.AppendLine(TruncarColunas(empresa?.TituloEmpresa ?? "LABORATÓRIO", 40));
            sb.AppendLine(linhaDivisoria);
            sb.AppendLine("Nome da Folha de Exames:");
            sb.AppendLine(TruncarColunas(coleta.RefExame, 40));
            sb.AppendLine(linhaDivisoria);
            sb.AppendLine("Código Exame: " + coleta.ExameRealizadoId);
            sb.AppendLine("Sequencial: " + (instituicao?.Sigla ?? "") + "-" + (exame?.Sequencial ?? 0).ToString(@"000\.000"));
            sb.AppendLine("Código de Coleta: " + FormatarControleColeta(controleApoio));
            sb.AppendLine("Código - Nome do Cliente:");
            sb.AppendLine(TruncarColunas(coleta.PacienteId + " - " + (paciente?.NomePaciente ?? ""), 40));
            sb.AppendLine(TruncarColunas("Nasc.: " + (paciente?.Nascimento.ToString("dd/MM/yyyy") ?? "") + " - CPF: " + (paciente?.CPF ?? ""), 40));
            sb.AppendLine(linhaDivisoria);
            sb.AppendLine(" ");
            sb.AppendLine(linhaDivisoria);

            var detalhes = _db.ItensExamesRealizados
                .AsNoTracking()
                //Feito pelo Qoder em 23/08/2026 — fallback do ControleApoio no cabeçalho
                // (registros antigos com o item vazio, como no ConsultarExamesController).
                .Where(i => (i.ControleApoio == controleApoio
                             || ((i.ControleApoio == null || i.ControleApoio == "") && i.ExamesRealizados.ControleApoio == controleApoio))
                         && i.Liberado == 0
                         && i.Baixado != 1
                         && i.ContaExame.StartsWith(prefixoConta))
                .OrderBy(i => i.ContaExame)
                .Select(i => new { i.ContaExame, i.Descricao, i.RefExame })
                .ToList();

            foreach (var detalhe in detalhes)
            {
                string descricao = (detalhe.Descricao ?? "").Trim().ToUpper();

                if (detalhe.ContaExame.Length >= 11 && detalhe.ContaExame.Substring(7, 4) == "0000")
                {
                    sb.AppendLine(CentralizarColunas(TruncarColunas("{ " + descricao + " }", 40), 40));
                    continue;
                }

                sb.AppendLine(TruncarColunas(descricao.PadRight(40, '_'), 40));

                // Folhas da família FEZES ganham entrelinhas para o lançamento manual.
                string folhaItem = (detalhe.RefExame ?? "").ToUpper();
                bool folhaFezes = folhaItem.Contains("FEZES") || folhaItem.Contains("COPROCULTURA")
                    || folhaItem.Contains("PARASITOLOGICO") || folhaItem.Contains("PARASITOLÓGICO");
                if (folhaFezes && !folhaItem.Contains("CULTURA"))
                {
                    sb.AppendLine(" ");
                    sb.AppendLine(" ");
                    sb.AppendLine(" ");
                    sb.AppendLine(" ");
                    sb.AppendLine(linhaDivisoria);
                }
            }

            sb.AppendLine(" ");
            sb.AppendLine("OBS:" + new string('_', 36));
            sb.AppendLine(" ");
            sb.AppendLine(linhaEmBranco);
            sb.AppendLine(" ");
            sb.AppendLine(linhaEmBranco);
            sb.AppendLine("#");
            sb.AppendLine("#");
            sb.AppendLine("#");
            sb.AppendLine(linhaDivisoria);
            sb.AppendLine("#");
            sb.AppendLine("#");
            sb.AppendLine("#");
            sb.AppendLine("#");
            sb.AppendLine("#");
            sb.AppendLine("#");
            sb.AppendLine("#");

            return null;
        }
        //..Qoder

        #endregion

        #region Utilidades

        /// <summary>
        /// Monta um registro de FichasInternas com os dados do exame (comum ao
        /// Mapa Horizontal e ao Mapa Meia-Folha).
        /// </summary>
        private static FichasInternas MontarFichaInterna(string nomeFicha, string? contaExame, string descricao, string? abreviacao, ExamesRealizados exame, int pacienteId, DateTime dataMapa)
        {
            return new FichasInternas
            {
                NomeFicha = nomeFicha,
                ContaExame = contaExame,
                Descricao = descricao,
                MapaHorizontal = abreviacao,
                ExamesRealizadosId = exame.Id,
                PacienteId = pacienteId,
                MedicoId = exame.MedicoId,
                InstituicaoId = exame.InstituicaoId,
                DataExame = dataMapa,
                ControleApoio = exame.ControleApoio,
                Sequencial = exame.Sequencial,
                HistoricoClinico = exame.HistoricoClinico,
                DataIni = exame.DataIni,
                DataFim = exame.DataFim,
                Pagina = 0
            };
        }

        /// <summary>
        /// Converte o ControleApoio (AAAAMMDDNNNN) na máscara AAAA.MM.DD-NNNN.
        /// </summary>
        private static string FormatarControleColeta(string? controleApoio)
        {
            if (string.IsNullOrEmpty(controleApoio) || controleApoio.Length < 12)
                return controleApoio ?? "";
            return $"{controleApoio.Substring(0, 4)}.{controleApoio.Substring(4, 2)}.{controleApoio.Substring(6, 2)}-{controleApoio.Substring(8, 4)}";
        }

        private static string TruncarColunas(string? texto, int colunas)
        {
            texto = texto ?? "";
            return texto.Length <= colunas ? texto : texto.Substring(0, colunas);
        }

        private static string CentralizarColunas(string texto, int colunas)
        {
            if (texto.Length >= colunas) return texto;
            int sobra = colunas - texto.Length;
            return new string(' ', sobra / 2) + texto + new string(' ', sobra - sobra / 2);
        }

        // Uma linha de coleta da Ficha 40 Colunas (cliente + exame + controle de apoio).
        private sealed class ColetaFicha40
        {
            public string ControleApoio { get; set; } = "";
            public string PrefixoConta { get; set; } = "";
            public int ExamesRealizadosId { get; set; }
            public int PacienteId { get; set; }
            public string NomePaciente { get; set; } = "";
            public string SiglaInstituicao { get; set; } = "";
            public int Sequencial { get; set; }
        }

        /// <summary>
        /// Converte a conta de 9 posições (XXYYYZZZZ) na máscara XX.YYY.ZZZZ usada em tela.
        /// </summary>
        private static string FormatarContaMascarada(string chaveConta)
        {
            if (chaveConta.Length != 9) return chaveConta;
            return $"{chaveConta.Substring(0, 2)}.{chaveConta.Substring(2, 3)}.{chaveConta.Substring(5, 4)}";
        }

        /// <summary>
        /// Rótulo do item no Mapa Agrupado: abreviação MapaHorizontal quando existir,
        /// senão a descrição truncada em 20 posições (padrão do FRelMapaAgrupado).
        /// </summary>
        private static string RotuloItemAgrupado(string? abreviacao, string? descricao)
        {
            if (!string.IsNullOrWhiteSpace(abreviacao))
                return abreviacao.Trim();

            var texto = (descricao ?? "").Trim();
            return texto.Length > 20 ? texto.Substring(0, 20) : texto;
        }

                /// <summary>
                /// Conta principal da folha (final '0000', ex.: 11170000000): no Delphi o Mapa
                /// Planilhado exporta só subitens; a principal entra apenas quando a folha não
                /// tem subitens no exame (regra de degradação de 23/08/2026).
                /// </summary>
                private static bool EhContaPrincipal(string contaExame)
                    => contaExame.Length >= 4 && contaExame.Substring(contaExame.Length - 4) == "0000";
        
        private static bool TryParseDataUnica(string? data, out DateTime dataExame)
        {
            dataExame = default;
            if (string.IsNullOrWhiteSpace(data))
                return false;

            return DateTime.TryParseExact(data.Trim(), "dd/MM/yyyy",
                CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out dataExame);
        }

        private static bool TryParsePeriodo(string? dataInicial, string? dataFinal, out DateTime dataIni, out DateTime dataFim)
        {
            dataIni = default;
            dataFim = default;

            var cultura = CultureInfo.GetCultureInfo("pt-BR");
            if (string.IsNullOrWhiteSpace(dataInicial) || string.IsNullOrWhiteSpace(dataFinal))
                return false;

            if (!DateTime.TryParseExact(dataInicial.Trim(), "dd/MM/yyyy", cultura, DateTimeStyles.None, out dataIni))
                return false;
            if (!DateTime.TryParseExact(dataFinal.Trim(), "dd/MM/yyyy", cultura, DateTimeStyles.None, out dataFim))
                return false;

            return dataFim >= dataIni;
        }

        private static decimal? ParseValorResultado(string? resultado)
        {
            if (string.IsNullOrWhiteSpace(resultado))
                return null;

            var primeiroToken = resultado.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(primeiroToken))
                return null;

            var limpo = new string(primeiroToken.Where(c => char.IsDigit(c) || c == ',' || c == '.' || c == '-').ToArray());
            if (decimal.TryParse(limpo, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out var valor))
                return valor;

            return null;
        }

        #endregion
    }
}
