using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    public class DatabaseHelper
    {
        private readonly DbContext _context;

        public DatabaseHelper(DbContext context)
        {
            _context = context;
        }

        public string GetDatabaseProviderName()
        {
            // Obtém o provedor de banco de dados
            string? databaseProvider = _context.Database.ProviderName;

            return databaseProvider switch
            {
                "Microsoft.EntityFrameworkCore.SqlServer" => "MSSQL",
                "Microsoft.EntityFrameworkCore.Oracle" => "Oracle",
                "MySql.EntityFrameworkCore" => "MySQL",
                "Npgsql.EntityFrameworkCore.PostgreSQL" => "PostgreSQL",
                _ => "Desconhecido"
            };
        }

        public static string IdentificaTipoDeBancoDeDados(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A string de conexão não pode estar vazia.", nameof(connectionString));

            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) &&
                connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase) &&
                connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase))
                return "PostgreSQL";

            else if (connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase) &&
                     connectionString.Contains(".oracle", StringComparison.OrdinalIgnoreCase))
                return "Oracle";

            else if (connectionString.Contains("Server", StringComparison.OrdinalIgnoreCase) &&
                    (connectionString.Contains("Database", StringComparison.OrdinalIgnoreCase) ||
                     connectionString.Contains("Initial Catalog", StringComparison.OrdinalIgnoreCase)))
                return "MSSQL";

            else if (connectionString.Contains("Server", StringComparison.OrdinalIgnoreCase) &&
                    (connectionString.Contains("MySql.", StringComparison.OrdinalIgnoreCase) ||
                     connectionString.Contains("Uid=", StringComparison.OrdinalIgnoreCase)))
                return "MySQL";
            else
                return "Desconhecido";
        }

    }//Fim
}