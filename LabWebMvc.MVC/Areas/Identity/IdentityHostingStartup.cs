using LabWebMvc.MVC.Areas.Utils;

//[assembly: HostingStartup(typeof(LabWebMvc.MVC.Areas.Identity.IdentityHostingStartup))]
namespace LabWebMvc.MVC.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        //Essa forma de conexão é exclusivamente usada pelo Migration para o Core
        public void Configure(IWebHostBuilder builder)
        {
            string? envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string? stringConnection = string.Empty;   //IConfigurationSection retorna uma string que pode ser nula

            IConfigurationRoot configuration;

            if (!string.IsNullOrEmpty(envName) && string.Equals(envName, "DEVELOPMENT", StringComparison.OrdinalIgnoreCase))
            {
                configuration = new ConfigurationBuilder()
                                   .SetBasePath(Utils.Utils.GetPathAppSettingsJson())
                                   .AddJsonFile($"appsettings.{envName}.json", optional: false)
                                   .Build();
            }
            else
            {
                configuration = new ConfigurationBuilder()
                                    .SetBasePath(Utils.Utils.GetPathAppSettingsJson())
                                    .AddJsonFile("appsettings.json", optional: false)
                                    .Build();
            }
            //Verifica se a configuração do serviço de fato existe
            if (configuration.GetSection("ConexaoPostgreSQL").GetSection("PSQLConnectionString").Exists())
            {
                stringConnection = configuration.GetSection("ConexaoPostgreSQL").GetSection("PSQLConnectionString").Value ?? "";
                stringConnection = stringConnection.ReformaTexto("usubanco", BasePadrao.UserId).ReformaTexto("ususenha", BasePadrao.Password);
            }

        }
    }
}