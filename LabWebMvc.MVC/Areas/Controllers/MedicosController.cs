using BLL;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using static BLL.UtilBLL;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class MedicosController : BaseController
    {
        private readonly IMemoryCache _cache;

        public MedicosController(
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
        [Route("Medicos")]
        public IActionResult Index()
        {
            MontaControllers("IncluirMedico", "Medicos");

            // Dados do grid carregados via AJAX pelo DataTables (server-side processing).
            ViewBag.TextoMenu = new object[] { "Cadastro de Médicos", false };
            return View(new vmMedicos());
        }

        /// <summary>
        /// Endpoint server-side do DataTables para o cadastro de médicos.
        /// Carrega blocos de 100 registros do banco (cache de curta duração) e
        /// devolve a página solicitada de 10 em 10.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Medicos/Listar")]
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

                if (!_cache.TryGetValue(cacheKey, out List<Medicos>? blockData) || blockData == null)
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
                    nomeMedico = item.NomeMedico ?? string.Empty,
                    crm = item.CRM ?? string.Empty,
                    especialidade = item.Especialidade ?? string.Empty,
                    telefone = item.Telefone.FormataTelefone(),
                    email = item.Email ?? string.Empty,
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
                _eventLogHelper.LogEventViewer("[Medicos] Listar - Erro: " + ex.Message, "wError");
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
            return "Medicos_" + Convert.ToHexString(hash);
        }

        private async Task<List<Medicos>> LoadBlockAsync(string searchValue, string sortColumn, string sortDir, int blockStart, int blockSize)
        {
            IQueryable<Medicos> query = BuildBaseQuery(searchValue);
            query = ApplyOrdering(query, sortColumn, sortDir);
            return await query.Skip(blockStart).Take(blockSize).ToListAsync();
        }

        private async Task<int> CountTotalAsync(string searchValue)
        {
            return await BuildBaseQuery(searchValue).CountAsync();
        }

        private IQueryable<Medicos> BuildBaseQuery(string searchValue)
        {
            var query = _db.Medicos.AsNoTracking();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.FiltrarPorConteudo(searchValue,
                    x => x.CRM,
                    x => x.NomeMedico,
                    x => x.Especialidade,
                    x => x.Id.ToString());
            }

            return query;
        }

        private IQueryable<Medicos> ApplyOrdering(IQueryable<Medicos> query, string sortColumn, string sortDir)
        {
            bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

            return sortColumn.ToLowerInvariant() switch
            {
                "nomemedico" => desc ? query.OrderByDescending(p => p.NomeMedico) : query.OrderBy(p => p.NomeMedico),
                "crm" => desc ? query.OrderByDescending(p => p.CRM) : query.OrderBy(p => p.CRM),
                "especialidade" => desc ? query.OrderByDescending(p => p.Especialidade) : query.OrderBy(p => p.Especialidade),
                "telefone" => desc ? query.OrderByDescending(p => p.Telefone) : query.OrderBy(p => p.Telefone),
                "email" => desc ? query.OrderByDescending(p => p.Email) : query.OrderBy(p => p.Email),
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
        [Route("IncluirMedico")]
        public IActionResult IncluirMedico()
        {
            //Finalização da View
            return _geralController.Validacao("IncluirMedico", "Cadastro de Médicos");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("IncluirMedico")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> SalvarMedico(vmMedicos obj)
        {
            string redirecionaUrl = "Medicos".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(obj.NomeMedico))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });

            Medicos? Medicos = await _db.Medicos.Where(s => s.Email == obj.Email ||
                                     (s.NomeMedico == obj.NomeMedico && (s.CRM == obj.CRM)) ||
                                     (s.CRM == obj.CRM) ||
                                     (s.NomeMedico == obj.NomeMedico)).SingleOrDefaultAsync();
            if (Medicos != null)
            {
                if (Medicos.Email == obj.Email)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Já existe Médico cadastrado com este e-mail", action = "", sucesso = false });
                else if (Medicos.NomeMedico == obj.NomeMedico.ToUpper() && Medicos.CRM == obj.CRM)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Médico já cadastrado com este CRM/Registro", action = "", sucesso = false });
                else
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Já existe Médico cadastrado com este Nome ou CRM/Registro", action = "", sucesso = false });
            }

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        await _db.Medicos.AddAsync(new Medicos()
                        {
                            //Colunas NÃO nulas:
                            NomeMedico = obj.NomeMedico.ToUpper(),

                            //Colunas que aceitam nulas:
                            CRM = obj.CRM,
                            Telefone = obj.Telefone,
                            Email = obj.Email,
                            Especialidade = obj.Especialidade
                        });

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Médico foi salvo", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Medicos] Salvar - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Médico NÃO foi salvo", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("AlterarMedico")]
        public async Task<IActionResult> AlterarMedico(vmMedicos vm, int id)
        {
            Medicos dados = await _db.Medicos.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.NomeMedico = dados.NomeMedico;
                vm.CRM = dados.CRM;
                vm.Email = dados.Email;
                vm.Telefone = dados.Telefone;
                vm.Especialidade = dados.Especialidade;
            }

            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Alterar Cadastro de Médicos", false };
            //Finalização da View
            _geralController.Validacao("AlterarMedico,Medicos", ViewBag.TextoMenu[0]);
            return View(vm); //na edição a vm precisa retornar para a View
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("AlterarMedico")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> SalvarAlteracaoMedico(vmMedicos vm, int id)
        {
            string redirecionaUrl = "Medicos".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.NomeMedico))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });

            Medicos? Medicos = await _db.Medicos.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (Medicos == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        //Colunas NÃO nulas:
                        Medicos.NomeMedico = vm.NomeMedico.ToUpper();
                        Medicos.CRM = vm.CRM;
                        Medicos.Especialidade = vm.Especialidade != null ? vm.Especialidade.ToUpper() : string.Empty;
                        Medicos.Telefone = vm.Telefone;
                        Medicos.Email = vm.Email != null ? vm.Email.ToLower() : string.Empty;

                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Médico foi atualizado", action = "", sucesso = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();

                        _eventLogHelper.LogEventViewer("[Medicos] Atualizar - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Médico NÃO foi atualizado", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirMedico")]
        //Feito pelo Kiro em 20/04/2026
        public async Task<IActionResult> ExcluirMedico(int id)
        {
            //Feito pelo Kiro em 20/04/2026
            // Verifica se o médico possui vínculos antes de excluir
            bool possuiVinculos = await _db.Requisitar.AnyAsync(r => r.MedicoId == id)
                               || await _db.ExamesRealizados.AnyAsync(e => e.MedicoId == id)
                               || await _db.ExamesPendentes.AnyAsync(e => e.MedicoId == id);

            if (possuiVinculos)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Médico possui requisições ou exames vinculados e não pode ser excluído", action = "", sucesso = false });
            //..Kiro

            Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        Medicos registro = await _db.Medicos.FirstAsync(s => s.Id == id);
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

                        _eventLogHelper.LogEventViewer("[Medicos] Excluir - Erro: " + ex.Message, "wError");

                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false });
                    }
                }
            });
        }
        //..Kiro

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ConsultarMedico")]
        public async Task<ActionResult> ConsultarMedico(vmMedicos vm, int id)
        {
            Medicos dados = await _db.Medicos.Where(c => c.Id == id).AsNoTracking().FirstAsync();

            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.NomeMedico = dados.NomeMedico.ToUpper();
                vm.CRM = dados.CRM;
                vm.Especialidade = dados.Especialidade.ToCapitalize();
                vm.Telefone = dados.Telefone;
                vm.Email = dados.Email != null ? dados.Email.ToLower() : string.Empty;
            }
            //Parâmetros auxiliares em ViewBag
            ViewBag.TextoMenu = new object[] { "Consulta de Médico", false };
            //Finalização para a View
            _geralController.Validacao("ConsultarMedico,Medicos", ViewBag.TextoMenu[0]);
            return PartialView(vm); //na edição a vm precisa retornar para a View
        }

        //Feito pelo Kiro em 17/05/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Medicos/ModalConsultarMedico")]
        public async Task<ActionResult> ModalConsultarMedico(int id)
        {
            var dados = await _db.Medicos.Where(c => c.Id == id).AsNoTracking().FirstOrDefaultAsync();
            var vm = new vmMedicos();
            if (dados != null)
            {
                vm.Id = dados.Id;
                vm.NomeMedico = dados.NomeMedico.ToUpper();
                vm.CRM = dados.CRM;
                vm.Especialidade = dados.Especialidade.ToCapitalize();
                vm.Telefone = dados.Telefone;
                vm.Email = dados.Email != null ? dados.Email.ToLower() : string.Empty;
            }
            ViewBag.TextoMenu = new object[] { "Consulta de Médico", false };
            _geralController.Validacao("ConsultarMedico,Medicos", ViewBag.TextoMenu[0]);
            return PartialView("_ModalConsultarMedico", vm);
        }
        //..Kiro

        public IActionResult ConverterPdf()
        {
            try
            {
                //ConversoresPdf pdf = new ConversoresPdf();
                //pdf.ConverteHtmlToPdf(@"F:\Temp2\Arquivo.html");

                //pdf.ConverteHtmlToPdf();

                return Json(new { success = true, responseText = "Salvou com sucesso" });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Medicos] ConverterPdf: " + ex.Message, "wError");
                return Json(new { success = false, responseText = string.Format("{0} {1}", "Falha:", ex.Message) });
            }
        }
    }

    //internal class CustomErrorModel
    //{
    //    private string v;

    //    public CustomErrorModel(string v)
    //    {
    //        this.v = v;
    //    }
    //}
}