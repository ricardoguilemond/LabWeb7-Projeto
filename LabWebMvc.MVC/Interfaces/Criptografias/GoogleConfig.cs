namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public static class GoogleConfig
    {
        public static readonly string MySecretKeyGoogle = string.Empty;
        public static readonly string MySecretKeyLEGADA = string.Empty;
        public static readonly string MyVetorDeCifras = string.Empty;
        public static readonly string MySecretKeyPublic = string.Empty;
        public static readonly string MySecretKeyPrivate = string.Empty;
        public static readonly string GatewayUrl = string.Empty;

        static GoogleConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // ou Directory.GetCurrentDirectory()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            if (configuration.GetSection("Secrets").GetSection("myVetorDeCifras").Exists())
            {
                MyVetorDeCifras = configuration["Secrets:myVetorDeCifras"] ?? "";
            }
            if (configuration.GetSection("Secrets").GetSection("mySecretKeyLEGADA").Exists())
            {
                MySecretKeyLEGADA = configuration["Secrets:mySecretKeyLEGADA"] ?? "";
            }
            if (configuration.GetSection("Secrets").GetSection("mySecretKeyGoogle").Exists())
            {
                MySecretKeyGoogle = configuration["Secrets:mySecretKeyGoogle"] ?? "";
            }
            if (configuration.GetSection("Secrets").GetSection("mySecretKeyPublic").Exists())
            {
                MySecretKeyPublic = configuration["Secrets:mySecretKeyPublic"] ?? "";
            }
            if (configuration.GetSection("Secrets").GetSection("mySecretKeyPrivate").Exists())
            {
                MySecretKeyPrivate = configuration["Secrets:mySecretKeyPrivate"] ?? "";
            }
            if (configuration.GetSection("Secrets").GetSection("GatewayUrl").Exists())
            {
                GatewayUrl = configuration["Secrets:GatewayUrl"] ?? "";
            }
        }
    }
}
