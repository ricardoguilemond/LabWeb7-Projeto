namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    public static class HttpContextHelper
    {
        private static IHttpContextAccessor _httpContextAccessor = null!;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static HttpContext? Current
        {
            get
            {
                if (_httpContextAccessor == null)
                    throw new InvalidOperationException("HttpContextHelper ainda não foi configurado. Certifique-se de chamar Configure() no Startup.");

                return _httpContextAccessor.HttpContext;
            }
        }
    }
}