using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    //Feito pelo Kiro em 17/05/2026
    public class ConsultarExamesController : BaseController
    {
        private readonly IMemoryCache _cache;
        private readonly IGeralService _geralService;

        public ConsultarExamesController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IMemoryCache cache,
            IGeralService geralService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _cache = cache;
            _geralService = geralService;
        }

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames")]
        public IActionResult Index(
            string? dataExameDe,
            string? dataExamePara,
            string? nomePaciente,
            int? codigoExame,
            string? siglaInstituicao,
            string? nomeInstituicao,
            string? siglaPosto,
            string? nomePosto,
            string situacaoExame = "todos")
        {
            // Valida valores permitidos
            var opcoesValidas = new[] { "todos", "liberados", "naoLiberados", "pendentes", "baixados" };
            if (!opcoesValidas.Contains(situacaoExame))
                situacaoExame = "todos";

            // Defaults: DE = 15 dias atras, PARA = hoje
            DateTime hojeLocal = DateTime.Now.Date;
            DateTime dataDe = hojeLocal.AddDays(-15);
            DateTime dataPara = hojeLocal;

            if (!string.IsNullOrEmpty(dataExameDe))
                dataDe = dataExameDe.Trim().FormataData("dd/MM/yyyy", true);
            if (!string.IsNullOrEmpty(dataExamePara))
                dataPara = dataExamePara.Trim().FormataData("dd/MM/yyyy", true);

            //Feito pelo Qoder em 23/08/2026 — período máximo de consulta: 90 dias (contagem inclusiva)
            ViewBag.ErroPeriodo = ValidarPeriodoConsulta(dataDe, dataPara);

            ViewBag.DataExameDe = dataDe.ToString("dd/MM/yyyy");
            ViewBag.DataExamePara = dataPara.ToString("dd/MM/yyyy");
            ViewBag.SituacaoExame = situacaoExame;

            ViewBag.TextoMenu = new object[] { "Consultar Exames", false };
            return View(new vmConsultarExames());
        }

        /// <summary>
        /// Endpoint server-side do DataTables para a tela Consultar Exames.
        /// Carrega blocos de 100 registros do banco (cache de curta duração) e
        /// devolve a página solicitada de 10 em 10.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ConsultarExames/Listar")]
        public async Task<IActionResult> Listar(
            [FromForm] DataTableRequest request,
            string? dataExameDe,
            string? dataExamePara,
            string? nomePaciente,
            int? codigoExame,
            string? siglaInstituicao,
            string? nomeInstituicao,
            string? siglaPosto,
            string? nomePosto,
            string situacaoExame = "todos")
        {
            try
            {
                var opcoesValidas = new[] { "todos", "liberados", "naoLiberados", "pendentes", "baixados" };
                if (!opcoesValidas.Contains(situacaoExame))
                    situacaoExame = "todos";

                var (dataDe, dataPara, inicioDeUtc, fimParaUtc) = ObterDatasFiltro(dataExameDe, dataExamePara);

                //Feito pelo Qoder em 23/08/2026 — bloqueio server-side: período inválido não consulta
                if (ValidarPeriodoConsulta(dataDe, dataPara) != null)
                {
                    return Json(new DataTableResponse<object>
                    {
                        Draw = request.Draw,
                        RecordsTotal = 0,
                        RecordsFiltered = 0,
                        Data = new List<object>()
                    });
                }

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

                string cacheKey = BuildCacheKey(dataDe, dataPara, nomePaciente, codigoExame, siglaInstituicao, nomeInstituicao, siglaPosto, nomePosto, situacaoExame, searchValue, sortColumn, sortDir, blockIndex);

                if (!_cache.TryGetValue(cacheKey, out List<ConsultarExamesGridItem>? blockData) || blockData == null)
                {
                    blockData = await LoadBlockAsync(dataDe, dataPara, nomePaciente, codigoExame, siglaInstituicao, nomeInstituicao, siglaPosto, nomePosto, situacaoExame, searchValue, sortColumn, sortDir, blockStart, blockSize);
                    _cache.Set(cacheKey, blockData, TimeSpan.FromMinutes(5));
                }

                int recordsTotal = await CountTotalAsync(dataDe, dataPara, nomePaciente, codigoExame, siglaInstituicao, nomeInstituicao, siglaPosto, nomePosto, situacaoExame, searchValue);

                int skipInBlock = start - blockStart;
                var pageData = blockData.Skip(skipInBlock).Take(length).ToList();

                List<object> result = pageData.Select(item =>
                {
                    (string statusTexto, string statusCor) = ObterStatus(item);
                    return (object)new
                    {
                        id = item.Id,
                        siglaTabela = item.SiglaTabela,
                        siglaInstituicao = item.SiglaInstituicao,
                        nomeInstituicao = item.NomeInstituicao,
                        siglaPosto = item.SiglaPosto,
                        nomePosto = item.NomePosto,
                        nomePaciente = item.NomePaciente,
                        nascimento = item.Nascimento.FormataData(),
                        sequencial = item.Sequencial,
                        dataIni = item.DataIni.FormataData(),
                        liberacao = item.Liberacao,
                        baixado = item.Baixado,
                        situacaoExame = item.SituacaoExame,
                        statusTexto,
                        statusCor,
                        acoes = item.Faturado
                            ? $"<span title='Exame faturado — imexível'><i class='fa-solid fa-lock' style='color: #999;'></i></span>"
                            : $"<a id='{item.Id}' class='grid_itens' onclick=clickDeleteExame(this) title='Excluir'><i class='fa-sharp fa-solid fa-trash-can'></i> </a>"
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
                _eventLogHelper.LogEventViewer("[ConsultarExames] Listar - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        private (DateTime dataDe, DateTime dataPara, DateTime inicioDeUtc, DateTime fimParaUtc) ObterDatasFiltro(string? dataExameDe, string? dataExamePara)
        {
            DateTime hojeLocal = DateTime.Now.Date;
            DateTime dataDe = hojeLocal.AddDays(-15);
            DateTime dataPara = hojeLocal;

            if (!string.IsNullOrEmpty(dataExameDe))
                dataDe = dataExameDe.Trim().FormataData("dd/MM/yyyy", true);
            if (!string.IsNullOrEmpty(dataExamePara))
                dataPara = dataExamePara.Trim().FormataData("dd/MM/yyyy", true);

            // Feito pelo Qoder em 22/08/2026 — DataIni agora é DATE: retorna as datas locais diretamente (sem conversão UTC)
            return (dataDe.Date, dataPara.Date, dataDe.Date, dataPara.Date);
        }

        //Feito pelo Qoder em 23/08/2026 — regra de negócio: a consulta abrange no máximo 90 dias
        //(contagem inclusiva: o dia inicial e o final entram no total). Retorna a mensagem de erro ou null.
        private static string? ValidarPeriodoConsulta(DateTime dataDe, DateTime dataPara)
        {
            if (dataPara.Date < dataDe.Date)
                return "A data final (ATÉ) não pode ser anterior à data inicial (DE).";

            if ((dataPara.Date - dataDe.Date).Days + 1 > 90)
                return "O período máximo de consulta é de 90 dias. Reduza o intervalo entre as datas.";

            return null;
        }

        private IQueryable<int> ObterPendentesIdsQuery()
        {
            return _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ContaExame.Substring(i.ContaExame.Length - 4) != "0000")
                .Where(i => string.IsNullOrEmpty(i.Resultado))
                .Select(i => i.ExameRealizadoId)
                .Distinct();
        }

        private string BuildCacheKey(
            DateTime dataDe, DateTime dataPara,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, string? nomeInstituicao,
            string? siglaPosto, string? nomePosto,
            string situacaoExame, string searchValue,
            string sortColumn, string sortDir, int blockIndex)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            string raw = $"{dataDe:yyyyMMdd}|{dataPara:yyyyMMdd}|{nomePaciente?.ToLowerInvariant()}|{codigoExame}|{siglaInstituicao?.ToLowerInvariant()}|{nomeInstituicao?.ToLowerInvariant()}|{siglaPosto?.ToLowerInvariant()}|{nomePosto?.ToLowerInvariant()}|{situacaoExame}|{searchValue.ToLowerInvariant()}|{sortColumn}|{sortDir}|{blockIndex}";
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return "ConsultarExames_" + Convert.ToHexString(hash);
        }

        private async Task<List<ConsultarExamesGridItem>> LoadBlockAsync(
            DateTime dataDe, DateTime dataPara,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, string? nomeInstituicao,
            string? siglaPosto, string? nomePosto,
            string situacaoExame, string searchValue,
            string sortColumn, string sortDir,
            int blockStart, int blockSize)
        {
            IQueryable<ConsultarExamesGridItem> query = BuildBaseQuery(dataDe, dataPara, nomePaciente, codigoExame, siglaInstituicao, nomeInstituicao, siglaPosto, nomePosto, situacaoExame, searchValue);
            query = ApplyOrdering(query, sortColumn, sortDir);
            return await query.Skip(blockStart).Take(blockSize).ToListAsync();
        }

        private async Task<int> CountTotalAsync(
            DateTime dataDe, DateTime dataPara,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, string? nomeInstituicao,
            string? siglaPosto, string? nomePosto,
            string situacaoExame, string searchValue)
        {
            return await BuildBaseQuery(dataDe, dataPara, nomePaciente, codigoExame, siglaInstituicao, nomeInstituicao, siglaPosto, nomePosto, situacaoExame, searchValue).CountAsync();
        }

        private IQueryable<ConsultarExamesGridItem> BuildBaseQuery(
            DateTime dataDe, DateTime dataPara,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, string? nomeInstituicao,
            string? siglaPosto, string? nomePosto,
            string situacaoExame, string searchValue)
        {
            var (_, _, inicioDeUtc, fimParaUtc) = ObterDatasFiltro(dataDe.ToString("dd/MM/yyyy"), dataPara.ToString("dd/MM/yyyy"));
            var pendentesIds = ObterPendentesIdsQuery();

            IQueryable<ConsultarExamesGridItem> queryEr = _db.ExamesRealizados
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.Postos)
                .Include(e => e.Pacientes)
                .Include(e => e.TabelaExames)
                .Where(e => e.DataIni >= inicioDeUtc && e.DataIni <= fimParaUtc && e.Situacao >= 1)
                .Select(e => new ConsultarExamesGridItem
                {
                    Id = e.Id,
                    SiglaTabela = e.TabelaExames != null ? e.TabelaExames.SiglaTabela : "",
                    SiglaInstituicao = e.Instituicao != null ? e.Instituicao.Sigla : "",
                    NomeInstituicao = e.Instituicao != null ? e.Instituicao.Nome : "",
                    SiglaPosto = e.Postos != null ? e.Postos.SiglaPosto : "",
                    NomePosto = e.Postos != null ? e.Postos.NomePosto : "",
                    NomePaciente = e.Pacientes != null ? e.Pacientes.NomePaciente : "",
                    Nascimento = e.Pacientes != null ? e.Pacientes.Nascimento : DateTime.MinValue,
                    Sequencial = e.Sequencial,
                    DataIni = e.DataIni,
                    Liberacao = e.Liberacao,
                    Baixado = e.Baixado,
                    Faturado = e.Faturado,
                    SituacaoExame = situacaoExame
                });

            IQueryable<ConsultarExamesGridItem> queryAm = _db.ExamesRealizadosAM
                .AsNoTracking()
                .Include(e => e.Instituicao)
                .Include(e => e.Postos)
                .Include(e => e.Pacientes)
                .Include(e => e.TabelaExames)
                //Feito pelo Qoder em 15/08/2026 — item 5.4 do plano: oculta registros
                // baixados não enviados para análise (Situacao == 0).
                .Where(e => e.DataIni >= inicioDeUtc && e.DataIni <= fimParaUtc && e.Situacao >= 1)
                //..Qoder
                .Select(e => new ConsultarExamesGridItem
                {
                    Id = e.Id,
                    SiglaTabela = e.TabelaExames != null ? e.TabelaExames.SiglaTabela : "",
                    SiglaInstituicao = e.Instituicao != null ? e.Instituicao.Sigla : "",
                    NomeInstituicao = e.Instituicao != null ? e.Instituicao.Nome : "",
                    SiglaPosto = e.Postos != null ? e.Postos.SiglaPosto : "",
                    NomePosto = e.Postos != null ? e.Postos.NomePosto : "",
                    NomePaciente = e.Pacientes != null ? e.Pacientes.NomePaciente : "",
                    Nascimento = e.Pacientes != null ? e.Pacientes.Nascimento : DateTime.MinValue,
                    Sequencial = e.Sequencial,
                    DataIni = e.DataIni,
                    Liberacao = e.Liberacao,
                    Baixado = e.Baixado,
                    Faturado = e.Faturado,
                    SituacaoExame = situacaoExame
                });

            // Situação para ExamesRealizados
            queryEr = situacaoExame switch
            {
                "liberados" => queryEr.Where(e => e.Liberacao == 1 && e.Baixado == 0),
                "naoLiberados" => queryEr.Where(e => e.Liberacao == 0 && e.Baixado == 0 && !pendentesIds.Contains(e.Id)),
                "pendentes" => queryEr.Where(e => e.Liberacao == 0 && e.Baixado == 0 && pendentesIds.Contains(e.Id)),
                _ => queryEr
            };

            IQueryable<ConsultarExamesGridItem> query;
            if (situacaoExame == "baixados")
            {
                query = queryAm;
            }
            else if (situacaoExame == "todos")
            {
                query = queryEr.Concat(queryAm);
            }
            else
            {
                query = queryEr;
            }

            query = AplicarFiltrosBackend(query, nomePaciente, codigoExame, siglaInstituicao, nomeInstituicao, siglaPosto, nomePosto);

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(e =>
                    (e.SiglaTabela != null && e.SiglaTabela.Contains(searchValue)) ||
                    (e.SiglaInstituicao != null && e.SiglaInstituicao.Contains(searchValue)) ||
                    (e.NomeInstituicao != null && e.NomeInstituicao.Contains(searchValue)) ||
                    (e.SiglaPosto != null && e.SiglaPosto.Contains(searchValue)) ||
                    (e.NomePosto != null && e.NomePosto.Contains(searchValue)) ||
                    (e.NomePaciente != null && e.NomePaciente.Contains(searchValue)) ||
                    e.Id.ToString().Contains(searchValue));
            }

            return query;
        }

        private IQueryable<ConsultarExamesGridItem> AplicarFiltrosBackend(
            IQueryable<ConsultarExamesGridItem> query,
            string? nomePaciente, int? codigoExame,
            string? siglaInstituicao, string? nomeInstituicao,
            string? siglaPosto, string? nomePosto)
        {
            if (!string.IsNullOrEmpty(nomePaciente))
                query = query.Where(e => e.NomePaciente.ToLower().Contains(nomePaciente.Trim().ToLower()));

            if (codigoExame.HasValue)
                query = query.Where(e => e.Id == codigoExame.Value);

            if (!string.IsNullOrEmpty(siglaInstituicao))
                query = query.Where(e => e.SiglaInstituicao.ToLower().Contains(siglaInstituicao.Trim().ToLower()));

            if (!string.IsNullOrEmpty(nomeInstituicao))
                query = query.Where(e => e.NomeInstituicao.ToLower().Contains(nomeInstituicao.Trim().ToLower()));

            if (!string.IsNullOrEmpty(nomePosto))
                query = query.Where(e => e.NomePosto.ToLower().Contains(nomePosto.Trim().ToLower()));

            if (!string.IsNullOrEmpty(siglaPosto))
                query = query.Where(e => e.SiglaPosto.ToLower().Contains(siglaPosto.Trim().ToLower()));

            return query;
        }

        private IQueryable<ConsultarExamesGridItem> ApplyOrdering(IQueryable<ConsultarExamesGridItem> query, string sortColumn, string sortDir)
        {
            bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortColumn.ToLowerInvariant() switch
            {
                "siglatabela" => desc ? query.OrderByDescending(e => e.SiglaTabela) : query.OrderBy(e => e.SiglaTabela),
                "siglainstituicao" => desc ? query.OrderByDescending(e => e.SiglaInstituicao) : query.OrderBy(e => e.SiglaInstituicao),
                "nomeinstituicao" => desc ? query.OrderByDescending(e => e.NomeInstituicao) : query.OrderBy(e => e.NomeInstituicao),
                "siglaposto" => desc ? query.OrderByDescending(e => e.SiglaPosto) : query.OrderBy(e => e.SiglaPosto),
                "nomeposto" => desc ? query.OrderByDescending(e => e.NomePosto) : query.OrderBy(e => e.NomePosto),
                "nomepaciente" => desc ? query.OrderByDescending(e => e.NomePaciente) : query.OrderBy(e => e.NomePaciente),
                "sequencial" => desc ? query.OrderByDescending(e => e.Sequencial) : query.OrderBy(e => e.Sequencial),
                "dataini" => desc ? query.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id) : query.OrderBy(e => e.DataIni).ThenBy(e => e.Id),
                _ => desc ? query.OrderByDescending(e => e.DataIni).ThenByDescending(e => e.Id) : query.OrderBy(e => e.DataIni).ThenBy(e => e.Id)
            };
        }

        private static (string statusTexto, string statusCor) ObterStatus(ConsultarExamesGridItem item)
        {
            if (item.SituacaoExame == "todos")
            {
                if (item.Baixado == 1)
                    return ("Baixado", "gray");
                if (item.Liberacao == 1)
                    return ("Liberado", "green");
                return ("Não Liberado", "orange");
            }

            string statusTexto = item.SituacaoExame switch
            {
                "liberados" => "Liberado",
                "naoLiberados" => "Não Liberado",
                "pendentes" => "Pendente",
                "baixados" => "Baixado",
                _ => "-"
            };

            string statusCor = item.SituacaoExame switch
            {
                "liberados" => "green",
                "naoLiberados" => "orange",
                "pendentes" => "red",
                "baixados" => "gray",
                _ => "black"
            };

            return (statusTexto, statusCor);
        }
        //..Kiro

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames/ObterItensExame")]
        public async Task<IActionResult> ObterItensExame(int exameRealizadoId)
        {
            //Feito pelo Qoder em 15/08/2026 — Laboratório de Apoio (Fase 1 do plano):
            // valores do header usados como fallback quando o item não tiver sigla/controle.
            var header = await _db.ExamesRealizados
                .AsNoTracking()
                .Where(e => e.Id == exameRealizadoId)
                .Select(e => new { e.LaboratorioApoio, e.ControleApoio })
                .FirstOrDefaultAsync();
            //..Qoder

            var itensRaw = await _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ExameRealizadoId == exameRealizadoId)
                .OrderBy(i => i.OrdemItem)
                .Select(i => new
                {
                    i.Id,
                    i.ClasseExamesNome,
                    i.RefExame,
                    i.RefItem,
                    i.ContaExame,
                    i.Descricao,
                    i.ValorItem,
                    i.Etiquetas,
                    i.LaboratorioApoio,
                    i.ControleApoio
                })
                .ToListAsync();

            var itens = itensRaw.Select(i => new
            {
                i.Id,
                i.ClasseExamesNome,
                i.RefExame,
                i.RefItem,
                ContaExame = i.ContaExame.FormatarContaExameSem11(),
                i.Descricao,
                ValorItem = i.ValorItem.HasValue
                    ? i.ValorItem.Value.ToString("N2")
                    : "-",
                i.Etiquetas,
                //Feito pelo Qoder em 15/08/2026 — exibe sigla/controle do item,
                // com fallback para o header (registros antigos).
                LaboratorioApoio = string.IsNullOrEmpty(i.LaboratorioApoio)
                    ? (header != null ? header.LaboratorioApoio ?? "" : "")
                    : i.LaboratorioApoio,
                ControleApoio = string.IsNullOrEmpty(i.ControleApoio)
                    ? (header != null ? header.ControleApoio ?? "" : "")
                    : i.ControleApoio
                //..Qoder
            }).ToList();

            return Json(new { sucesso = true, itens });
        }
        //..Kiro

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames/ExcluirExame")]
        public async Task<IActionResult> ExcluirExame(int id)
        {
            var exame = await _db.ExamesRealizados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exame == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Exame não encontrado", action = "", sucesso = false });

            // Bloqueio: exame faturado não pode ser excluído
            if (exame.Faturado)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Exame está faturado e não pode ser excluído. Desbloqueie na tela de Manutenção de Faturamento.", action = "", sucesso = false });

            var itens = await _db.ItensExamesRealizados
                .AsNoTracking()
                .Where(i => i.ExameRealizadoId == id)
                .ToListAsync();

            bool possuiResultado = itens.Any(i => !string.IsNullOrEmpty(i.Resultado));

            if (possuiResultado)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Exame possui resultados lançados e não pode ser excluído", action = "", sucesso = false });

            //Feito pelo Qoder em 04/06/2026
            // Verificar se existem fichas vinculadas ao exame antes de permitir a exclusão
            bool possuiFichasInternas = await _db.FichasInternas.AnyAsync(f => f.ExamesRealizadosId == id);
            bool possuiFichasLotes = await _db.FichasLotes.AnyAsync(f => f.ExamesRealizadosId == id);
            bool possuiFichasPlanilhas = await _db.FichasPlanilhas.AnyAsync(f => f.ExamesRealizadosId == id);

            if (possuiFichasInternas || possuiFichasLotes || possuiFichasPlanilhas)
            {
                string mensagem = "Exame possui fichas vinculadas e não pode ser excluído. Verifique:";
                if (possuiFichasInternas) mensagem += " Fichas Internas;";
                if (possuiFichasLotes) mensagem += " Fichas de Lotes;";
                if (possuiFichasPlanilhas) mensagem += " Fichas de Planilhas;";
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = mensagem.TrimEnd(';'), action = "", sucesso = false });
            }
            //..Qoder

            var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var itensParaExcluir = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == id)
                    .ToListAsync();

                _db.ItensExamesRealizados.RemoveRange(itensParaExcluir);

                //Feito pelo Qoder em 12/08/2026 — removido bloco de exclusão de Requisitar (tabela eliminada)

                var exameParaExcluir = await _db.ExamesRealizados
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (exameParaExcluir != null)
                    _db.ExamesRealizados.Remove(exameParaExcluir);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Exame foi excluído com sucesso", action = "", sucesso = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _eventLogHelper.LogEventViewer("[ConsultarExames] ExcluirExame - Erro: " + ex.Message, "wError");
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Erro ao excluir exame", action = "", sucesso = false });
            }
        }
        //..Kiro

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarExames/ExcluirItemExame")]
        public async Task<IActionResult> ExcluirItemExame(int itemId)
        {
            var item = await _db.ItensExamesRealizados
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Item de exame não encontrado", action = "", sucesso = false });

            // Bloqueio: exame faturado não permite exclusão de itens
            var examePai = await _db.ExamesRealizados.AsNoTracking().FirstOrDefaultAsync(e => e.Id == item.ExameRealizadoId);
            if (examePai?.Faturado == true)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Exame está faturado. Itens não podem ser excluídos. Desbloqueie na tela de Manutenção de Faturamento.", action = "", sucesso = false });

            if (!string.IsNullOrEmpty(item.Resultado))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Este item possui resultado lançado e não pode ser excluído", action = "", sucesso = false });

            //Feito pelo Qoder em 12/08/2026 — removida verificação em Requisitar (tabela eliminada).
            // A existência do item em ItensExamesRealizados já é suficiente para permitir a exclusão.
            if (item == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Item de exame não encontrado.", action = "", sucesso = false });

            var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Excluir o item de ItensExamesRealizados
                var itemParaExcluir = await _db.ItensExamesRealizados
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (itemParaExcluir != null)
                    _db.ItensExamesRealizados.Remove(itemParaExcluir);

                //Feito pelo Qoder em 12/08/2026 — removida exclusão em Requisitar (tabela eliminada)

                await _db.SaveChangesAsync();

                // Reordenar os itens restantes do exame
                var itensRestantes = await _db.ItensExamesRealizados
                    .Where(i => i.ExameRealizadoId == item.ExameRealizadoId)
                    .OrderBy(i => i.OrdemItem)
                    .ToListAsync();

                int ordem = 1;
                foreach (var it in itensRestantes)
                {
                    it.OrdemItem = ordem++;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _eventLogHelper.LogEventViewer(
                    $"[ConsultarExames] ExcluirItemExame - Item Id={itemId}, ContaExame={item.ContaExame}, ExameRealizadoId={item.ExameRealizadoId} excluído com sucesso.",
                    "wInformation");

                return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Item de exame excluído com sucesso", action = "", sucesso = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _eventLogHelper.LogEventViewer("[ConsultarExames] ExcluirItemExame - Erro: " + ex.Message, "wError");
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Erro ao excluir item de exame", action = "", sucesso = false });
            }
        }
        //..Kiro
    }
    //..Kiro
}
