namespace BLL
{
    public class TempoLocal : ITempoServidorService
    {
        // Método síncrono
        public string ObterDataHoraServidor(string? formato = null)
        {
            DateTime agora = DateTime.UtcNow;

            return formato?.ToLower() switch
            {
                "iso" => agora.ToString("o"), // ISO 8601
                _ => agora.ToString("dd/MM/yyyy HH:mm:ss") // Padrão brasileiro
            };
        }

        // Método assíncrono sem dependência externa
        public Task<DateTime?> ObterDataHoraServidorAsync()
        {
            return Task.FromResult<DateTime?>(DateTime.UtcNow);
        }

        // Método assíncrono com formatação
        public Task<string> ObterDataHoraServidorFormatadoAsync(string? formato = null)
        {
            string resultado = ObterDataHoraServidor(formato);
            return Task.FromResult(resultado);
        }
    }
}