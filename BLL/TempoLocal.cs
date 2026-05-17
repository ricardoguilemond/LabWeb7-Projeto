namespace BLL
{
    /// <summary>
    /// Fallback offline para ITempoServidorService.
    /// Usado quando o PostgreSQL está inacessível.
    /// SEMPRE retorna UTC internamente; conversão para local ocorre apenas na formatação legacy.
    /// </summary>
    public class TempoLocal : ITempoServidorService
    {
        // ===================================================================
        // MÉTODOS UTC
        // ===================================================================

        public DateTime ObterDataHoraUtc()
        {
            return DateTime.UtcNow;
        }

        public Task<DateTime> ObterDataHoraUtcAsync()
        {
            return Task.FromResult(DateTime.UtcNow);
        }

        public string FormatarUtcParaLocal(DateTime utc, string? formato = null, string timezoneId = "America/Sao_Paulo")
        {
            if (utc.Kind != DateTimeKind.Utc)
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

            return formato?.ToLower() switch
            {
                "iso" => local.ToString("o"),
                _ => local.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        // ===================================================================
        // MÉTODOS LEGACY — delegam para os métodos UTC + conversão local
        // ===================================================================

        public string ObterDataHoraServidor(string? formato = null)
        {
            var utc = ObterDataHoraUtc();
            return FormatarUtcParaLocal(utc, formato);
        }

        public Task<DateTime?> ObterDataHoraServidorAsync()
        {
            var utc = ObterDataHoraUtc();
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            return Task.FromResult<DateTime?>(local);
        }

        public Task<string> ObterDataHoraServidorFormatadoAsync(string? formato = null)
        {
            var resultado = ObterDataHoraServidor(formato);
            return Task.FromResult(resultado);
        }
    }
}