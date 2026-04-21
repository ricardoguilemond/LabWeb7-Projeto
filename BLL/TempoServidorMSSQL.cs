namespace BLL
{
    using Microsoft.Extensions.Configuration;
    using Npgsql;

    namespace BLL
    {
        public class TempoServidorMSSQL : ITempoServidorService
        {
            private readonly string _connectionString;

            public TempoServidorMSSQL(IConfiguration config)
            {
                _connectionString = config.GetSection("ConexaoPostgreSQL")["PSQLConnectionString"]
                    ?? throw new InvalidOperationException("Connection string 'PSQLConnectionString' not found.");
            }

            /*
                USO direto:
                string data = _tempoService.ObterDataHoraServidor();          // formato padrão
                string dataIso = _tempoService.ObterDataHoraServidor("iso");  // formato ISO 8601
            */

            //Feito pelo Kiro em 20/04/2026
            // Método síncrono para obter a data e hora do servidor PostgreSQL
            public string ObterDataHoraServidor(string? formato = null)
            {
                try
                {
                    using NpgsqlConnection connection = new(_connectionString);
                    connection.Open();

                    using NpgsqlCommand command = new("SELECT NOW()", connection);
                    object resultado = command.ExecuteScalar();

                    if (resultado is DateTime dataHora)
                    {
                        return formato?.ToLower() switch
                        {
                            "iso" => dataHora.ToString("o"), // ISO 8601
                            _ => dataHora.ToString("dd/MM/yyyy HH:mm:ss") // Padrão brasileiro
                        };
                    }

                    return "Data inválida";
                }
                catch (Exception ex)
                {
                    return $"Erro ao obter data do servidor: {ex.Message}";
                }
            }
            //..Kiro

            //Feito pelo Kiro em 20/04/2026
            // Método assíncrono para obter a data e hora do servidor PostgreSQL
            public async Task<DateTime?> ObterDataHoraServidorAsync()
            {
                try
                {
                    using NpgsqlConnection conn = new(_connectionString);
                    using NpgsqlCommand cmd = new("SELECT NOW()", conn);

                    await conn.OpenAsync();
                    object? result = await cmd.ExecuteScalarAsync();
                    return Convert.ToDateTime(result);
                }
                catch
                {
                    return null;
                }
            }
            //..Kiro

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
}