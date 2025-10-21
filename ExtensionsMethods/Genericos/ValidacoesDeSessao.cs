using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;    //para uso do JsonResult
using Microsoft.AspNetCore.Mvc.Filters;    //para uso do IActionFilter

namespace ExtensionsMethods.Genericos
{
    public class ValidacoesDeSessao
    {
        public ValidacoesDeSessao()
        {
        }

        //Observar também a instrução "options.IdleTimeout = TimeSpan.FromSeconds(60);" em Startup.cs, que define o tempo de Sessão.
        public static bool ValidaSessao(HttpContext httpContext)
        {
            string? email = httpContext.Session.GetString("SessionEmail");
            string? nome = httpContext.Session.GetString("SessionNome");
            string? token = httpContext.Session.GetString("SessionToken");

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(nome) && !string.IsNullOrEmpty(token))
                return true;

            return false;
        }
    }

    //classe obrigatória para dar passagem do HttpContext entre as actions dos controllers
    //public class SessionFilter : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(ActionExecutingContext filterContext)
    //    {
    //        //testando apenas
    //        string? value = filterContext.HttpContext.Session.GetString("SessionEmail");
    //    }
    //}

    public class SessionFilter : IActionFilter
    {
        private readonly IValidadorDeSessao _validador;

        public SessionFilter(IValidadorDeSessao validador)
        {
            _validador = validador;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            string? controller = context.RouteData.Values["controller"]?.ToString();
            string? action = context.RouteData.Values["action"]?.ToString();

            string[] permissoesPublicas = new[] { "Login", "EsqueciSenha", "RecuperarSenha" };

            if (controller == "Login" || permissoesPublicas.Contains(action))
                return;

            // Verifica se a sessão é válida antes de continuar com a execução da ação
            if (!_validador.SessaoValida())
            {
                context.Result = new RedirectToActionResult("ErrorGenerico", "Mensagem", new
                {
                    sucesso = false,
                    mensagemErro = "Sua sessão expirou. Precisa realizar Login."
                });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        { }
    }
}