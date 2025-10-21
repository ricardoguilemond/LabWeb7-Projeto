namespace LabWebMvc.MVC.Areas.Middleware
{
    public class ResponseCookieLoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseCookieLoggerMiddleware> _logger;

        public ResponseCookieLoggerMiddleware(RequestDelegate next, ILogger<ResponseCookieLoggerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Captura a resposta original
            HttpResponse originalResponse = context.Response;

            // Hook no OnStarting para logar antes da resposta final
            originalResponse.OnStarting(() =>
            {
                Microsoft.Extensions.Primitives.StringValues setCookieHeaders = context.Response.Headers["Set-Cookie"];

                if (setCookieHeaders.Count == 0)
                {
                    _logger.LogWarning("Nenhum cookie foi enviado na resposta.");
                }
                else
                {
                    foreach (string? header in setCookieHeaders)
                    {
                        _logger.LogInformation("Cookie enviado na resposta: {Cookie}", header);
                    }
                }

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}