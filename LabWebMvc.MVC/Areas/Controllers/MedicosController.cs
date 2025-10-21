using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ExpressionCombiner;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using static BLL.UtilBLL;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class MedicosController : BaseController
    {
        public MedicosController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem)
        {
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
        public async Task<IActionResult> Index(string? Conteudo, int registros = 50)
        {
            MontaControllers("IncluirMedico", "Medicos");
            if (Conteudo == null) Conteudo = string.Empty; else Conteudo = Conteudo.Trim();

            ICollection<dynamic> listaGrid = [];
            List<Medicos> dados = [];

            int totalTabela = 0;
            int totalRegistros = 0;
            if (string.IsNullOrEmpty(Conteudo)) registros = 100; //quando não tem dados para filtrar

            totalTabela = _db.Medicos.AsNoTracking().AsEnumerable().Count();

            if (!string.IsNullOrEmpty(Conteudo))
            {
                dados = await _db.Medicos.AsNoTracking()
                          .FiltrarPorConteudo(Conteudo, x => x.CRM, x => x.NomeMedico, x => x.Id.ToString())
                          .OrderByDescending(x => x.Id)
                          .ToListAsync();
            }
            else
                dados = await _db.Medicos.AsNoTracking().OrderByDescending(o => o.Id).Take(registros).ToListAsync();

            foreach (Medicos item in dados)
            {
                totalRegistros++;
                vmMedicos resultado = new()
                {
                    Id = item.Id,
                    NomeMedico = item.NomeMedico,
                    CRM = item.CRM,
                    Telefone = item.Telefone.FormataTelefone(),
                    Email = item.Email,
                    Especialidade = item.Especialidade
                };
                listaGrid.Add(resultado);
            }

            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = listaGrid;

            //Finalização da View
            return _geralController.Validacao("Index", "Cadastro de Médicos", totalRegistros, totalTabela, listaGrid);
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
            try
            {
                using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
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

                    if (_db.SaveChanges() <= 0)
                    {
                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Médico NÃO foi salvo", action = "", sucesso = false });
                    }
                    trans.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                _eventLogHelper.LogEventViewer("[Medicos] Salvar - TransactionAbortedException Message: " + ex.Message, "wError");
            }

            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Médico foi salvo", action = "", sucesso = true });
        }

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
        public async Task<IActionResult> SalvarAlteracaoMedico(vmMedicos vm, int id)
        {
            string redirecionaUrl = "Medicos".MontaUrl(base.HttpContext.Request);

            if (string.IsNullOrEmpty(vm.NomeMedico))
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Formulário possui campos obrigatórios vazios" });

            Medicos? Medicos = await _db.Medicos.Where(s => s.Id == id).SingleOrDefaultAsync();
            if (Medicos == null)
                return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Não foi possível salvar o registro neste momento", action = "", sucesso = false });

            try
            {
                using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
                {
                    //Colunas NÃO nulas:
                    Medicos.NomeMedico = vm.NomeMedico.ToUpper();
                    Medicos.CRM = vm.CRM;
                    Medicos.Especialidade = vm.Especialidade != null ? vm.Especialidade.ToUpper() : string.Empty;
                    Medicos.Telefone = vm.Telefone;
                    Medicos.Email = vm.Email != null ? vm.Email.ToLower() : string.Empty;

                    if (_db.SaveChanges() <= 0)
                    {
                        LoggerFile.Write("ERRO: Médico não foi atualizado - Id:" + id.ToString());
                        return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Médico NÃO foi atualizado", action = "", sucesso = false });
                    }
                    trans.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                _eventLogHelper.LogEventViewer("[Medicos] Atualizar - TransactionAbortedException Message: " + ex.Message, "wError");
            }
            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Médico foi atualizado", action = "", sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ExcluirMedico")]
        public async Task<IActionResult> ExcluirMedico(int id)
        {
            using (TransactionScope trans = new(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted }))
            {
                bool erro = false;
                int change = 0;
                try
                {
                    Medicos registro = await _db.Medicos.FirstAsync(s => s.Id == id);
                    if (registro != null && registro.Id == id)
                    {
                        _db.Remove(registro);
                        change = _db.SaveChanges();
                    }
                }
                catch
                {
                    erro = true;
                }
                finally
                {
                    trans.Complete();
                }
                if (erro || change < 1)
                    return Json(new { titulo = MensagensError_pt_BR.ErroFalhou, mensagem = "Registro não foi excluído", action = "", sucesso = false });
            }
            return Json(new { titulo = Mensagens_pt_BR.Sucesso, mensagem = "Registro foi excluído", action = "", sucesso = true });
        }

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