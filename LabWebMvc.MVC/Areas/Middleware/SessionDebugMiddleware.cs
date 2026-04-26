namespace LabWebMvc.MVC.Areas.Middleware
{
    public class SessionDebugMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionDebugMiddleware> _logger;

        public SessionDebugMiddleware(RequestDelegate next, ILogger<SessionDebugMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ISession session = context.Session;

            // Garante que a sessão foi carregada
            if (session != null)
            {
                string? login = session.GetString("SessionLogin");
                string? email = session.GetString("SessionEmail");
                string? nome = session.GetString("SessionNome");
                string? token = session.GetString("SessionToken");

                _logger.LogInformation("[SESSION DEBUG]");
                _logger.LogInformation($"Path: {context.Request.Path}");
                _logger.LogInformation($"SessionLogin: {login}");
                _logger.LogInformation($"SessionEmail: {email}");
                _logger.LogInformation($"SessionNome: {nome}");
                _logger.LogInformation($"SessionToken: {token}");
            }
            else
            {
                _logger.LogWarning("Sessão não está disponível neste contexto.");
            }

            await _next(context);
        }
    }
}