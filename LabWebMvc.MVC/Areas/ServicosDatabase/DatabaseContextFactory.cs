using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    public static class DatabaseContextFactory
    {
        //Cria instâncias de conexões personalizadas, com base no meu "Db" (DbContext) para conectar dinamicamente entre Bancos de Dados.
        public static Db CreateDbContextCliente(string connectionString,
                                                IConnectionService connectionService,
                                                IEventLogHelper eventLogHelper)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A connection string não pode ser nula ou vazia.", nameof(connectionString));

            var optionsBuilder = new DbContextOptionsBuilder<Db>().UseNpgsql(connectionString);

            return new Db(optionsBuilder.Options, connectionService, eventLogHelper);
        }

        //Retorna o String de Conexão Padrão da empresa TESTE (LABWEB7), e configura Builder da conexão.
        public static string RetornaStringDeConexaoPadrao()
        {
            string? envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string pathToContentRoot = Areas.Utils.Utils.GetPathAppSettingsJson();
            IConfigurationRoot configuration;

            if (!string.IsNullOrEmpty(envName) && envName.Equals("DEVELOPMENT", StringComparison.CurrentCultureIgnoreCase))
            {
                configuration = new ConfigurationBuilder()
                                    .SetBasePath(pathToContentRoot)
                                    .AddJsonFile($"appsettings.{envName}.json", optional: false)
                                    .Build();
            }
            else
            {
                configuration = new ConfigurationBuilder()
                                   .SetBasePath(pathToContentRoot)
                                   .AddJsonFile("appsettings.json", optional: false)
                                   .Build();
            }
            string link = configuration.GetSection("ConexaoPostgreSQL").GetSection("PSQLConnectionString").Value ?? "";
            link = link.ReformaTexto("usubanco", BasePadrao.UserId).ReformaTexto("ususenha", BasePadrao.Password);
            return link;
        }

        //Retorna o String de Conexão das Empresas-Clientes (LABWEB7Empresas), e configura Builder da conexão.
        public static string RetornaStringDeConexaoPadraoEmpresas()
        {
            string? envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string pathToContentRoot = Utils.Utils.GetPathAppSettingsJson();
            IConfigurationRoot configuration;

            if (!string.IsNullOrEmpty(envName) && envName.Equals("DEVELOPMENT", StringComparison.CurrentCultureIgnoreCase))
            {
                configuration = new ConfigurationBuilder()
                                    .SetBasePath(pathToContentRoot)
                                    .AddJsonFile($"appsettings.{envName}.json", optional: false)
                                    .Build();
            }
            else
            {
                configuration = new ConfigurationBuilder()
                                   .SetBasePath(pathToContentRoot)
                                   .AddJsonFile("appsettings.json", optional: false)
                                   .Build();
            }
            string link = configuration.GetSection("ConexaoPostgreSQL").GetSection("PSQLConnectionStringEmpresas").Value ?? "";
            link = link.ReformaTexto("usubanco", BasePadrao.UserId).ReformaTexto("ususenha", BasePadrao.Password);
            return link;
        }
    }
}