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
                "GerarPdf: periodo {DataIni:dd/MM/yyyy} a {DataFim:dd/MM/yyyy}, instituicoes={Instituicoes}, tabelas={Tabelas}, incluirBaixados={IncluirBaixados}",
                filtro.DataIni, filtro.DataFim,
                string.Join(",", filtro.InstituicoesSelecionadas),
                string.Join(",", filtro.TabelasSelecionadas),
                filtro.IncluirBaixados);

            var empresa = await _db.Empresa.AsNoTracking().FirstOrDefaultAsync();
            var dados = await MontarDadosRelatorioAsync(filtro);

            _logger.LogInformation("GerarPdf: total de exames no relatorio={Total}", dados.Exames.Count);

            var gerador = new GeradorPdfFaturamento();
            byte[] pdfBytes = gerador.Gerar(dados, empresa, filtro.DuasColunas);

            string nomeArquivo = $"Faturamento_{filtro.DataIni:ddMMyyyy}_a_{filtro.DataFim:ddMMyyyy}.pdf";
            return File(pdfBytes, "application/pdf", nomeArquivo);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("RelatorioFaturamento/CarregarTabelas")]
        public async Task<IActionResult> CarregarTabelas(DateTime dataIni, DateTime dataFim, List<int> instituicoes, bool incluirBaixados)
        {
            var tabelas = await ObterTabelasAsync(dataIni, dataFim, instituicoes, incluirBaixados);
            return Json(tabelas);
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
            // Regra de negocio: exibir SEMPRE todas as instituicoes cadastradas,
            // independente de periodo ou existencia de exames.
            var instituicoes = await _db.Instituicao
                .AsNoTracking()
                .Where(i => !string.IsNullOrEmpty(i.Sigla))
                .OrderBy(i => i.Sigla)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Sigla} - {i.Nome}"
                })
                .ToListAsync();

            _logger.LogInformation(
                "ObterInstituicoesAsync: total de instituicoes cadastradas={Total}",
                instituicoes.Count);

            return instituicoes;
        }

        private async Task<List<SelectListItem>> ObterTabelasAsync(DateTime dataIni, DateTime dataFim, List<int> instituicoes, bool incluirBaixados)
        {
            var (inicioUtc, _) = _geralController.ConverterDataLocalParaRangeUtc(dataIni);
            var (_, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(dataFim);

            _logger.LogInformation(
                "ObterTabelasAsync: periodo local {DataIni:dd/MM/yyyy} a {DataFim:dd/MM/yyyy} -> UTC {InicioUtc:dd/MM/yyyy HH:mm} a {FimUtc:dd/MM/yyyy HH:mm}, instituicoes=[{Instituicoes}], incluirBaixados={IncluirBaixados}",
                dataIni, dataFim, inicioUtc, fimUtc,
                instituicoes == null ? "null" : string.Join(",", instituicoes),
                incluirBaixados);

            // Diagnostico: quantos exames no periodo, ignorando instituicao/tabela/liberacao/baixado
            var totalExamesPeriodo = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: totalExamesRealizados no periodo={Total}", totalExamesPeriodo);

            var liberados = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
                .Where(e => e.Liberacao == 1)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: apos Liberacao==1 ={Total}", liberados);

            var naoBaixados = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
                .Where(e => e.Liberacao == 1)
                .Where(e => incluirBaixados || e.Baixado != 1)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: apos filtro Baixado ={Total}", naoBaixados);

            var comTabela = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
                .Where(e => e.Liberacao == 1)
                .Where(e => incluirBaixados || e.Baixado != 1)
                .Where(e => e.TabelaExames != null)
                .CountAsync();
            _logger.LogInformation("ObterTabelasAsync: apos TabelaExames!=null ={Total}", comTabela);

            var query = _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
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
                    .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
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
            var (inicioUtc, _) = _geralController.ConverterDataLocalParaRangeUtc(filtro.DataIni);
            var (_, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(filtro.DataFim);

            // Atualiza valores zerados antes da impressão
            if (filtro.MostragemPrecos != 2)
            {
                await AtualizarValoresZeradosAsync(inicioUtc, fimUtc, filtro.InstituicoesSelecionadas, filtro.TabelasSelecionadas);
            }

            // Seleciona exames (ativos + arquivados/baixados quando solicitado)
            var exames = await SelecionarExamesAsync(inicioUtc, fimUtc, filtro);

            var dados = new DadosPdfFaturamento
            {
                DataIni = filtro.DataIni,
                DataFim = filtro.DataFim,
                Ordenacao = filtro.Ordenacao,
                MostragemPrecos = filtro.MostragemPrecos
            };

            int sequencia = 0;
            foreach (var exame in exames)
            {
                sequencia++;
                var itens = await SelecionarItensAsync(exame, filtro);

                if (filtro.MostragemPrecos == 2)
                {
                    itens = itens.Where(i => i.ValorItem.HasValue && i.ValorItem.Value != 0).ToList();
                }

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
                    DataExame = exame.DataExame,
                    Itens = itens.Select(i => new ItemFaturamentoDto
                    {
                        Descricao = i.Descricao ?? "",
                        ValorItem = i.ValorItem ?? 0,
                        ClasseExamesNome = i.ClasseExamesNome ?? ""
                    }).ToList()
                };

                dados.Exames.Add(exameDto);
            }

            // Totais por instituição/tabela
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

            // Tabelas de preços utilizadas nos exames do relatório
            dados.TabelasUtilizadas = dados.Exames
                .Select(e => $"{e.SiglaTabela} - {e.NomeTabela}")
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return dados;
        }

        private async Task AtualizarValoresZeradosAsync(DateTime inicioUtc, DateTime fimUtc, List<int> instituicoes, List<int> tabelas)
        {
            var query = _db.ItensExamesRealizados
                .Where(i => i.ValorItem == 0 || i.ValorItem == null)
                .Where(i => i.ExamesRealizados.DataIni >= inicioUtc && i.ExamesRealizados.DataIni <= fimUtc)
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

        private async Task<List<ExameFaturamentoFonte>> SelecionarExamesAsync(DateTime inicioUtc, DateTime fimUtc, vmRelatorioFaturamento filtro)
        {
            var exames = new List<ExameFaturamentoFonte>();

            var totalNoPeriodo = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
                .CountAsync();

            var totalLiberadosNaoBaixados = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
                .Where(e => e.Liberacao == 1 && e.Baixado != 1)
                .CountAsync();

            _logger.LogInformation(
                "SelecionarExamesAsync: periodo UTC {InicioUtc:dd/MM/yyyy HH:mm} a {FimUtc:dd/MM/yyyy HH:mm}, totalNoPeriodo={TotalNoPeriodo}, liberadosNaoBaixados={LiberadosNaoBaixados}",
                inicioUtc, fimUtc, totalNoPeriodo, totalLiberadosNaoBaixados);

            var query = _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.TabelaExames)
                .Include(e => e.Pacientes)
                .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
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

            var amostraExames = await query
                .Take(20)
                .Select(e => new { e.Id, e.InstituicaoId, e.TabelaExamesId, e.DataIni, e.DataExame, e.Sequencial, e.Liberacao, e.Baixado })
                .ToListAsync();
            _logger.LogInformation(
                "SelecionarExamesAsync: amostra dos primeiros {Qtde} exames = {Detalhes}",
                amostraExames.Count,
                string.Join("; ", amostraExames.Select(e => $"Id={e.Id} Inst={e.InstituicaoId} Tab={e.TabelaExamesId} DataIni={e.DataIni:dd/MM/yyyy HH:mm} DataExame={e.DataExame:dd/MM/yyyy} Lib={e.Liberacao} Baix={e.Baixado}")));

            query = filtro.Ordenacao switch
            {
                0 => query.OrderBy(e => e.Pacientes.NomePaciente).ThenBy(e => e.Id),
                1 => query.OrderBy(e => e.Instituicao.Sigla).ThenBy(e => e.Sequencial),
                _ => query.OrderBy(e => e.DataIni).ThenBy(e => e.Instituicao.Sigla).ThenBy(e => e.Sequencial)
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
                    .Where(e => e.DataIni >= inicioUtc && e.DataIni <= fimUtc)
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
                        DataExame = e.DataExame,
                        OrigemAM = true
                    });
                }
            }

            // Reordena caso AM tenha sido incluído e ordenação seja por Data
            if (filtro.IncluirBaixados && filtro.Ordenacao == 2)
            {
                exames = exames
                    .OrderBy(e => e.DataExame)
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

        private async Task<List<ItemFaturamentoFonte>> SelecionarItensAsync(ExameFaturamentoFonte exame, vmRelatorioFaturamento filtro)
        {
            var itens = new List<ItemFaturamentoFonte>();

            if (!exame.OrigemAM)
            {
                var query = _db.ItensExamesRealizados
                    .AsNoTracking()
                    .Where(i => i.ExameRealizadoId == exame.Id)
                    .Where(i => i.TabelaExamesId == exame.TabelaExamesId)
                    .Where(i => !string.IsNullOrEmpty(i.Descricao))
                    .Where(i => !EF.Functions.Like(i.Descricao!.ToLower(), "exames%"))
                    .AsQueryable();

                if (filtro.MostragemPrecos == 2)
                    query = query.Where(i => i.ValorItem.HasValue && i.ValorItem.Value != 0);

                itens = await query
                    .OrderBy(i => i.OrdemItem)
                    .Select(i => new ItemFaturamentoFonte
                    {
                        Descricao = i.Descricao,
                        ValorItem = i.ValorItem,
                        ClasseExamesNome = i.ClasseExamesNome
                    })
                    .ToListAsync();
            }
            else
            {
                var query = _db.ItensExamesRealizadosAM
                    .AsNoTracking()
                    .Where(i => i.ExameRealizadoAMId == exame.Id)
                    .Where(i => i.TabelaExamesId == exame.TabelaExamesId)
                    .Where(i => !string.IsNullOrEmpty(i.Descricao))
                    .Where(i => !EF.Functions.Like(i.Descricao!.ToLower(), "exames%"))
                    .AsQueryable();

                if (filtro.MostragemPrecos == 2)
                    query = query.Where(i => i.ValorItem.HasValue && i.ValorItem.Value != 0);

                itens = await query
                    .OrderBy(i => i.OrdemItem)
                    .Select(i => new ItemFaturamentoFonte
                    {
                        Descricao = i.Descricao,
                        ValorItem = i.ValorItem,
                        ClasseExamesNome = i.ClasseExamesNome
                    })
                    .ToListAsync();
            }

            return itens;
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
