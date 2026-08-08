using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class RelatorioFaturamentoController : BaseController
    {
        private readonly ILogger<RelatorioFaturamentoController> _logger;

        public RelatorioFaturamentoController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            ILogger<RelatorioFaturamentoController> logger)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _logger = logger;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("RelatorioFaturamento")]
        public async Task<IActionResult> Index()
        {
            var model = new vmRelatorioFaturamento();
            await CarregarListasAsync(model);
            return View(model);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("RelatorioFaturamento/GerarPdf")]
        public async Task<IActionResult> GerarPdf(vmRelatorioFaturamento filtro)
        {
            if (filtro.DataFim < filtro.DataIni)
            {
                TempData["MensagemErro"] = "Data Final precisa ser maior ou igual que Data Inicial.";
                await CarregarListasAsync(filtro);
                return View("Index", filtro);
            }

            if (filtro.InstituicoesSelecionadas == null || filtro.InstituicoesSelecionadas.Count == 0)
            {
                TempData["MensagemErro"] = "Pelo menos uma Instituição deve ser selecionada.";
                await CarregarListasAsync(filtro);
                return View("Index", filtro);
            }

            _logger.LogInformation(
                "GerarPdf: periodo {DataIni:dd/MM/yyyy} a {DataFim:dd/MM/yyyy}, instituicoes={Instituicoes}, tabelas={Tabelas}, incluirBaixados={IncluirBaixados}, formato={Formato}",
                filtro.DataIni, filtro.DataFim,
                string.Join(",", filtro.InstituicoesSelecionadas),
                string.Join(",", filtro.TabelasSelecionadas),
                filtro.IncluirBaixados,
                filtro.FormatoSaida);

            var empresa = await _db.Empresa.AsNoTracking().FirstOrDefaultAsync();
            var dados = await MontarDadosRelatorioAsync(filtro);

            _logger.LogInformation("GerarPdf: total de exames no relatorio={Total}", dados.Exames.Count);

            string nomeBase = $"Faturamento_{filtro.DataIni:ddMMyyyy}_a_{filtro.DataFim:ddMMyyyy}";

            return filtro.FormatoSaida switch
            {
                1 => GerarRespostaHtml(dados, empresa, nomeBase),
                2 => GerarRespostaWord(dados, empresa, nomeBase, filtro.DuasColunas),
                _ => GerarRespostaPdf(dados, empresa, nomeBase, filtro.DuasColunas)
            };
        }

        private FileResult GerarRespostaPdf(DadosPdfFaturamento dados, Empresa? empresa, string nomeBase, bool duasColunas)
        {
            var gerador = new GeradorPdfFaturamento();
            byte[] bytes = gerador.Gerar(dados, empresa, duasColunas);
            return File(bytes, "application/pdf", $"{nomeBase}.pdf");
        }

        private FileResult GerarRespostaHtml(DadosPdfFaturamento dados, Empresa? empresa, string nomeBase)
        {
            var gerador = new GeradorHtmlFaturamento();
            byte[] bytes = gerador.Gerar(dados, empresa);
            return File(bytes, "text/html; charset=utf-8", $"{nomeBase}.html");
        }

        private FileResult GerarRespostaWord(DadosPdfFaturamento dados, Empresa? empresa, string nomeBase, bool duasColunas)
        {
            var gerador = new GeradorWordFaturamento();
            byte[] bytes = gerador.Gerar(dados, empresa);
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{nomeBase}.docx");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("RelatorioFaturamento/CarregarInstituicoes")]
        public async Task<IActionResult> CarregarInstituicoes(DateTime dataIni, DateTime dataFim, bool incluirBaixados)
        {
            var instituicoes = await ObterInstituicoesAsync(dataIni, dataFim, incluirBaixados);
            return Json(instituicoes);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("RelatorioFaturamento/CarregarTabelas")]
        public async Task<IActionResult> CarregarTabelas(DateTime dataIni, DateTime dataFim, List<string> instituicoes, bool incluirBaixados)
        {
            var ids = instituicoes?
                .Select(v => int.TryParse(v, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList() ?? [];

            _logger.LogInformation("CarregarTabelas recebido: dataIni={DataIni}, dataFim={DataFim}, instituicoesRaw=[{Raw}], ids=[{Ids}]", dataIni, dataFim, string.Join(",", instituicoes ?? []), string.Join(",", ids));

            var tabelas = await ObterTabelasAsync(dataIni, dataFim, ids, incluirBaixados);
            return Json(tabelas);
        }

        // Endpoint temporario de diagnostico para investigar divergencia de tabelas (PART x GARCIA).
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("RelatorioFaturamento/DiagnosticoTabelas")]
        public async Task<IActionResult> DiagnosticoTabelas(DateTime dataIni, DateTime dataFim, List<string> instituicoes, bool incluirBaixados)
        {
            var ids = instituicoes?
                .Select(v => int.TryParse(v, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList() ?? [];

            // As colunas DataIni/DataFim sao timestamptz (UTC no banco).
            // A carga de dados do Firebird gravou as datas locais do Delphi como UTC,
            // entao filtramos pelo proprio valor UTC da coluna, sem conversao de timezone.
            // Os parametros de filtro permanecem com Kind=Unspecified (data pura).
            var inicio = DateTime.SpecifyKind(dataIni.Date, DateTimeKind.Unspecified);
            var fim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Unspecified);

            var query = _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.TabelaExames)
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .Where(e => e.Liberacao == 1)
                .Where(e => incluirBaixados || e.Baixado != 1)
                .Where(e => e.TabelaExames != null)
                .AsQueryable();

            if (ids != null && ids.Count > 0)
                query = query.Where(e => ids.Contains(e.InstituicaoId));

            var ativos = await query
                .Select(e => new
                {
                    e.Id,
                    e.InstituicaoId,
                    InstituicaoSigla = e.Instituicao!.Sigla,
                    TabelaId = e.TabelaExamesId,
                    TabelaSigla = e.TabelaExames!.SiglaTabela,
                    e.DataIni,
                    e.DataFim,
                    e.Liberacao,
                    e.Baixado
                })
                .ToListAsync();

            var am = new List<object>();
            if (incluirBaixados)
            {
                var queryAm = _db.ExamesRealizadosAM
                    .AsNoTracking()
                    .Include(e => e.Instituicao)
                    .Include(e => e.TabelaExames)
                    .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                    .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                    .Where(e => e.Liberacao == 1)
                    .Where(e => e.TabelaExames != null)
                    .AsQueryable();

                if (ids != null && ids.Count > 0)
                    queryAm = queryAm.Where(e => ids.Contains(e.InstituicaoId));

                am = await queryAm
                    .Select(e => new
                    {
                        e.Id,
                        e.InstituicaoId,
                        InstituicaoSigla = e.Instituicao!.Sigla,
                        TabelaId = e.TabelaExamesId,
                        TabelaSigla = e.TabelaExames!.SiglaTabela,
                        e.DataIni,
                        e.DataFim,
                        e.Liberacao,
                        e.Baixado
                    })
                    .ToListAsync<object>();
            }

            var semDataFim = await _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.TabelaExames)
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => !e.DataFim.HasValue)
                .Where(e => e.Liberacao == 1)
                .Where(e => incluirBaixados || e.Baixado != 1)
                .Where(e => ids != null && ids.Count > 0 ? ids.Contains(e.InstituicaoId) : true)
                .Select(e => new
                {
                    e.Id,
                    e.InstituicaoId,
                    InstituicaoSigla = e.Instituicao!.Sigla,
                    TabelaId = e.TabelaExamesId,
                    TabelaSigla = e.TabelaExames!.SiglaTabela,
                    e.DataIni,
                    e.DataFim,
                    e.Liberacao,
                    e.Baixado,
                    Motivo = "Sem DataFim - excluido pelo filtro DataFim.HasValue"
                })
                .ToListAsync();

            return Json(new
            {
                PeriodoLocal = new { Inicio = inicio, Fim = fim },
                IncluirBaixados = incluirBaixados,
                Ativos = ativos,
                AM = am,
                SemDataFim = semDataFim,
                TabelasDistintasAtivos = ativos.Select(a => a.TabelaSigla).Distinct().OrderBy(x => x),
                TabelasDistintasAM = am.Select(a => ((dynamic)a).TabelaSigla).Distinct().OrderBy(x => x)
            });
        }

        private async Task CarregarListasAsync(vmRelatorioFaturamento model)
        {
            model.Instituicoes = await ObterInstituicoesAsync(model.DataIni, model.DataFim, model.IncluirBaixados);

            if (model.InstituicoesSelecionadas.Count > 0)
            {
                model.Tabelas = await ObterTabelasAsync(model.DataIni, model.DataFim, model.InstituicoesSelecionadas, model.IncluirBaixados);
            }
            else
            {
                model.Tabelas = [];
            }
        }

        private async Task<List<SelectListItem>> ObterInstituicoesAsync(DateTime dataIni, DateTime dataFim, bool incluirBaixados)
        {
            // Regra de negocio: exibir apenas as instituicoes que possuem exames realizados
            // no periodo informado. As colunas DataIni/DataFim sao timestamptz (UTC no banco).
            // A carga de dados do Firebird gravou as datas locais do Delphi como UTC, entao
            // usamos o proprio valor UTC da coluna para filtrar, igual ao comportamento
            // do Delphi (que nao converte timezone).
            // Os parametros de filtro permanecem com Kind=Unspecified (data pura).
            var inicio = DateTime.SpecifyKind(dataIni.Date, DateTimeKind.Unspecified);
            var fim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Unspecified);

            // IDs de instituicoes com exames ativos no periodo
            var idsAtivos = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim
                         && e.Liberacao == 1 && e.Baixado != 1
                         && e.InstituicaoId > 0)
                .Select(e => e.InstituicaoId)
                .Distinct()
                .ToListAsync();

            // Se incluir baixados, adiciona tambem as de ExamesRealizadosAM
            if (incluirBaixados)
            {
                var idsAm = await _db.ExamesRealizadosAM
                    .AsNoTracking()
                    .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim
                             && e.Liberacao == 1
                             && e.InstituicaoId > 0)
                    .Select(e => e.InstituicaoId)
                    .Distinct()
                    .ToListAsync();

                idsAtivos = idsAtivos.Union(idsAm).Distinct().ToList();
            }

            var instituicoes = await _db.Instituicao
                .AsNoTracking()
                .Where(i => idsAtivos.Contains(i.Id) && !string.IsNullOrEmpty(i.Sigla))
                .OrderBy(i => i.Sigla)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Sigla} - {i.Nome}"
                })
                .ToListAsync();

            _logger.LogInformation(
                "ObterInstituicoesAsync: periodo {DataIni:dd/MM/yyyy} a {DataFim:dd/MM/yyyy}, incluirBaixados={IncluirBaixados}, total instituicoes no periodo={Total}",
                dataIni, dataFim, incluirBaixados, instituicoes.Count);

            return instituicoes;
        }

        private async Task<List<SelectListItem>> ObterTabelasAsync(DateTime dataIni, DateTime dataFim, List<int> instituicoes, bool incluirBaixados)
        {
            // As colunas DataIni/DataFim sao timestamptz (UTC no banco).
            // A carga de dados do Firebird gravou as datas locais do Delphi como UTC,
            // entao filtramos pelo proprio valor UTC da coluna, sem conversao de timezone.
            // Os parametros de filtro permanecem com Kind=Unspecified (data pura).
            var inicio = DateTime.SpecifyKind(dataIni.Date, DateTimeKind.Unspecified);
            var fim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Unspecified);

            _logger.LogInformation(
                "ObterTabelasAsync: periodo local {DataIni:dd/MM/yyyy} a {DataFim:dd/MM/yyyy}, instituicoes=[{Instituicoes}], incluirBaixados={IncluirBaixados}",
                dataIni, dataFim,
                instituicoes == null ? "null" : string.Join(",", instituicoes),
                incluirBaixados);

            // Diagnostico: quantos exames no periodo, ignorando instituicao/tabela/liberacao/baixado
            // Regra alinhada com o Delphi: exame deve iniciar E terminar dentro do periodo.
            var totalExamesPeriodo = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: totalExamesRealizados no periodo={Total}", totalExamesPeriodo);

            var liberados = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .Where(e => e.Liberacao == 1)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: apos Liberacao==1 ={Total}", liberados);

            var naoBaixados = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .Where(e => e.Liberacao == 1)
                .Where(e => incluirBaixados || e.Baixado != 1)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: apos filtro Baixado ={Total}", naoBaixados);

            var comTabela = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .Where(e => e.Liberacao == 1)
                .Where(e => incluirBaixados || e.Baixado != 1)
                .Where(e => e.TabelaExames != null)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: apos TabelaExames!=null ={Total}", comTabela);

            var query = _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .Where(e => e.Liberacao == 1)
                .Where(e => incluirBaixados || e.Baixado != 1)
                .Where(e => e.TabelaExames != null)
                .AsQueryable();

            if (instituicoes != null && instituicoes.Count > 0)
            {
                var comInstituicao = await query.Where(e => instituicoes.Contains(e.InstituicaoId)).CountAsync();
                _logger.LogInformation("ObterTabelasAsync: apos filtro InstituicaoId=[{Ids}] ={Total}", string.Join(",", instituicoes), comInstituicao);
                query = query.Where(e => instituicoes.Contains(e.InstituicaoId));
            }

            var tabelas = await query
                .Select(e => new { e.TabelaExames!.Id, e.TabelaExames.SiglaTabela, e.TabelaExames.NomeTabela, e.InstituicaoId, e.DataIni, e.DataExame })
                .ToListAsync();

            _logger.LogInformation(
                "ObterTabelasAsync: ExamesRealizados com tabela encontrados={Total}, detalhes={Detalhes}",
                tabelas.Count,
                string.Join("; ", tabelas.Select(t => $"Inst={t.InstituicaoId} Tab={t.Id}({t.SiglaTabela}) DataIni={t.DataIni:dd/MM/yyyy HH:mm} DataExame={t.DataExame:dd/MM/yyyy}")));

            if (incluirBaixados)
            {
                var queryAm = _db.ExamesRealizadosAM
                    .AsNoTracking()
                    .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                    .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                    .Where(e => e.Liberacao == 1)
                    .Where(e => e.TabelaExames != null)
                    .AsQueryable();

                if (instituicoes != null && instituicoes.Count > 0)
                {
                    queryAm = queryAm.Where(e => instituicoes.Contains(e.InstituicaoId));
                }

                var tabelasAm = await queryAm
                    .Select(e => new { e.TabelaExames!.Id, e.TabelaExames.SiglaTabela, e.TabelaExames.NomeTabela, e.InstituicaoId, e.DataIni, e.DataExame })
                    .ToListAsync();

                _logger.LogInformation(
                    "ObterTabelasAsync: ExamesRealizadosAM com tabela encontrados={Total}, detalhes={Detalhes}",
                    tabelasAm.Count,
                    string.Join("; ", tabelasAm.Select(t => $"Inst={t.InstituicaoId} Tab={t.Id}({t.SiglaTabela}) DataIni={t.DataIni:dd/MM/yyyy HH:mm} DataExame={t.DataExame:dd/MM/yyyy}")));

                tabelas.AddRange(tabelasAm);
            }

            var resultado = tabelas
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .OrderBy(t => t.SiglaTabela)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.SiglaTabela} - {t.NomeTabela}"
                })
                .ToList();

            _logger.LogInformation(
                "ObterTabelasAsync: resultado final={Resultado}",
                string.Join(", ", resultado.Select(r => r.Text)));

            return resultado;
        }

        private async Task<DadosPdfFaturamento> MontarDadosRelatorioAsync(vmRelatorioFaturamento filtro)
        {
            // As colunas DataIni/DataFim no PostgreSQL sao timestamptz (UTC no banco).
            // A carga de dados do Firebird gravou as datas locais do Delphi como UTC.
            // Portanto usamos o proprio valor UTC da coluna para filtrar, sem conversao
            // de timezone, garantindo alinhamento com o Delphi.
            // Os parametros de filtro permanecem com Kind=Unspecified (data pura).
            var inicio = DateTime.SpecifyKind(filtro.DataIni.Date, DateTimeKind.Unspecified);
            var fim = DateTime.SpecifyKind(filtro.DataFim.Date, DateTimeKind.Unspecified);

            // Atualiza valores zerados pelo PlanoExames antes da impressao.
            // Roda sempre: a opcao MostragemPrecos controla apenas a exibicao,
            // nao deve impedir a tentativa de recuperar valores do PlanoExames.
            await AtualizarValoresZeradosAsync(inicio, fim, filtro.InstituicoesSelecionadas, filtro.TabelasSelecionadas);

            // Seleciona exames (ativos + arquivados/baixados quando solicitado)
            var exames = await SelecionarExamesAsync(inicio, fim, filtro);

            // --- Batch de itens: 2 queries fixas no lugar de N+1 ---
            var idsAtivos = exames.Where(e => !e.OrigemAM).Select(e => e.Id).ToList();
            var idsAM    = exames.Where(e => e.OrigemAM).Select(e => e.Id).ToList();

            // Query unica para todos os itens de exames ativos
            var queryItensAtivos = _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => idsAtivos.Contains(i.ExameRealizadoId))
                .Where(i => !string.IsNullOrEmpty(i.Descricao))
                .Where(i => !EF.Functions.Like(i.Descricao!.ToLower(), "exames%"));

            if (filtro.MostragemPrecos == 2)
                queryItensAtivos = queryItensAtivos.Where(i => i.ValorItem.HasValue && i.ValorItem.Value != 0);

            var rawAtivos = await queryItensAtivos
                .OrderBy(i => i.ExameRealizadoId).ThenBy(i => i.OrdemItem)
                .Select(i => new { ExameId = i.ExameRealizadoId, i.TabelaExamesId, i.Descricao, i.ValorItem, i.ClasseExamesNome })
                .ToListAsync();

            var dictAtivos = rawAtivos
                .GroupBy(i => i.ExameId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(i => new ItemFaturamentoFonte
                    {
                        Descricao = i.Descricao,
                        ValorItem = i.ValorItem,
                        ClasseExamesNome = i.ClasseExamesNome
                    }).ToList());

            // Query unica para todos os itens de exames arquivados (AM)
            Dictionary<int, List<ItemFaturamentoFonte>> dictAM = [];
            if (idsAM.Count > 0)
            {
                var queryItensAM = _db.ItensExamesRealizadosAM
                    .AsNoTracking()
                    .Where(i => idsAM.Contains(i.ExameRealizadoAMId))
                    .Where(i => !string.IsNullOrEmpty(i.Descricao))
                    .Where(i => !EF.Functions.Like(i.Descricao!.ToLower(), "exames%"));

                if (filtro.MostragemPrecos == 2)
                    queryItensAM = queryItensAM.Where(i => i.ValorItem.HasValue && i.ValorItem.Value != 0);

                var rawAM = await queryItensAM
                    .OrderBy(i => i.ExameRealizadoAMId).ThenBy(i => i.OrdemItem)
                    .Select(i => new { ExameId = i.ExameRealizadoAMId, i.TabelaExamesId, i.Descricao, i.ValorItem, i.ClasseExamesNome })
                    .ToListAsync();

                dictAM = rawAM
                    .GroupBy(i => i.ExameId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(i => new ItemFaturamentoFonte
                        {
                            Descricao = i.Descricao,
                            ValorItem = i.ValorItem,
                            ClasseExamesNome = i.ClasseExamesNome
                        }).ToList());
            }
            // --- fim batch ---

            var dados = new DadosPdfFaturamento
            {
                DataIni = filtro.DataIni,
                DataFim = filtro.DataFim,
                Ordenacao = filtro.Ordenacao,
                MostragemPrecos = filtro.MostragemPrecos,
                ExibirDataConclusao = filtro.ExibirDataConclusao
            };

            int sequencia = 0;
            foreach (var exame in exames)
            {
                sequencia++;

                // Lookup no dicionario — sem query adicional ao banco
                var itens = exame.OrigemAM
                    ? dictAM.GetValueOrDefault(exame.Id) ?? []
                    : dictAtivos.GetValueOrDefault(exame.Id) ?? [];

                var exameDto = new ExameFaturamentoDto
                {
                    Sequencia = sequencia,
                    ExameId = exame.Id,
                    PacienteId = exame.PacienteId,
                    NomePaciente = exame.NomePaciente,
                    SiglaInstituicao = exame.SiglaInstituicao,
                    NomeInstituicao = exame.NomeInstituicao,
                    SiglaTabela = exame.SiglaTabela,
                    NomeTabela = exame.NomeTabela,
                    Sequencial = exame.Sequencial,
                    // O padrao do Delphi nao exibe data do exame.
                    // Quando solicitado, exibe a Data de Conclusao (DataFim).
                    DataExame = filtro.ExibirDataConclusao && exame.DataFim.HasValue
                        ? exame.DataFim
                        : null,
                    Itens = itens.Select(i => new ItemFaturamentoDto
                    {
                        Descricao = i.Descricao ?? "",
                        ValorItem = i.ValorItem ?? 0,
                        ClasseExamesNome = i.ClasseExamesNome ?? ""
                    }).ToList()
                };

                // Quando MostragemPrecos=2 (nao imprimir zerados), exames sem nenhum item com valor
                // nao devem aparecer no relatorio (todos os itens foram filtrados por serem zerados)
                if (filtro.MostragemPrecos == 2 && exameDto.Itens.Count == 0)
                    continue;

                dados.Exames.Add(exameDto);
            }

            // Totais por instituicao
            dados.TotaisPorInstituicao = dados.Exames
                .GroupBy(e => new { e.SiglaInstituicao, e.NomeInstituicao })
                .Select(g => new TotalFaturamentoDto
                {
                    Descricao = g.Key.NomeInstituicao,
                    Sigla = g.Key.SiglaInstituicao,
                    Valor = g.Sum(e => e.ValorTotal)
                })
                .OrderBy(t => t.Sigla)
                .ToList();

            // Tabelas de precos utilizadas nos exames do relatorio
            dados.TabelasUtilizadas = dados.Exames
                .Select(e => $"{e.SiglaTabela} - {e.NomeTabela}")
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Quantitativo de itens de exames realizados (igual ao final do relatorio Delphi)
            dados.QuantitativoItens = await ObterQuantitativoItensAsync(inicio, fim, filtro.TabelasSelecionadas);

            return dados;
        }

        private async Task<List<QuantitativoItemDto>> ObterQuantitativoItensAsync(DateTime inicio, DateTime fim, List<int> tabelas)
        {
            // Alinhado com FRelExamesRealizados.BandaSumarioExtensaoBeforePrint (Delphi):
            // - Conta itens de ExamesRealizados no periodo (DataIni/DataFim dentro do range).
            // - Exclui itens com ValorItem = 0 e descricoes que iniciem com "EXAMES".
            // - Filtra pelas tabelas de precos selecionadas.
            // - Nao filtra por instituicao (igual ao Delphi).
            // - Agrupa por RefExame (Folha), Descricao (Item) e ContaExame.
            var query = _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ValorItem != 0 && i.ValorItem != null)
                .Where(i => !string.IsNullOrEmpty(i.Descricao) && !EF.Functions.Like(i.Descricao.ToLower(), "exames%"))
                .Where(i => tabelas == null || tabelas.Count == 0 || (tabelas.Contains(i.TabelaExamesId) && tabelas.Contains(i.ExamesRealizados.TabelaExamesId)))
                .Where(i => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataIni, "UTC").Date >= inicio
                         && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataIni, "UTC").Date <= fim)
                .Where(i => i.ExamesRealizados.DataFim.HasValue
                         && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataFim.Value, "UTC").Date >= inicio
                         && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataFim.Value, "UTC").Date <= fim)
                .Where(i => i.ExamesRealizados.Liberacao == 1 && i.ExamesRealizados.Baixado != 1);

            var resultado = await query
                .GroupBy(i => new { i.RefExame, i.Descricao, i.ContaExame })
                .Select(g => new QuantitativoItemDto
                {
                    ContaExame = g.Key.ContaExame ?? "",
                    Folha = g.Key.RefExame ?? "",
                    Item = g.Key.Descricao ?? "",
                    Quantidade = g.Count()
                })
                .OrderBy(q => q.Folha)
                .ThenBy(q => q.Item)
                .ThenBy(q => q.ContaExame)
                .ToListAsync();

            _logger.LogInformation(
                "ObterQuantitativoItensAsync: periodo {Inicio:dd/MM/yyyy} a {Fim:dd/MM/yyyy}, tabelas=[{Tabelas}], itens={Itens}",
                inicio, fim,
                tabelas == null ? "null" : string.Join(",", tabelas),
                resultado.Count);

            return resultado;
        }

        private async Task AtualizarValoresZeradosAsync(DateTime inicio, DateTime fim, List<int> instituicoes, List<int> tabelas)
        {
            var query = _db.ItensExamesRealizados
                .Where(i => i.ValorItem == 0 || i.ValorItem == null)
                .Where(i => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataIni, "UTC").Date <= fim)
                .Where(i => i.ExamesRealizados.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(i.ExamesRealizados.DataFim.Value, "UTC").Date <= fim)
                .Where(i => i.ExamesRealizados.Liberacao == 1 && i.ExamesRealizados.Baixado != 1)
                .AsQueryable();

            if (instituicoes != null && instituicoes.Count > 0)
                query = query.Where(i => instituicoes.Contains(i.InstituicaoId));

            if (tabelas != null && tabelas.Count > 0)
                query = query.Where(i => tabelas.Contains(i.TabelaExamesId));

            var itensZerados = await query.ToListAsync();

            foreach (var item in itensZerados)
            {
                var plano = await _db.PlanoExames
                    .AsNoTracking()
                    .Where(p => p.ContaExame == item.ContaExame)
                    .Where(p => p.TabelaExamesId == item.TabelaExamesId)
                    .FirstOrDefaultAsync();

                if (plano?.ValorItem.HasValue == true && plano.ValorItem.Value > 0)
                {
                    item.ValorItem = plano.ValorItem.Value;
                }
            }

            await _db.SaveChangesAsync();
        }

        private async Task<List<ExameFaturamentoFonte>> SelecionarExamesAsync(DateTime inicio, DateTime fim, vmRelatorioFaturamento filtro)
        {
            var exames = new List<ExameFaturamentoFonte>();

            var totalNoPeriodo = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .CountAsync();

            var totalLiberadosNaoBaixados = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .Where(e => e.Liberacao == 1 && e.Baixado != 1)
                .CountAsync();

            _logger.LogInformation(
                "SelecionarExamesAsync: periodo local {Inicio:dd/MM/yyyy} a {Fim:dd/MM/yyyy}, totalNoPeriodo={TotalNoPeriodo}, liberadosNaoBaixados={LiberadosNaoBaixados}",
                inicio, fim, totalNoPeriodo, totalLiberadosNaoBaixados);

            var query = _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.TabelaExames)
                .Include(e => e.Pacientes)
                .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                .Where(e => e.Liberacao == 1 && e.Baixado != 1)
                .AsQueryable();

            if (filtro.InstituicoesSelecionadas.Count > 0)
                query = query.Where(e => filtro.InstituicoesSelecionadas.Contains(e.InstituicaoId));

            if (filtro.TabelasSelecionadas.Count > 0)
                query = query.Where(e => filtro.TabelasSelecionadas.Contains(e.TabelaExamesId));

            var contagemComFiltros = await query.CountAsync();
            _logger.LogInformation(
                "SelecionarExamesAsync: apos filtros de instituicao/tabela, exames encontrados={Contagem}",
                contagemComFiltros);

            query = filtro.Ordenacao switch
            {
                0 => query.OrderBy(e => e.Pacientes.NomePaciente).ThenBy(e => e.Id),
                1 => query.OrderBy(e => e.Instituicao.Sigla).ThenBy(e => e.Sequencial),
                _ => query.OrderBy(e => e.DataFim).ThenBy(e => e.Instituicao.Sigla).ThenBy(e => e.Sequencial)
            };

            foreach (var e in await query.ToListAsync())
            {
                exames.Add(new ExameFaturamentoFonte
                {
                    Id = e.Id,
                    PacienteId = e.PacienteId,
                    NomePaciente = e.Pacientes?.NomePaciente ?? "",
                    InstituicaoId = e.InstituicaoId,
                    SiglaInstituicao = e.Instituicao?.Sigla ?? "",
                    NomeInstituicao = e.Instituicao?.Nome ?? "",
                    TabelaExamesId = e.TabelaExamesId,
                    SiglaTabela = e.TabelaExames?.SiglaTabela ?? "",
                    NomeTabela = e.TabelaExames?.NomeTabela ?? "",
                    Sequencial = e.Sequencial,
                    DataIni = e.DataIni,
                    DataFim = e.DataFim,
                    DataExame = e.DataExame,
                    OrigemAM = false
                });
            }

            if (filtro.IncluirBaixados)
            {
                var queryAm = _db.ExamesRealizadosAM
                    .AsNoTracking()
                    .Include(e => e.Instituicao)
                    .Include(e => e.TabelaExames)
                    .Include(e => e.Pacientes)
                    .Where(e => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataIni, "UTC").Date <= fim)
                    .Where(e => e.DataFim.HasValue && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date >= inicio && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.DataFim.Value, "UTC").Date <= fim)
                    .Where(e => e.Liberacao == 1)
                    .AsQueryable();

                if (filtro.InstituicoesSelecionadas.Count > 0)
                    queryAm = queryAm.Where(e => filtro.InstituicoesSelecionadas.Contains(e.InstituicaoId));

                if (filtro.TabelasSelecionadas.Count > 0)
                    queryAm = queryAm.Where(e => filtro.TabelasSelecionadas.Contains(e.TabelaExamesId));

                foreach (var e in await queryAm.ToListAsync())
                {
                    exames.Add(new ExameFaturamentoFonte
                    {
                        Id = e.Id,
                        PacienteId = e.PacienteId,
                        NomePaciente = e.Pacientes?.NomePaciente ?? "",
                        InstituicaoId = e.InstituicaoId,
                        SiglaInstituicao = e.Instituicao?.Sigla ?? "",
                        NomeInstituicao = e.Instituicao?.Nome ?? "",
                        TabelaExamesId = e.TabelaExamesId,
                        SiglaTabela = e.TabelaExames?.SiglaTabela ?? "",
                        NomeTabela = e.TabelaExames?.NomeTabela ?? "",
                        Sequencial = e.Sequencial,
                        DataIni = e.DataIni,
                        DataFim = e.DataFim,
                        DataExame = e.DataExame,
                        OrigemAM = true
                    });
                }
            }

            // Reordena caso AM tenha sido incluido e ordenacao seja por Data.
            // No Delphi a ordenacao por data usa DataFim (coluna 14), depois Instituicao e Sequencial.
            if (filtro.IncluirBaixados && filtro.Ordenacao == 2)
            {
                exames = exames
                    .OrderBy(e => e.DataFim)
                    .ThenBy(e => e.SiglaInstituicao)
                    .ThenBy(e => e.Sequencial)
                    .ToList();
            }
            else if (filtro.IncluirBaixados && filtro.Ordenacao == 1)
            {
                exames = exames
                    .OrderBy(e => e.SiglaInstituicao)
                    .ThenBy(e => e.Sequencial)
                    .ToList();
            }
            else if (filtro.IncluirBaixados && filtro.Ordenacao == 0)
            {
                exames = exames
                    .OrderBy(e => e.NomePaciente)
                    .ThenBy(e => e.Id)
                    .ToList();
            }

            return exames;
        }

        private class ExameFaturamentoFonte
        {
            public int Id { get; set; }
            public int PacienteId { get; set; }
            public string NomePaciente { get; set; } = "";
            public int InstituicaoId { get; set; }
            public string SiglaInstituicao { get; set; } = "";
            public string NomeInstituicao { get; set; } = "";
            public int TabelaExamesId { get; set; }
            public string SiglaTabela { get; set; } = "";
            public string NomeTabela { get; set; } = "";
            public int Sequencial { get; set; }
            public DateTime DataIni { get; set; }
            public DateTime? DataFim { get; set; }
            public DateTime? DataExame { get; set; }
            public bool OrigemAM { get; set; }
        }

        private class ItemFaturamentoFonte
        {
            public string? Descricao { get; set; }
            public decimal? ValorItem { get; set; }
            public string? ClasseExamesNome { get; set; }
        }
    }
}
