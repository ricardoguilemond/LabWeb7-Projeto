using ExtensionsMethods.Genericos;
using LabWebMvc.MVC;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ServicoExportacao
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string? envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string? stringConnection = string.Empty;   //IConfigurationSection retorna uma string que pode ser nula
            IConfigurationRoot configurationRoot;
            try
            {
                if (!string.IsNullOrEmpty(envName) && envName.ToUpper() == "DEVELOPMENT")
                {
                    //O caminho do arquivo JSon é o da aplicação principal, por isso usei "replace" para modificar o caminho.
                    configurationRoot = new ConfigurationBuilder()
                                            .SetBasePath(Utils.GetPathAppSettingsJson())
                                            .AddJsonFile($"appsettings.{envName}.json", optional: false)
                                            .Build();
                }
                else
                {
                    //O caminho do arquivo JSon é o da aplicação principal, por isso usei "replace" para modificar o caminho.
                    configurationRoot = new ConfigurationBuilder()
                                            .SetBasePath(Utils.GetPathAppSettingsJson())
                                            .AddJsonFile($"appsettings.json", optional: false)
                                            .Build();
                }
            }
            catch (Exception)
            {
                LoggerFile.Write(string.Format("Windows Service '{0}' com falha na leitura da conexão Json ::: {1} ", "Lab7ServiceIntegracao", DateTime.UtcNow));
                throw;
            }

            //Verifica se a configuração do serviço de fato existe
            if (configurationRoot.GetSection("ConexaoPostgreSQL").GetSection("PSQLConnectionString").Exists())
            {
                stringConnection = configurationRoot.GetSection("ConexaoPostgreSQL").GetSection("PSQLConnectionString").Value ?? "";
                stringConnection = stringConnection.ReformaTexto("usubanco", BasePadrao.UserId).ReformaTexto("ususenha", BasePadrao.Password);
            }
            //services.AddDbContext<Db>(options => options.UseNpgsql(stringConnection));

            //****************************************************************************
            //****************************************************************************
            //****************************************************************************
            //
            //Serviço de Exportação
            services.AddHostedService<SvcExportacao>();

            //Se desejar, pode chamar abaixo outros serviços...

            //****************************************************************************
            //****************************************************************************
            //****************************************************************************
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();    /* este instalou como padrão */
                app.UseStatusCodePages();           /* coloquei */
            }

            /* Para dar suporte as datas brasileiras como padrão em toda a aplicação
             * Parece que tem que ficar aqui entre o app.UseRouting() e app.UseAuthentication()
             */
            CultureInfo[] supportedCultures = new[] { new CultureInfo(name: "pt-BR") };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture: "pt-BR", uiCulture: "pt-BR"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
        }
    }
}