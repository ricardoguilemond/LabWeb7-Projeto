using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class MensagemController : Controller
    {
        public ActionResult MensagemTela(string[] mensagem, string mensagemModal, string tituloModal = "Atenção", bool success = true)
        {
            /* USO EXEMPLOS: a partir de outras actions que acionam esta MensagemTela():
             * return RedirectToAction("MensagemTela", "Mensagem", new { mensagem = new string[] { "null", "false" }, mensagemModal = MensagensError_pt_BR.ErroSalvar, tituloModal = "Atenção", success = false });
             */
            if (string.Equals(mensagem[0], "null", StringComparison.OrdinalIgnoreCase))
            {
                mensagem[0] = (string)TextoMenuPrincipalResponse.TextoMenuPrincipal[0];
            }

            ViewData["TextoMenu"] = new List<string> { "Mensagem" };
            ViewData["MensagemModal"] = mensagemModal ?? "Mensagem não informada.";
            ViewData["TituloModal"] = tituloModal ?? "Atenção";
            ViewData["Sucesso"] = success;

            return PartialView("MensagemTela", new { mensagem, mensagemModal, tituloModal, success });
        }

        public ActionResult MensagemTela(string mensagemModal = "Erro", bool? success = null)
        {
            /* USO EXEMPLOS: a partir de outras actions que acionam esta MensagemTela():
             * return RedirectToAction("MensagemTela", "Mensagem", new { mensagemModal = MensagensError_pt_BR.ErroSalvar, success = false });
             */
            ViewData["TextoMenu"] = new List<string> { "Mensagem" };
            ViewData["MensagemModal"] = mensagemModal ?? "Mensagem não informada.";
            ViewData["TituloModal"] = "Atenção";
            ViewData["Sucesso"] = success == true ? "Ok" : success == null ? "Atenção" : "Alerta";

            return PartialView("MensagemTela");
        }

        public ActionResult MensagemTela(string mensagemModal = "Procedimento executado com sucesso")
        {
            /* USO EXEMPLOS: a partir de outras actions que acionam esta MensagemTela():
             * return RedirectToAction("MensagemTela", "Mensagem", new { mensagemModal = "Procedimento executado com sucesso" });
             */
            ViewData["TextoMenu"] = new List<string> { "Mensagem" };
            ViewData["MensagemModal"] = mensagemModal ?? "Mensagem não informada.";
            ViewData["TituloModal"] = "Atenção";
            ViewData["Sucesso"] = true;

            return PartialView("MensagemTela");
        }

        public IActionResult MensagemTela(string mensagemModal, string tituloModal, bool success)
        {
            ViewData["TextoMenu"] = new List<string> { "Mensagem" };
            ViewData["MensagemModal"] = mensagemModal ?? "Mensagem não informada.";
            ViewData["TituloModal"] = tituloModal ?? "Atenção";
            ViewData["Sucesso"] = success;

            return PartialView("MensagemTela"); // A view deve herdar do _Layout.cshtml
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult MensagemConfirma(string tituloModal, string mensagemModal, string perguntaModal, string returnAction, string returnController, string? textoRodape = "")
        {
            ViewData["TextoMenu"] = new List<string> { "Mensagem" };
            ViewData["TituloModal"] = tituloModal ?? "Atenção";
            ViewData["MensagemModal"] = mensagemModal ?? "";
            ViewData["PerguntaModal"] = perguntaModal ?? "Confirma?";
            ViewData["Sucesso"] = null;
            ViewData["ReturnAction"] = returnAction ?? "Index";
            ViewData["ReturnController"] = returnController ?? "Home";
            ViewData["TextoRodape"] = textoRodape ?? string.Empty;

            return View("MensagemConfirma");
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult RespostaConfirmacao(string resposta, string returnAction, string returnController)
        {
            if (resposta == "sim")
            {
                return RedirectToAction(returnAction, returnController);
            }
            return RedirectToAction("Logout", "Home");
        }

        public JsonResult MensagemView(string mensagemModal = "Erro", bool? success = null)
        {
            /* USO EXEMPLOS: a partir de outras actions que acionam esta MensagemTela():
             * return RedirectToAction("MensagemView", "Mensagem", new { mensagemModal = "Registro foi salvo", success = true });
             */
            //ViewBag.TextoMenu = new object[] { TextoMenuPrincipalResponse.TextoMenuPrincipal[0], false };

            ViewData["TextoMenu"] = new List<string> { "Mensagem" };
            ViewData["MensagemModal"] = mensagemModal ?? "Mensagem não informada.";
            ViewData["TituloModal"] = success == true ? "Ok" : success == null ? "Atenção" : "Alerta";
            ViewData["Sucesso"] = success;

            return Json(new { success = true, responseText = mensagemModal });
        }

        [Route("AcessoNegado")]
        public ActionResult AcessoNegado(string[] mensagem)
        {
            if (mensagem != null && mensagem.Length < 2)
            {
                mensagem = new string[] { "Validação de Segurança: acesso negado por falta de permissão", "false" };
            }
            if (mensagem == null || mensagem[0] == null) mensagem = new string[] { "Ocorreu uma queda devido a um 'time out' no servidor", "false" };
            ViewBag.TextoMenu = new object[] { mensagem[0], bool.Parse(mensagem[1].ToLower()) };
            ViewBag.TituloErro = "Atenção";
            if (mensagem != null)
                ViewBag.MensagemErro = mensagem[0];
            else
                ViewBag.MensagemErro = "Aguardando validação de segurança (acesso indisponível: verifique também seu sinal de internet)";

            return PartialView("AcessoNegado");
        }

        [Route("AcessoEmail")]
        public ActionResult AcessoEmail(string[] mensagem)
        {
            ViewBag.TextoMenu = new object[] { mensagem[0], bool.Parse(mensagem[1].ToLower()) };
            ViewBag.TituloErro = "Atenção";
            ViewBag.MensagemErro = "Por favor, para ter acesso você precisa confirmar seu Email de Login";

            return PartialView("AcessoEmail");
        }

        [Route("AcessoValidado")]
        public ActionResult AcessoValidado(string[] retornoDeRota, int codError = 0)
        {
            string[] retorno = new string[] { "", "" };
            if (string.IsNullOrEmpty(retornoDeRota[0])) //se houver tentativa de burlar a rota, considera tudo index
            {
                //Obrigatório para forçar retorno para a página principal, no caso de tentativa de desvio de URL.
                retorno[0] = "Index";
                retorno[1] = "Index";
            }
            else
                retorno = retornoDeRota[0].Split(',');

            ViewBag.TextoMenu = new object[] { "", false };
            ViewBag.AlertaErro = "";
            ViewBag.MensagemErro = "";

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("SessionEmail")) ||
                string.IsNullOrEmpty(HttpContext.Session.GetString("SessionNome")) ||
                string.IsNullOrEmpty(HttpContext.Session.GetString("SessionToken")))
            {
                return View("Error");
            }
            return View(retorno[0], retorno[1]);
        }

        private bool ValidaLogin(string? token)
        {
            if (token == HttpContext.Session.GetString("SessionToken"))
                return true;

            return false;
        }

        //Erro genérico para erros genéricos de gravação
        [Route("ErrorGenerico")]
        public ActionResult ErrorGenerico(string mensagemErro = "")
        {
            ViewBag.TextoMenu = new object[] { "", false };
            ViewBag.TituloErro = "Atenção";
            ViewBag.MensagemErro = mensagemErro;
            return PartialView("ErrorGenerico");
        }

        /*
         * Para todos os tipos de mensagens de Error (pode usar esse padrão para todas as mensagens!)
         * USO: dentro de qualquer controller: return RedirectToAction("Error", "Mensagem", new { mensagem = "Falhou a tentativa de realizar login" });
         */

        [Route("Error")]
        public ActionResult Error(string mensagem = "Falhou o procedimento executado")
        {
            ViewBag.TextoMenu = new object[] { "", false };   //linha obrigatória
            ViewBag.TituloErro = "Atenção";                   //título padrão
            ViewBag.MensagemErro = mensagem;                  //mensagem de erro enviada

            return PartialView("Error");
        }

        //Erro 500 Servidor
        [Route("Error500")]
        public ActionResult Error500()
        {
            ViewBag.TituloErro = "Erro";
            ViewBag.MensagemErro = "Servidor indisponível. Tente novamente ou contate um Administrador";

            return PartialView("Error500");
        }

        //Error 404
        [Route("Error404")]
        public ActionResult Error404()
        {
            ViewBag.TituloErro = "Erro";
            ViewBag.MensagemErro = "A página informada não está acessível neste momento";

            return PartialView("Error404");
        }

        //Erro 401 permissão de acesso ou execução
        [Route("Error401")]
        public ActionResult Error401()
        {
            ViewBag.TituloErro = "Acesso Negado";
            ViewBag.MensagemErro = "Você não tem permissão para executar isso";

            return PartialView("Error401");
        }

        //Estudar este método
        [HttpGet]
        [Route("Html")]
        public ContentResult Html()
        {
            object html = MensagemHtmlResponse.MensagemHtml[0];
            return new ContentResult
            {
                Content = (string)html,
                ContentType = "text/html"
            };
        }
    }
}