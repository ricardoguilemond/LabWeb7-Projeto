namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public static class GoogleConfig
    {
        public static readonly string MySecretKeyGoogle = string.Empty;
        public static readonly string MySecretKeyLEGADA = string.Empty;
        public static readonly string MyVetorDeCifras = string.Empty;

        static GoogleConfig()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // ou Directory.GetCurrentDirectory()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            if (configuration.GetSection("Secrets").GetSection("mySecretKeyGoogle").Exists())
            {
                MySecretKeyGoogle = configuration["Secrets:mySecretKeyGoogle"] ?? "";
            }
            if (configuration.GetSection("Secrets").GetSection("mySecretKeyLEGADA").Exists())
            {
                MySecretKeyLEGADA = configuration["Secrets:mySecretKeyLEGADA"] ?? "";
            }
            if (configuration.GetSection("Secrets").GetSection("myVetorDeCifras").Exists())
            {
                MyVetorDeCifras = configuration["Secrets:myVetorDeCifras"] ?? "";
            }
        }
    }
}
