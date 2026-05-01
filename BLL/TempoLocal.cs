namespace BLL
{
    // TempoLocal: fallback usado quando o banco PostgreSQL não está acessível (modo offline/standalone)
    // Usa DateTime.Now do computador local — único recurso disponível sem conexão
    public class TempoLocal : ITempoServidorService
    {
        // Método síncrono
        public string ObterDataHoraServidor(string? formato = null)
        {
            DateTime agora = DateTime.Now; // horário local do computador — não depende do banco

            return formato?.ToLower() switch
            {
                "iso" => agora.ToString("o"), // ISO 8601
                _ => agora.ToString("dd/MM/yyyy HH:mm:ss") // Padrão brasileiro
            };
        }

        // Método assíncrono sem dependência externa
        public Task<DateTime?> ObterDataHoraServidorAsync()
        {
            return Task.FromResult<DateTime?>(DateTime.Now);
        }

        // Método assíncrono com formatação
        public Task<string> ObterDataHoraServidorFormatadoAsync(string? formato = null)
        {
            string resultado = ObterDataHoraServidor(formato);
            return Task.FromResult(resultado);
        }
    }
}