using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BLL
{
    // Fonte canônica de data/hora do servidor PostgreSQL
    // Usa AT TIME ZONE 'America/Sao_Paulo' — retorna sempre horário de Brasília
    // Fallback automático para DateTime.Now local se banco inacessível (modo offline)
    public class TempoServidorPostgreSQL : ITempoServidorService
    {
        private readonly string _connectionString;

        public TempoServidorPostgreSQL(IConfiguration config)
        {
            _connectionString = config.GetSection("ConexaoPostgreSQL")["PSQLConnectionString"]
                ?? throw new InvalidOperationException("Connection string 'PSQLConnectionString' not found.");
        }

        /*
            USO direto:
            string data = _tempoService.ObterDataHoraServidor();          // formato padrão
            string dataIso = _tempoService.ObterDataHoraServidor("iso");  // formato ISO 8601
        */

        // Método síncrono para obter a data e hora do servidor PostgreSQL no fuso de Brasília
        // Usa AT TIME ZONE 'America/Sao_Paulo' para garantir horário correto
        // independente do fuso do servidor .NET ou do cliente
        // Fallback: se o banco não estiver acessível (modo offline/standalone), usa DateTime.Now do computador local
        public string ObterDataHoraServidor(string? formato = null)
        {
            try
            {
                using NpgsqlConnection connection = new(_connectionString);
                connection.Open();

                // AT TIME ZONE 'America/Sao_Paulo': converte UTC → horário de Brasília no banco
                using NpgsqlCommand command = new("SELECT NOW() AT TIME ZONE 'America/Sao_Paulo'", connection);
                object? resultado = command.ExecuteScalar();

                if (resultado is DateTime dataHora)
                {
                    return formato?.ToLower() switch
                    {
                        "iso" => dataHora.ToString("o"), // ISO 8601
                        _ => dataHora.ToString("dd/MM/yyyy HH:mm:ss") // Padrão brasileiro
                    };
                }

                // Banco retornou valor inesperado: fallback para data local
                return FormatarDataLocal(DateTime.Now, formato);
            }
            catch
            {
                // Banco inacessível (offline/standalone): usa data do computador local
                return FormatarDataLocal(DateTime.Now, formato);
            }
        }

        // Método assíncrono para obter a data e hora do servidor PostgreSQL no fuso de Brasília
        // Fallback: se o banco não estiver acessível, usa DateTime.Now do computador local
        public async Task<DateTime?> ObterDataHoraServidorAsync()
        {
            try
            {
                using NpgsqlConnection conn = new(_connectionString);
                // AT TIME ZONE 'America/Sao_Paulo': converte UTC → horário de Brasília no banco
                using NpgsqlCommand cmd = new("SELECT NOW() AT TIME ZONE 'America/Sao_Paulo'", conn);

                await conn.OpenAsync();
                object? result = await cmd.ExecuteScalarAsync();
                return Convert.ToDateTime(result);
            }
            catch
            {
                // Banco inacessível: fallback para data local
                return DateTime.Now;
            }
        }

        // Helper privado: formata um DateTime conforme o parâmetro formato
        private static string FormatarDataLocal(DateTime dt, string? formato) =>
            formato?.ToLower() switch
            {
                "iso" => dt.ToString("o"),
                _ => dt.ToString("dd/MM/yyyy HH:mm:ss")
            };

        // Método assíncrono com formatação
        public async Task<string> ObterDataHoraServidorFormatadoAsync(string? formato = null)
        {
            DateTime? dataHora = await ObterDataHoraServidorAsync();

            if (dataHora == null)
                return "Data inválida";

            return formato?.ToLower() switch
            {
                "iso" => dataHora.Value.ToString("o"),
                _ => dataHora.Value.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }
    }
}