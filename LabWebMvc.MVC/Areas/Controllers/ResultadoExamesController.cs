using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    //Feito pelo Kiro em 06/06/2026
    public class ResultadoExamesController : BaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly IExameReferenciaCache _exameReferenciaCache;
        private readonly IMemoryCache _cache;
        private readonly IGeralService _geralService;

        public ResultadoExamesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IWebHostEnvironment env,
            IExameReferenciaCache exameReferenciaCache,
            IMemoryCache cache,
            IGeralService geralService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _env = env;
            _exameReferenciaCache = exameReferenciaCache;
            _cache = cache;
            _geralService = geralService;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ResultadoExames")]
        public IActionResult Index(
            string? dataInicial,
            string? dataFinal,
            string? nomePaciente,
            int? codigoExame,
            string? siglaInstituicao,
            int? status)
        {
            // Dados do grid carregados via AJAX pelo DataTables (server-side processing).
            ViewBag.TextoMenu = new object[] { "Resultado de Exames", false };
            return View();
        }

        /// <summary>
        /// Endpoint server-side do DataTables para a tela Resultado de Exames.
        /// Carrega blocos de 100 registros do banco (cache de curta duração) e
        /// devolve a página solicitada de 10 em 10.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ResultadoExames/Listar")]
        public async Task<IActionResult> Listar(
            [FromForm] DataTableRequest request,
            string? dataInicial,
            string? dataFinal,
            string? nomePaciente,
            int? codigoExame,
            string? siglaInstituicao,
            int? status)
        {
            try
            {
                int draw = request.Draw;
                int start = request.Start;
                int length = Math.Max(request.Length, 10);
                string searchValue = request.Search?.Value?.Trim() ?? string.Empty;

                const int blockSize = 100;
                int blockIndex = start / blockSize;
                int blockStart = blockIndex * blockSize;

                string sortColumn = request.Order.Count > 0 && request.Order[0].Column < request.Columns.Count
                    ? (request.Columns[request.Order[0].Column].Data ?? "id")
                    : "id";
                string sortDir = request.Order.Count > 0
                    ? (request.Order[0].Dir ?? "desc")
                    : "desc";

                string cacheKey = BuildCacheKey(dataInicial, dataFinal, nomePaciente, codigoExame, siglaInstituicao, status, searchValue, sortColumn, sortDir, blockIndex);

                if (!_cache.TryGetValue(cacheKey, out List<ResultadoExamesGridItem>? blockData) || blockData == null)
                {
                    blockData = await LoadBlockAsync(dataInicial, dataFinal, nomePaciente, codigoExame, siglaInstituicao, status, searchValue, sortColumn, sortDir, blockStart, blockSize);
                    _cache.Set(cacheKey, blockData, TimeSpan.FromMinutes(5));
                }

                int recordsTotal = await CountTotalAsync(dataInicial, dataFinal, nomePaciente, codigoExame, siglaInstituicao, status, searchValue);

                int skipInBlock = start - blockStart;
                var pageData = blockData.Skip(skipInBlock).Take(length).ToList();

                List<object> result = pageData.Select(item =>
                {
                    (string statusTexto, string statusCor) = ObterStatus(item.Situacao, item.TotalImpresso);
                    return (object)new
                    {
                        id = item.Id,
                        nomePaciente = item.NomePaciente,
                        siglaInstituicao = item.SiglaInstituicao,
                        siglaTabela = item.SiglaTabela,
                        nomePosto = item.NomePosto,
                        sequencial = item.Sequencial,
                        dataFim = item.DataFim?.ToLocalString("dd/MM/yyyy") ?? "-",
                        medico = (item.NomeMedico + " " + item.CRM).Trim(),
                        situacao = item.Situacao,
                        totalImpresso = item.TotalImpresso,
                        statusTexto,
                        statusCor
                    };
                }).ToList();

                return Json(new DataTableResponse<object>
                {
                    Draw = draw,
                    RecordsTotal = recordsTotal,
                    RecordsFiltered = recordsTotal,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ResultadoExames] Listar - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        private string BuildCacheKey(
            string? dataInicial, string? dataFinal,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, int? status,
            string searchValue, string sortColumn, string sortDir, int blockIndex)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            string raw = $"{dataInicial?.ToLowerInvariant()}|{dataFinal?.ToLowerInvariant()}|{nomePaciente?.ToLowerInvariant()}|{codigoExame}|{siglaInstituicao?.ToLowerInvariant()}|{status}|{searchValue.ToLowerInvariant()}|{sortColumn}|{sortDir}|{blockIndex}";
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return "ResultadoExames_" + Convert.ToHexString(hash);
        }

        private async Task<List<ResultadoExamesGridItem>> LoadBlockAsync(
            string? dataInicial, string? dataFinal,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, int? status,
            string searchValue, string sortColumn, string sortDir,
            int blockStart, int blockSize)
        {
            IQueryable<ResultadoExamesGridItem> query = BuildBaseQuery(dataInicial, dataFinal, nomePaciente, codigoExame, siglaInstituicao, status, searchValue);
            query = ApplyOrdering(query, sortColumn, sortDir);
            return await query.Skip(blockStart).Take(blockSize).ToListAsync();
        }

        private async Task<int> CountTotalAsync(
            string? dataInicial, string? dataFinal,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, int? status,
            string searchValue)
        {
            return await BuildBaseQuery(dataInicial, dataFinal, nomePaciente, codigoExame, siglaInstituicao, status, searchValue).CountAsync();
        }

        private IQueryable<ResultadoExamesGridItem> BuildBaseQuery(
            string? dataInicial, string? dataFinal,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, int? status,
            string searchValue)
        {
            IQueryable<ExamesRealizados> queryEr = _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.Postos)
                .Include(e => e.Pacientes)
                .Include(e => e.Medicos)
                .Include(e => e.TabelaExames)
                .Where(e => e.Liberacao == 0 && e.Baixado == 0 && e.Situacao >= 1);

            // Filtros backend
            if (!string.IsNullOrEmpty(dataInicial))
            {
                DateTime dataParsed = dataInicial.Trim().FormataData("dd/MM/yyyy", true);
                var (inicioUtc, _) = _geralService.ConverterDataLocalParaRangeUtc(dataParsed);
                queryEr = queryEr.Where(e => e.DataIni >= inicioUtc);
            }

            if (!string.IsNullOrEmpty(dataFinal))
            {
                DateTime dataParsed = dataFinal.Trim().FormataData("dd/MM/yyyy", true);
                var (_, fimUtc) = _geralService.ConverterDataLocalParaRangeUtc(dataParsed);
                queryEr = queryEr.Where(e => e.DataIni <= fimUtc);
            }

            if (!string.IsNullOrEmpty(nomePaciente))
                queryEr = queryEr.Where(e => e.Pacientes.NomePaciente.ToLower().Contains(nomePaciente.Trim().ToLower()));

            if (codigoExame.HasValue)
                queryEr = queryEr.Where(e => e.Id == codigoExame.Value);

            if (!string.IsNullOrEmpty(siglaInstituicao))
                queryEr = queryEr.Where(e => e.Instituicao.Sigla.ToLower().Contains(siglaInstituicao.Trim().ToLower()));

            if (status.HasValue)
                queryEr = queryEr.Where(e => e.Situacao == status.Value);

            IQueryable<ResultadoExamesGridItem> query = queryEr.Select(e => new ResultadoExamesGridItem
            {
                Id = e.Id,
                NomePaciente = e.Pacientes != null ? e.Pacientes.NomePaciente : "",
                SiglaInstituicao = e.Instituicao != null ? e.Instituicao.Sigla : "",
                SiglaTabela = e.TabelaExames != null ? e.TabelaExames.SiglaTabela : "",
                NomePosto = e.Postos != null ? (e.Postos.SiglaPosto ?? "") + "-" + (e.Postos.NomePosto ?? "") : "",
                Sequencial = e.Sequencial,
                DataFim = e.DataFim,
                NomeMedico = e.Medicos != null ? e.Medicos.NomeMedico : "",
                CRM = e.Medicos != null ? e.Medicos.CRM : "",
                Situacao = e.Situacao,
                TotalImpresso = e.TotalImpresso
            });

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(e =>
                    (e.NomePaciente != null && e.NomePaciente.ToLower().Contains(searchValue.ToLower())) ||
                    (e.SiglaInstituicao != null && e.SiglaInstituicao.ToLower().Contains(searchValue.ToLower())) ||
                    (e.SiglaTabela != null && e.SiglaTabela.ToLower().Contains(searchValue.ToLower())) ||
                    (e.NomePosto != null && e.NomePosto.ToLower().Contains(searchValue.ToLower())) ||
                    (e.NomeMedico != null && e.NomeMedico.ToLower().Contains(searchValue.ToLower())) ||
                    (e.CRM != null && e.CRM.ToLower().Contains(searchValue.ToLower())) ||
                    e.Id.ToString().Contains(searchValue) ||
                    e.Sequencial.ToString().Contains(searchValue));
            }

            return query;
        }

        private IQueryable<ResultadoExamesGridItem> ApplyOrdering(IQueryable<ResultadoExamesGridItem> query, string sortColumn, string sortDir)
        {
            bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortColumn.ToLowerInvariant() switch
            {
                "nomepaciente" => desc ? query.OrderByDescending(e => e.NomePaciente) : query.OrderBy(e => e.NomePaciente),
                "siglainstituicao" => desc ? query.OrderByDescending(e => e.SiglaInstituicao) : query.OrderBy(e => e.SiglaInstituicao),
                "siglatabela" => desc ? query.OrderByDescending(e => e.SiglaTabela) : query.OrderBy(e => e.SiglaTabela),
                "nomeposto" => desc ? query.OrderByDescending(e => e.NomePosto) : query.OrderBy(e => e.NomePosto),
                "sequencial" => desc ? query.OrderByDescending(e => e.Sequencial) : query.OrderBy(e => e.Sequencial),
                "datafim" => desc ? query.OrderByDescending(e => e.DataFim).ThenByDescending(e => e.Id) : query.OrderBy(e => e.DataFim).ThenBy(e => e.Id),
                "medico" => desc ? query.OrderByDescending(e => e.NomeMedico).ThenByDescending(e => e.CRM) : query.OrderBy(e => e.NomeMedico).ThenBy(e => e.CRM),
                _ => desc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id)
            };
        }

        private static (string statusTexto, string statusCor) ObterStatus(int situacao, int totalImpresso)
        {
            if (situacao == 1 && totalImpresso > 0)
                return ($"Impresso({totalImpresso}), Em Análise", "#CC6600");

            if (situacao == 3)
                return (totalImpresso > 0 ? $"Impresso({totalImpresso})" : "Impresso", "darkgreen");

            string statusTexto = situacao switch
            {
                0 => "Pendente",
                1 => "Em Análise",
                2 => "Liberado",
                5 => "A Repetir",
                6 => "Material Inválido",
                7 => "Pend. Cadastral",
                11 => "Baixando",
                _ => "Pendente"
            };

            string statusCor = situacao switch
            {
                0 => "red",
                1 => "blue",
                2 => "green",
                5 => "orange",
                6 => "darkred",
                7 => "gray",
                11 => "gray",
                _ => "red"
            };

            return (statusTexto, statusCor);
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

                var exame = await _db.ExamesRealizados.FindAsync(item.ExameRealizadoId);

                // Validação de intervalo numérico/percentual configurado no Plano de Exames
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

                item.Resultado = resultado?.Trim();

                if (exame != null)
                {
                    if (!string.IsNullOrWhiteSpace(resultado))
                    {
                        // Primeiro lançamento: Pendente → Em Análise
                        if (exame.Situacao == 0)
                            exame.Situacao = 1; // Em Análise
                    }
                    else
                    {
                        // Resultado apagado: se estava Impresso → volta para Em Análise
                        if (exame.Situacao == 3)
                            exame.Situacao = 1; // Em Análise (exame alterado após impressão)
                    }
                }

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Resultado salvo.", situacao = exame?.Situacao ?? 0, totalImpresso = exame?.TotalImpresso ?? 0 });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ResultadoExames] SalvarResultado - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar resultado." });
            }
        }
        //..Kiro

        //Feito pelo Kiro em 08/07/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ResultadoExames/LiberarExame")]
        public async Task<IActionResult> LiberarExame(int exameRealizadoId)
        {
            try
            {
                var exame = await _db.ExamesRealizados
                    .Where(e => e.Id == exameRealizadoId)
                    .FirstOrDefaultAsync();

                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado." });

                if (exame.Liberacao == 1)
                    return Json(new { sucesso = false, mensagem = "Este exame já está liberado." });

                var itens = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == exameRealizadoId
                             && i.ContaExame.Substring(4, 7) != "0000000")
                    .ToListAsync();

                var itensSemResultado = itens
                    .Where(i => i.ContaExame.Substring(i.ContaExame.Length - 4) != "0000")
                    .Where(i => string.IsNullOrWhiteSpace(i.Resultado))
                    .ToList();

                if (itensSemResultado.Count > 0)
                {
                    var faltando = string.Join(", ", itensSemResultado.Select(i => i.Descricao ?? i.ContaExame));
                    return Json(new { sucesso = false, mensagem = "Não é possível liberar. Resultado faltando em: " + faltando });
                }

                exame.Liberacao = 1;
                exame.DataFim = _geralService.ObterDataHoraUtc();
                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Exame liberado com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ResultadoExames] LiberarExame - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao liberar exame." });
            }
        }
        //..Kiro

        //Feito pelo Kiro em 19/06/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ResultadoExames/ImprimirResultado")]
        public async Task<IActionResult> ImprimirResultado(int exameRealizadoId)
        {
            try
            {
                // Carregar ExamesRealizados com Includes
                var exame = await _db.ExamesRealizados
                    .Include(e => e.Pacientes)
                    .Include(e => e.Medicos)
                    .Include(e => e.Instituicao)
                    .Include(e => e.Postos)
                    .Include(e => e.TabelaExames)
                    .Where(e => e.Id == exameRealizadoId)
                    .FirstOrDefaultAsync();

                if (exame == null)
                    return Json(new { sucesso = false, mensagem = "Exame não encontrado." });

                // Carregar itens (excluindo Folha geral: posições 5-11 = "0000000"), ordenar por ContaExame ASC
                var itens = await _db.ItensExamesRealizados
                    .Include(i => i.ClasseExames)
                    .Where(i => i.ExameRealizadoId == exameRealizadoId
                             && i.ContaExame.Substring(4, 7) != "0000000")
                    .OrderBy(i => i.ContaExame)
                    .ToListAsync();

                // Validar que todos os itens editáveis (últimos 4 dígitos > "0000") possuem Resultado preenchido
                var itensSemResultado = itens
                    .Where(i => i.ContaExame.Substring(i.ContaExame.Length - 4) != "0000")
                    .Where(i => string.IsNullOrWhiteSpace(i.Resultado))
                    .ToList();

                if (itensSemResultado.Count > 0)
                {
                    var faltando = string.Join(", ", itensSemResultado.Select(i => i.Descricao ?? i.ContaExame));
                    return Json(new { sucesso = false, mensagem = "Resultado faltando em: " + faltando });
                }

                // Carregar dados adicionais para o PDF profissional
                var empresa = await _db.Empresa.AsNoTracking().FirstOrDefaultAsync();
                var assinaturas = await _db.Assinaturas.AsNoTracking().FirstOrDefaultAsync();

                // Obter data/hora local para exibição no PDF
                var dataImpressaoLocal = _geralService.ObterDataHoraLocal();

                // Montar procedência: Paciente → Instituição → Empresa → em branco
                string procedencia = "";
                if (!string.IsNullOrWhiteSpace(exame.Pacientes?.Cidade) && !string.IsNullOrWhiteSpace(exame.Pacientes?.UF))
                    procedencia = exame.Pacientes.Cidade.TrimEnd() + "/" + exame.Pacientes.UF.TrimEnd();
                else if (!string.IsNullOrWhiteSpace(exame.Instituicao?.Cidade) && !string.IsNullOrWhiteSpace(exame.Instituicao?.UF))
                    procedencia = exame.Instituicao.Cidade.TrimEnd() + "/" + exame.Instituicao.UF.TrimEnd();
                else if (!string.IsNullOrWhiteSpace(empresa?.Cidade) && !string.IsNullOrWhiteSpace(empresa?.UF))
                    procedencia = empresa.Cidade!.TrimEnd() + "/" + empresa.UF!.TrimEnd();

                // Formatar ControleApoio: yyyy.MM.dd-NNNN
                string controleFormatado = FormatarControleApoio(exame.ControleApoio);

                // Formatar Sequencial: 000.000
                string sequencialFormatado = FormatarSequencial(exame.Sequencial);

                // Montar endereço da instituição
                string enderecoInst = MontarEnderecoInstituicao(exame.Instituicao);

                // Montar lista de assinaturas ativas
                var listaAssinaturas = MontarAssinaturas(assinaturas);

                // Carregar flag de gráfico do PlanoExames para cada item (por ContaExame + TabelaExamesId)
                var contasExame = itens.Select(i => i.ContaExame).Distinct().ToList();
                var planoExamesDict = await _db.PlanoExames
                    .AsNoTracking()
                    .Where(p => p.TabelaExamesId == exame.TabelaExamesId && contasExame.Contains(p.ContaExame))
                    .ToDictionaryAsync(
                        p => p.ContaExame,
                        p => new { p.GraficoNoItem });

                // Montar DTO para o helper
                var dadosPdf = new DadosPdfResultado
                {
                    // Empresa (cabeçalho/timbre do laudo — sempre da tabela Empresa)
                    TituloEmpresa = empresa?.TituloEmpresa ?? "LABORATÓRIO",
                    SubTituloEmpresa = empresa?.SubTituloEmpresa ?? "",
                    EnderecoEmpresa = MontarEnderecoEmpresa(empresa),
                    TelefoneEmpresa = empresa?.Telefones ?? "",
                    EmailEmpresa = empresa?.Email ?? "",

                    // Instituição (logo + dados para convênio origem)
                    LogoInstituicao = exame.Instituicao?.Logomarca,
                    NomeInstituicao = exame.Instituicao?.Nome ?? "",
                    SiglaInstituicao = exame.Instituicao?.Sigla ?? "",

                    // Paciente
                    PacienteId = exame.PacienteId,
                    NomePaciente = exame.Pacientes?.NomePaciente ?? "",
                    Nascimento = exame.Pacientes?.Nascimento,
                    Sexo = exame.Pacientes?.Sexo ?? "",
                    Procedencia = procedencia,

                    // Médico
                    NomeMedico = exame.Medicos?.NomeMedico ?? "",

                    // Exame header
                    ExameId = exame.Id,
                    ControleApoioFormatado = controleFormatado,
                    SequencialFormatado = sequencialFormatado,
                    DataExameColeta = exame.DataIni.ToLocalString("dd/MM/yyyy"),
                    DataLaudoLiberado = exame.DataFim?.ToLocalString("dd/MM/yyyy") ?? dataImpressaoLocal.ToString("dd/MM/yyyy"),
                    DataImpressao = dataImpressaoLocal.ToString("dd/MM/yyyy"),
                    HoraImpressao = dataImpressaoLocal.ToString("HH:mm"),

                    // Itens
                    Itens = itens.Select(i => new ItemPdfResultado
                    {
                        ContaExame = i.ContaExame,
                        Folha = i.ClasseExames?.RefExame ?? "",
                        Descricao = i.Descricao ?? "",
                        Resultado = i.Resultado ?? "",
                        UnidadeMedida = i.UnidadeMedida ?? "",
                        Referencia = i.Referencia ?? "",
                        EhPrincipal = i.ContaExame.Length >= 11
                            && i.ContaExame.Substring(i.ContaExame.Length - 4) == "0000"
                            && i.ContaExame.Substring(4, 3) != "000"
                    }).ToList(),

                    // Assinaturas
                    Assinaturas = listaAssinaturas
                };

                //Feito pelo Kiro em 11/07/2025
                // Montar referências do cache para o DTO
                var referenciasDic = new Dictionary<string, List<ExameReferenciaItem>>();
                foreach (var conta in contasExame)
                {
                    var refs = _exameReferenciaCache.ObterReferencias(conta);
                    if (refs != null && refs.Count > 0)
                        referenciasDic[conta] = refs;
                }
                if (referenciasDic.Count > 0)
                    dadosPdf.ReferenciasPorContaExame = referenciasDic;
                //..Kiro

                // Montar histórico evolutivo dos itens configurados para exibir gráfico
                var itensGrafico = itens
                    .Where(i => planoExamesDict.TryGetValue(i.ContaExame, out var plano)
                             && plano.GraficoNoItem == 1)
                    .ToList();

                if (itensGrafico.Count > 0)
                {
                    var historicoDict = new Dictionary<string, List<DadoHistoricoExame>>();
                    var fusoLocal = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

                    foreach (var itemGrafico in itensGrafico)
                    {
                        var anteriores = await _db.ItensExamesRealizados
                            .AsNoTracking()
                            .Include(i => i.ExamesRealizados)
                            .Where(i => i.PacienteId == exame.PacienteId
                                     && i.ContaExame == itemGrafico.ContaExame
                                     && i.ExameRealizadoId != exame.Id
                                     && i.ExamesRealizados.DataIni <= exame.DataIni)
                            .OrderByDescending(i => i.ExamesRealizados.DataIni)
                            .Take(5)
                            .Select(i => new { i.Resultado, i.ExamesRealizados.DataIni })
                            .ToListAsync();

                        var pontos = anteriores
                            .Select(h => new { h.DataIni, Valor = ParseValorResultado(h.Resultado) })
                            .Where(h => h.Valor.HasValue)
                            .ToList();

                        var valorAtual = ParseValorResultado(itemGrafico.Resultado);
                        if (valorAtual.HasValue)
                            pontos.Add(new { DataIni = exame.DataIni, Valor = valorAtual });

                        var pontosOrdenados = pontos
                            .OrderBy(p => p.DataIni)
                            .Take(6)
                            .Select(p => new DadoHistoricoExame
                            {
                                DataExame = TimeZoneInfo.ConvertTimeFromUtc(p.DataIni, fusoLocal),
                                Valor = p.Valor!.Value
                            })
                            .ToList();

                        if (pontosOrdenados.Count >= 2)
                            historicoDict[itemGrafico.ContaExame] = pontosOrdenados;
                    }

                    if (historicoDict.Count > 0)
                        dadosPdf.HistoricoPorContaExame = historicoDict;
                }

                // Gerar PDF via helper (PdfSharpCore - MIT)
                var geradorPdf = new GeradorPdfResultado();
                byte[] pdfBytes = geradorPdf.Gerar(dadosPdf);

                // Salvar PDF em disco
                string cnpjDigitos = empresa?.CNPJ?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "00000000000000";
                string anoMes = dataImpressaoLocal.ToString("yyyyMM");
                string diretorioPdf = System.IO.Path.Combine(_env.ContentRootPath, "App_Data", "Resultados", cnpjDigitos, anoMes);
                Directory.CreateDirectory(diretorioPdf);

                string caminhoArquivo = System.IO.Path.Combine(diretorioPdf, exame.Id + ".pdf");
                await System.IO.File.WriteAllBytesAsync(caminhoArquivo, pdfBytes);

                // Atualizar ExamesRealizados: DataEntrega, Situacao = 3, TotalImpresso += 1
                exame.DataEntrega = _geralService.ObterDataHoraUtc();
                exame.Situacao = 3;
                exame.TotalImpresso += 1;
                await _db.SaveChangesAsync();

                return File(pdfBytes, "application/pdf", "Resultado_" + exame.Id + ".pdf");
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ResultadoExames] ImprimirResultado - Erro: " + ex.Message + " | StackTrace: " + ex.StackTrace, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao gerar PDF do resultado. Detalhe: " + ex.Message });
            }
        }

        #region Métodos auxiliares para ImprimirResultado

        /// <summary>
        /// Formata ControleApoio para exibição: yyyy.MM.dd-NNNN
        /// </summary>
        private static string FormatarControleApoio(string? controleApoio)
        {
            if (string.IsNullOrWhiteSpace(controleApoio) || controleApoio.Length < 12)
                return controleApoio ?? "";

            // Formato: primeiros 4 + "." + chars 5-6 + "." + chars 7-8 + "-" + últimos 4
            return controleApoio.Substring(0, 4) + "." +
                   controleApoio.Substring(4, 2) + "." +
                   controleApoio.Substring(6, 2) + "-" +
                   controleApoio.Substring(controleApoio.Length - 4);
        }

        /// <summary>
        /// Formata Sequencial como 000.000
        /// </summary>
        private static string FormatarSequencial(int sequencial)
        {
            string seq = sequencial.ToString("D6"); // 6 dígitos com zeros à esquerda
            return seq.Substring(0, 3) + "." + seq.Substring(3, 3);
        }

        /// <summary>
        /// Monta endereço completo da instituição para o cabeçalho do PDF.
        /// </summary>
        private static string MontarEnderecoInstituicao(Models.Instituicao? inst)
        {
            if (inst == null) return "";

            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(inst.Logradouro))
                partes.Add(inst.Logradouro.TrimEnd());
            if (!string.IsNullOrWhiteSpace(inst.Endereco))
                partes.Add(inst.Endereco.TrimEnd());
            if (!string.IsNullOrWhiteSpace(inst.Complemento))
                partes.Add(inst.Complemento.TrimEnd());

            string linha1 = string.Join(" ", partes);

            var partes2 = new List<string>();
            if (!string.IsNullOrWhiteSpace(inst.Bairro))
                partes2.Add(inst.Bairro.TrimEnd());
            if (!string.IsNullOrWhiteSpace(inst.Cidade))
            {
                string cidadeUf = inst.Cidade.TrimEnd();
                if (!string.IsNullOrWhiteSpace(inst.UF))
                    cidadeUf += "/" + inst.UF.TrimEnd();
                partes2.Add(cidadeUf);
            }

            string linha2 = string.Join(" - ", partes2);

            if (!string.IsNullOrWhiteSpace(linha1) && !string.IsNullOrWhiteSpace(linha2))
                return linha1 + " - " + linha2;
            return linha1 + linha2;
        }

        /// <summary>
        /// Monta endereço completo da empresa para o cabeçalho do PDF.
        /// </summary>
        private static string MontarEnderecoEmpresa(Models.Empresa? emp)
        {
            if (emp == null) return "";

            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(emp.Logradouro))
                partes.Add(emp.Logradouro.TrimEnd());
            if (!string.IsNullOrWhiteSpace(emp.Endereco))
                partes.Add(emp.Endereco.TrimEnd());
            if (!string.IsNullOrWhiteSpace(emp.Numero))
                partes.Add(emp.Numero.TrimEnd());
            if (!string.IsNullOrWhiteSpace(emp.Complemento))
                partes.Add(emp.Complemento.TrimEnd());

            string linha1 = string.Join(" ", partes);

            var partes2 = new List<string>();
            if (!string.IsNullOrWhiteSpace(emp.Bairro))
                partes2.Add(emp.Bairro.TrimEnd());
            if (!string.IsNullOrWhiteSpace(emp.Cidade))
            {
                string cidadeUf = emp.Cidade.TrimEnd();
                if (!string.IsNullOrWhiteSpace(emp.UF))
                    cidadeUf += "/" + emp.UF.TrimEnd();
                partes2.Add(cidadeUf);
            }

            string linha2 = string.Join(" - ", partes2);

            if (!string.IsNullOrWhiteSpace(linha1) && !string.IsNullOrWhiteSpace(linha2))
                return linha1 + " - " + linha2;
            return linha1 + linha2;
        }

        /// <summary>
        /// Monta lista de assinaturas ativas (Usar{N} == 1) a partir da tabela Assinaturas.
        /// </summary>
        private static List<AssinaturaPdf> MontarAssinaturas(Models.Assinaturas? assinaturas)
        {
            var lista = new List<AssinaturaPdf>();
            if (assinaturas == null) return lista;

            if (assinaturas.Usar1 == 1)
                lista.Add(new AssinaturaPdf { ImagemAssinatura = assinaturas.Assinatura1, Credenciais = assinaturas.Crbio1 ?? "" });
            if (assinaturas.Usar2 == 1)
                lista.Add(new AssinaturaPdf { ImagemAssinatura = assinaturas.Assinatura2, Credenciais = assinaturas.Crbio2 ?? "" });
            if (assinaturas.Usar3 == 1)
                lista.Add(new AssinaturaPdf { ImagemAssinatura = assinaturas.Assinatura3, Credenciais = assinaturas.Crbio3 ?? "" });
            if (assinaturas.Usar4 == 1)
                lista.Add(new AssinaturaPdf { ImagemAssinatura = assinaturas.Assinatura4, Credenciais = assinaturas.Crbio4 ?? "" });

            return lista;
        }

        #endregion
        //..Kiro

        //Feito pelo Kiro em 14/06/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ResultadoExames/BaixarExame")]
        public async Task<IActionResult> BaixarExame(int exameRealizadoId)
        {
            // Carregar exame COM tracking (precisa modificar a entidade)
            var exame = await _db.ExamesRealizados
                .Where(e => e.Id == exameRealizadoId)
                .FirstOrDefaultAsync();

            if (exame == null)
                return Json(new { sucesso = false, mensagem = "Exame não encontrado." });

            // Só permite baixar para Arquivo-Morto se o laudo já foi impresso ao menos 1 vez
            if (exame.TotalImpresso < 1)
                return Json(new { sucesso = false, mensagem = "Este exame ainda não foi impresso. É necessário imprimir o laudo pelo menos uma vez antes de baixar para Arquivo-Morto." });

            // Proteção de concorrência: se já está sendo baixado por outro terminal
            if (exame.Situacao == 11)
                return Json(new { sucesso = false, mensagem = "Exame está sendo baixado por outro terminal. Aguarde." });

            // Guardar situação anterior para restauração em caso de falha
            int situacaoAnterior = exame.Situacao;

            // Lock imediato: marcar Situacao = 11 FORA da transação
            exame.Situacao = 11;
            await _db.SaveChangesAsync();

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Carregar todos os itens do exame (com tracking)
                var itens = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == exameRealizadoId)
                    .ToListAsync();

                // Criar ExamesRealizadosAM mapeando campos
                var novoAM = new ExamesRealizadosAM
                {
                    OrigemId = exame.Id,
                    PacienteId = exame.PacienteId,
                    TabelaExamesId = exame.TabelaExamesId,
                    InstituicaoId = exame.InstituicaoId,
                    PostoId = exame.PostoId,
                    MedicoId = exame.MedicoId,
                    Sequencial = exame.Sequencial,
                    LaboratorioApoio = exame.LaboratorioApoio,
                    ControleApoio = exame.ControleApoio,
                    HistoricoClinico = exame.HistoricoClinico,
                    ExameColado = exame.ExameColado,
                    ExameColadoImagens = exame.ExameColadoImagens,
                    TravaColado = exame.TravaColado,
                    DataIni = exame.DataIni,
                    DataFim = exame.DataFim,
                    Liberacao = exame.Liberacao,
                    DataExame = exame.DataExame,
                    DataColeta = exame.DataColeta,
                    DataEntrega = exame.DataEntrega,
                    Baixado = 1,
                    EnviarEmail = exame.EnviarEmail,
                    Situacao = 4,
                    TotalImpresso = exame.TotalImpresso,
                    Faturado = exame.Faturado
                };

                _db.ExamesRealizadosAM.Add(novoAM);
                await _db.SaveChangesAsync();

                // Para cada item: criar ItensExamesRealizadosAM
                var itensAM = new List<ItensExamesRealizadosAM>();
                foreach (var item in itens)
                {
                    itensAM.Add(new ItensExamesRealizadosAM
                    {
                        OrigemAmid = item.Id,
                        PacienteId = item.PacienteId,
                        ClasseExamesId = item.ClasseExamesId,
                        ClasseExamesNome = item.ClasseExamesNome,
                        ExameRealizadoAMId = novoAM.Id,
                        TabelaExamesId = item.TabelaExamesId,
                        OrdemItem = item.OrdemItem,
                        RefExame = item.RefExame,
                        RefItem = item.RefItem,
                        ContaExame = item.ContaExame,
                        CitoTituloFolha = item.CitoTituloFolha,
                        CitoTituloExame = item.CitoTituloExame,
                        CitoRefItem = item.CitoRefItem,
                        InstituicaoId = item.InstituicaoId,
                        Sequencial = item.Sequencial,
                        LaboratorioApoio = item.LaboratorioApoio,
                        ControleApoio = item.ControleApoio,
                        LaboratorioExterno = item.LaboratorioExterno,
                        MaterialSaida = item.MaterialSaida,
                        MaterialRetorno = item.MaterialRetorno,
                        Descricao = item.Descricao,
                        CitoDescricao = item.CitoDescricao,
                        Resultado = item.Resultado,
                        UnidadeMedida = item.UnidadeMedida,
                        Referencia = item.Referencia,
                        ValorItem = item.ValorItem,
                        Laudo = item.Laudo,
                        Etiquetas = item.Etiquetas,
                        DataEntregaParcial = item.DataEntregaParcial,
                        Liberado = item.Liberado,
                        Baixado = 1
                    });
                }

                _db.ItensExamesRealizadosAM.AddRange(itensAM);
                await _db.SaveChangesAsync();

                // Excluir itens originais
                _db.ItensExamesRealizados.RemoveRange(itens);

                // Excluir header original
                _db.ExamesRealizados.Remove(exame);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { sucesso = true, mensagem = "Exame baixado para Arquivo-Morto com sucesso." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Restaurar Situacao anterior (re-query necessário pois a transação foi rollback)
                try
                {
                    var exameReload = await _db.ExamesRealizados.FindAsync(exameRealizadoId);
                    if (exameReload != null)
                    {
                        exameReload.Situacao = situacaoAnterior;
                        await _db.SaveChangesAsync();
                    }
                }
                catch (Exception exRestore)
                {
                    _eventLogHelper.LogEventViewer("[ResultadoExames] BaixarExame - Erro ao restaurar Situacao: " + exRestore.Message, "wError");
                }

                _eventLogHelper.LogEventViewer("[ResultadoExames] BaixarExame - Erro: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao baixar exame para Arquivo-Morto." });
            }
        }
        //..Kiro

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
    }
    //..Kiro
}
