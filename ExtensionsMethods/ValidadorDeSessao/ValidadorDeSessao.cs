using Microsoft.AspNetCore.Http;
using System.Text;

namespace ExtensionsMethods.ValidadorDeSessao
{
    public class ValidadorDeSessao : IValidadorDeSessao
    {
        private readonly IHttpContextAccessor _accessor;

        public ValidadorDeSessao(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public bool SessaoValida()
        {
            HttpContext? httpContext = _accessor.HttpContext;
            if (httpContext == null || !httpContext.Session.IsAvailable)
                return false;

            try
            {
                string? email = httpContext.Session.GetString("SessionEmail");
                string? nome = httpContext.Session.GetString("SessionNome");
                string? token = httpContext.Session.GetString("SessionToken");

                return !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(nome) && !string.IsNullOrEmpty(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao acessar sessão: " + ex.Message);
                return false;
            }
        }
    }
}