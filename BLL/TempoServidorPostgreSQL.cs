using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BLL
{
    /// <summary>
    /// Fonte canônica de data/hora UTC para a aplicação LabWeb7.
    /// 
    /// ARQUITETURA:
    /// - Fonte primária: PostgreSQL via SELECT NOW() (retorna timestamptz = UTC)
    /// - Fallback: DateTime.UtcNow do servidor de aplicação (nunca DateTime.Now)
    /// - Proibido: usar hora do cliente (browser/frontend)
    /// - Armazenamento: UTC no banco (timestamptz)
    /// - Exibição: conversão UTC → America/Sao_Paulo SOMENTE na camada de apresentação
    /// </summary>
    public class TempoServidorPostgreSQL : ITempoServidorService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Construtor usado pelo DI — recebe IConfiguration e aplica a substituição
        /// de credenciais (usubanco→sistema, ususenha→senha real) igual ao ConnectionService.
        /// Isso é necessário porque o appsettings.json contém placeholders.
        /// </summary>
        public TempoServidorPostgreSQL(IConfiguration config)
        {
            var raw = config.GetSection("ConexaoPostgreSQL")["PSQLConnectionString"]
                ?? throw new InvalidOperationException("Connection string 'PSQLConnectionString' not found.");

            var userId = config.GetSection("LoginPadraoSistema")?["Sistema"] ?? "sistema";
            var password = config.GetSection("LoginPadraoSistema")?["Senha"] ?? "Acer@105";

            _connectionString = raw
                .Replace("usubanco", userId)
                .Replace("ususenha", password);
        }

        /// <summary>
        /// Construtor alternativo — recebe a connection string já processada.
        /// Ideal para injeção via IConnectionService.GetConnectionString().
        /// </summary>
        public TempoServidorPostgreSQL(string connectionString)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // ===================================================================
        // MÉTODOS UTC — uso obrigatório para persistência e lógica de negócio
        // ===================================================================

        /// <summary>
        /// Retorna a data/hora UTC atual do servidor PostgreSQL.
        /// Query: SELECT NOW() → retorna timestamptz (UTC).
        /// Fallback: DateTime.UtcNow do servidor de aplicação.
        /// </summary>
        public DateTime ObterDataHoraUtc()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                using var command = new NpgsqlCommand("SELECT NOW()", connection);
                object? resultado = command.ExecuteScalar();

                if (resultado is DateTime dataHora)
                {
                    // Garante Kind=Utc independente do que o Npgsql retornar
                    return DateTime.SpecifyKind(dataHora, DateTimeKind.Utc);
                }

                // Banco retornou valor inesperado: fallback controlado
                return DateTime.UtcNow;
            }
            catch
            {
                // Banco inacessível: fallback controlado com UTC
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Versão assíncrona de ObterDataHoraUtc().
        /// Query: SELECT NOW() → retorna timestamptz (UTC).
        /// Fallback: DateTime.UtcNow do servidor de aplicação.
        /// </summary>
        public async Task<DateTime> ObterDataHoraUtcAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                using var cmd = new NpgsqlCommand("SELECT NOW()", conn);

                await conn.OpenAsync();
                object? result = await cmd.ExecuteScalarAsync();

                if (result is DateTime dataHora)
                {
                    return DateTime.SpecifyKind(dataHora, DateTimeKind.Utc);
                }

                return DateTime.UtcNow;
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Converte um DateTime UTC para string no timezone local de exibição.
        /// Padrão: America/Sao_Paulo, formato dd/MM/yyyy HH:mm:ss.
        /// </summary>
        public string FormatarUtcParaLocal(DateTime utc, string? formato = null, string timezoneId = "America/Sao_Paulo")
        {
            if (utc.Kind != DateTimeKind.Utc)
            {
                // Se receber Unspecified/Local, assume que já está em UTC
                // (defesa contra chamadas incorretas)
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }

            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

            return formato?.ToLower() switch
            {
                "iso" => local.ToString("o"),
                _ => local.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        // ===================================================================
        // MÉTODOS LEGACY — mantidos para compatibilidade com views/controllers existentes
        // Internamente delegam para os métodos UTC + conversão local
        // ===================================================================

        /// <summary>
        /// LEGACY: retorna string formatada no timezone local (dd/MM/yyyy HH:mm:ss).
        /// Para persistência, use ObterDataHoraUtc() em vez deste método.
        /// </summary>
        public string ObterDataHoraServidor(string? formato = null)
        {
            var utc = ObterDataHoraUtc();
            return FormatarUtcParaLocal(utc, formato);
        }

        /// <summary>
        /// LEGACY: retorna DateTime? do timezone local (Kind=Unspecified).
        /// Para persistência, use ObterDataHoraUtcAsync() em vez deste método.
        /// </summary>
        public async Task<DateTime?> ObterDataHoraServidorAsync()
        {
            var utc = await ObterDataHoraUtcAsync();
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            return local;
        }

        /// <summary>
        /// LEGACY: retorna string formatada de forma assíncrona.
        /// Para persistência, use ObterDataHoraUtcAsync() em vez deste método.
        /// </summary>
        public async Task<string> ObterDataHoraServidorFormatadoAsync(string? formato = null)
        {
            var utc = await ObterDataHoraUtcAsync();
            return FormatarUtcParaLocal(utc, formato);
        }
    }
}