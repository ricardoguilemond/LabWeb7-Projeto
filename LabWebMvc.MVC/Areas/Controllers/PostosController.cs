using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ExpressionCombiner;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class PostosController : BaseController
    {
        private readonly IMemoryCache _cache;

        public PostosController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IMemoryCache cache)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _cache = cache;
        }

        private void MontaControllers(string action, string controller, string parametros = "")
        {
            PartialFiltro.Action = action;
            PartialFiltro.Controller = controller;
            PartialFiltro.ActionButton = action + parametros;
            PartialFiltro.ControllerButton = controller;
            PartialFiltro.Esconde = false;
            ViewBag.TextoMenu = action.MensagemStartUp();
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Postos")]
        public IActionResult Index()
        {
            //monta o controller para chamada dos itens que estão em _PartialMenuPostos.cshtml
            MontaControllers("IncluirPostos", "Postos");

            // Dados do grid carregados via AJAX pelo DataTables (server-side processing).
            ViewBag.TextoMenu = new object[] { "Cadastro de Postos de Coletas e Anexos", false };
            return View(new vmPostos());
        }

        /// <summary>
        /// Endpoint server-side do DataTables para o cadastro de postos/anexos.
        /// Carrega blocos de 100 registros do banco (cache de curta duração) e
        /// devolve a página solicitada de 10 em 10.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Postos/Listar")]
        public async Task<IActionResult> Listar([FromForm] DataTableRequest request)
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

                string cacheKey = BuildCacheKey(searchValue, sortColumn, sortDir, blockIndex);

                if (!_cache.TryGetValue(cacheKey, out List<Postos>? blockData) || blockData == null)
                {
                    blockData = await LoadBlockAsync(searchValue, sortColumn, sortDir, blockStart, blockSize);
                    _cache.Set(cacheKey, blockData, TimeSpan.FromMinutes(5));
                }

                int recordsTotal = await CountTotalAsync(searchValue);

                int skipInBlock = start - blockStart;
                var pageData = blockData.Skip(skipInBlock).Take(length).ToList();

                List<object> result = pageData.Select(item => (object)new
                {
                    id = item.Id,
                    siglaInstituicao = item.Instituicao?.Sigla ?? string.Empty,
                    nomeInstituicao = item.Instituicao?.Nome ?? string.Empty,
                    siglaPosto = item.SiglaPosto ?? string.Empty,
                    nomePosto = item.NomePosto ?? string.Empty,
                    responsavel = item.Responsavel.ToCapitalizeNotNull(),
                    telefone = item.Telefone?.FormataTelefoneNotNull() ?? string.Empty,
                    acoes = BuildAcoes(item.Id)
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
                _eventLogHelper.LogEventViewer("[Postos] Listar - Erro: " + ex.Message, "wError");
                return Json(new DataTableResponse<object>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<object>()
                });
            }
        }

        private string BuildCacheKey(string searchValue, string sortColumn, string sortDir, int blockIndex)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            string raw = $"{searchValue.ToLowerInvariant()}|{sortColumn}|{sortDir}|{blockIndex}";
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return "Postos_" + Convert.ToHexString(hash);
        }

        private async Task<List<Postos>> LoadBlockAsync(string searchValue, string sortColumn, string sortDir, int blockStart, int blockSize)
        {
            IQueryable<Postos> query = BuildBaseQuery(searchValue);
            query = ApplyOrdering(query, sortColumn, sortDir);
            return await query.Skip(blockStart).Take(blockSize).ToListAsync();
        }

        private async Task<int> CountTotalAsync(string searchValue)
        {
            return await BuildBaseQuery(searchValue).CountAsync();
        }

        private IQueryable<Postos> BuildBaseQuery(string searchValue)
        {
            IQueryable<Postos> query = _db.Postos.AsNoTracking().Include(p => p.Instituicao);

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.FiltrarPorConteudo(searchValue,
                    x => x.SiglaPosto,
                    x => x.NomePosto,
                    x => x.Endereco,
                    x => x.Bairro,
                    x => x.Cidade,
                    x => x.Id.ToString());
            }

            return query;
        }

        private IQueryable<Postos> ApplyOrdering(IQueryable<Postos> query, string sortColumn, string sortDir)
        {
            bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortColumn.ToLowerInvariant() switch
            {
                "siglainstituicao" => desc ? query.OrderByDescending(p => p.Instituicao!.Sigla) : query.OrderBy(p => p.Instituicao!.Sigla),
                "nomeinstituicao" => desc ? query.OrderByDescending(p => p.Instituicao!.Nome) : query.OrderBy(p => p.Instituicao!.Nome),
                "siglaposto" => desc ? query.OrderByDescending(p => p.SiglaPosto) : query.OrderBy(p => p.SiglaPosto),
                "nomeposto" => desc ? query.OrderByDescending(p => p.NomePosto) : query.OrderBy(p => p.NomePosto),
                "responsavel" => desc ? query.OrderByDescending(p => p.Responsavel) : query.OrderBy(p => p.Responsavel),
                "telefone" => desc ? query.OrderByDescending(p => p.Telefone) : query.OrderBy(p => p.Telefone),
                _ => desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id)
            };
        }

        private static string BuildAcoes(int id)
        {
            return $"<a id='{id}' class='grid_itens' onclick=clickConsulta(this) title='Consultar'><i class='fa-sharp fa-solid fa-display'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickAlterar(this) title='Alterar'><i class='fa-sharp fa-solid fa-file-pen'></i> </a>" +
                   $"<a id='{id}' class='grid_itens' onclick=clickDelete(this) title='Excluir'><i class='fa-sharp fa-solid fa-trash-can'></i> </a>";
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("IncluirPostos")]
        public async Task<IActionResult> IncluirPostos()
        {
            //Feito pelo Qoder em 21/04/2026 - lista de Instituicoes para o select
            var instituicoes = await _db.Instituicao.AsNoTracking()
                .OrderBy(i => i.Sigla)
                .Select(i => new { i.Id, i.Sigla, i.Nome })
                .ToListAsync();

            var vm = new vmPostos
            {
                InstituicoesSigla = instituicoes
                    .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Sigla })
                    .ToList(),
                InstituicoesNome = instituicoes
                    .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Nome })
                    .ToList(),
                SessionUF = HttpContext.Session.GetString("SessionUF") ?? ""
            };
            //..Qoder

            //Finalização da View
            ViewBag.TextoMenu = new object[] { "Cadastro de Postos de Coletas e Anexos", false };
            return View(vm);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirPostos")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> SalvarPostos(vmPostos vm)
        {
            string redirecionaUrl = "Postos".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.NomePosto))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios ou não havia nada para ser salvo" });

            //Feito pelo Qoder em 21/04/2026 - validação da Instituicao informada
            if (vm.InstituicaoId <= 0 || !await _db.Instituicao.AnyAsync(i => i.Id == vm.InstituicaoId))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição informada é inválida", action = "", sucesso = false });

            string siglaNormalizada = GenericValidations.NormalizarSigla(vm.SiglaPosto);
            if (string.IsNullOrWhiteSpace(siglaNormalizada))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Sigla do posto é obrigatória", action = "", sucesso = false });
            //..Qoder

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        //Feito pelo Qoder em 21/04/2026 - aproveita o primeiro Id vago na sequência
                        var idsUsados = await _db.Postos.AsNoTracking().Select(p => p.Id).ToListAsync();
                        int proximoId = 1;
                        while (idsUsados.Contains(proximoId)) proximoId++;
                        //..Qoder

                        await _db.Postos.AddAsync(new Postos()
                        {
                            Id = proximoId,
                            //Colunas NÃO nulas:
                            InstituicaoId = vm.InstituicaoId,
                            SiglaPosto = siglaNormalizada,
                            NomePosto = vm.NomePosto.ToUpper(),
                            Responsavel = vm.Responsavel,

                            //Colunas que aceitam nulas:
                            Telefone = vm.Telefone.ApenasNumeros(),
                            Endereco = vm.Endereco.ToCapitalize(),
                            Logradouro = vm.Logradouro.ToCapitalize(),
                            Numero = vm.Numero,
                            Bairro = vm.Bairro.ToCapitalize(),
                            Complemento = vm.Complemento,
                            Cidade = vm.Cidade.ToCapitalize(),
                            UF = vm.vmGeral.TipoUF,
                            CEP = vm.CEP
                        });

                        await _db.SaveChangesWithSyncAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Posto foi salvo", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Postos] Erro ao salvar Posto/anexo: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto NÃO foi salvo", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarPostos")]
        public async Task<IActionResult> AlterarPostos(vmPostos vm, int id)
        {
            Postos dados = await _db.Postos.Where(c => c.Id == id).Include(p => p.Instituicao).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.InstituicaoId = dados.InstituicaoId;
                vm.SiglaInstituicao = dados.Instituicao?.Sigla;
                vm.NomeInstituicao = dados.Instituicao?.Nome;
                vm.SiglaPosto = dados.SiglaPosto;
                vm.NomePosto = dados.NomePosto.ToUpper();
                vm.Logradouro = dados.Logradouro.ToCapitalize();
                vm.Endereco = dados.Endereco.ToCapitalize();
                vm.Numero = dados.Numero;
                vm.Bairro = dados.Bairro.ToCapitalize();
                vm.Complemento = dados.Complemento;
                vm.Cidade = dados.Cidade.ToCapitalize();
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone.FormataTelefone();
                vm.Responsavel = dados.Responsavel;
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis via ViewModel tipado
                 */
                vm.SessionUF = dados.UF;
            }

            //Feito pelo Qoder em 21/04/2026 - lista de Instituicoes para o select
            var instituicoes = await _db.Instituicao.AsNoTracking()
                .OrderBy(i => i.Sigla)
                .Select(i => new { i.Id, i.Sigla, i.Nome })
                .ToListAsync();

            vm.InstituicoesSigla = instituicoes
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Sigla })
                .ToList();
            vm.InstituicoesNome = instituicoes
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Nome })
                .ToList();
            //..Qoder

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro de Postos/anexos", false };
            //Finalização da View
            _geralController.Validacao("AlterarPostos,Postos", ViewBag.TextoMenu[0]);
            return View(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarPostos")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> SalvarAlteracaoPostos(vmPostos vm, int id)
        {
            string redirecionaUrl = "Postos".MontaUrl(base.HttpContext.Request);

            if (vm == null || string.IsNullOrEmpty(vm.NomePosto))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios ou não havia nada para ser salvo" });

            //Feito pelo Qoder em 21/04/2026 - validação da Instituicao informada
            if (vm.InstituicaoId <= 0 || !await _db.Instituicao.AnyAsync(i => i.Id == vm.InstituicaoId))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Instituição informada é inválida", action = "", sucesso = false });

            string siglaNormalizada = GenericValidations.NormalizarSigla(vm.SiglaPosto);
            if (string.IsNullOrWhiteSpace(siglaNormalizada))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Sigla do posto é obrigatória", action = "", sucesso = false });
            //..Qoder

            Postos? Postos = await _db.Postos.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (Postos == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        //Colunas NÃO nulas:
                        Postos.InstituicaoId = vm.InstituicaoId;
                        Postos.SiglaPosto = siglaNormalizada;
                        Postos.NomePosto = vm.NomePosto.ToUpper();
                        Postos.Responsavel = vm.Responsavel.ToCapitalizeNotNull();

                        //Colunas que aceitam nulo:
                        Postos.Telefone = vm.Telefone.ApenasNumeros();
                        Postos.Logradouro = vm.Logradouro.ToCapitalize();
                        Postos.Endereco = vm.Endereco.ToCapitalize();
                        Postos.Numero = vm.Numero;
                        Postos.Complemento = vm.Complemento;
                        Postos.Bairro = vm.Bairro.ToCapitalize();
                        Postos.Cidade = vm.Cidade.ToCapitalize();
                        Postos.UF = vm.vmGeral.TipoUF;
                        Postos.CEP = vm.CEP;

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Posto/anexo foi atualizado", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Postos] Erro ao atualizar/alterar: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto/anexo NÃO foi atualizado", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirPostos")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> ExcluirPostos(int id)
        {
            //Feito pelo Kiro em 20/04/2026
            // Verifica se o posto possui vínculos antes de excluir
            //Feito pelo Qoder em 21/04/2026 - inclui verificação em ExamesRealizadosAM (D5)
            //Feito pelo Qoder em 12/08/2026 — removido _db.Requisitar.AnyAsync (tabela eliminada)
            bool possuiVinculos = await _db.ExamesRealizados.AnyAsync(e => e.PostoId == id)
                               || await _db.ExamesRealizadosAM.AnyAsync(e => e.PostoId == id);
            //..Qoder

            if (possuiVinculos)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Posto possui requisições ou exames vinculados e não pode ser excluído", action = "", sucesso = false });
            //..Kiro

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        Postos registro = await _db.Postos.FirstAsync(s => s.Id == id);
                        if (registro != null && registro.Id == id)
                        {
                            _db.Remove(registro);
                            await _db.SaveChangesAsync();
                            await transaction.CommitAsync();

                            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registro foi excluído", action = "", sucesso = true });
                        }

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi encontrado", action = "", sucesso = false });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Postos] Excluir - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarPostos")]
        public async Task<ActionResult> ConsultarPostos(vmPostos vm, int id)
        {
            Postos dados = await _db.Postos.Where(c => c.Id == id).Include(p => p.Instituicao).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.InstituicaoId = dados.InstituicaoId;
                vm.SiglaInstituicao = dados.Instituicao?.Sigla;
                vm.NomeInstituicao = dados.Instituicao?.Nome;
                vm.SiglaPosto = dados.SiglaPosto;
                vm.NomePosto = dados.NomePosto;
                vm.Logradouro = dados.Logradouro;
                vm.Endereco = dados.Endereco;
                vm.Numero = dados.Numero;
                vm.Complemento = dados.Complemento;
                vm.Bairro = dados.Bairro;
                vm.Cidade = dados.Cidade;
                vm.UF = dados.UF;
                vm.CEP = dados.CEP;
                vm.Telefone = dados.Telefone.FormataTelefone();
                vm.Responsavel = dados.Responsavel.ToCapitalizeNotNull();
                /*
                 * vm.vmGeral que pode receber dados de listas de tipos
                 */
                vmGeral vmGeral = new()
                {
                    TipoUF = dados.UF
                };
                vm.vmGeral = vmGeral;
                /*
                 * variáveis via ViewModel tipado
                 */
                vm.SessionUF = dados.UF;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta de Postos/anexos", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarPostos,Postos", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }
    }
}