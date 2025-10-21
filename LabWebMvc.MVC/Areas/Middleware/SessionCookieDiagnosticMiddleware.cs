namespace LabWebMvc.MVC.Areas.Middleware
{
    public class SessionCookieDiagnosticMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionCookieDiagnosticMiddleware> _logger;

        private const string SessionCookieName = ".LabWeb7.Session"; // ou o nome configurado no Startup

        public SessionCookieDiagnosticMiddleware(RequestDelegate next, ILogger<SessionCookieDiagnosticMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Verifica se o cookie está presente na requisição
            if (context.Request.Cookies.TryGetValue(SessionCookieName, out string? sessionId))
            {
                _logger.LogInformation("Cookie de sessão encontrado: {SessionId}", sessionId);
            }
            else
            {
                _logger.LogWarning("Cookie de sessão '{SessionCookieName}' NÃO foi enviado na requisição!", SessionCookieName);

                // Dica adicional:
                _logger.LogWarning("Verifique se o navegador está permitindo cookies, se o domínio é o mesmo e se o atributo SameSite está correto.");
            }

            await _next(context);
        }
    }
}