namespace BLL
{
    /// <summary>
    /// Fonte canônica de data/hora UTC para a aplicação.
    /// 
    /// Regras:
    /// - Primária: PostgreSQL via NOW() (timestamptz / UTC)
    /// - Fallback: DateTime.UtcNow do servidor de aplicação
    /// - Proibido: usar DateTime.Now (hora local) ou dados do cliente
    /// - Exibição: conversão UTC → timezone local ocorre SOMENTE na camada de apresentação
    /// </summary>
    public interface ITempoServidorService
    {
        // ============================================================
        // MÉTODOS LEGACY — mantidos para compatibilidade com views existentes
        // Retornam string formatada no padrão brasileiro (dd/MM/yyyy HH:mm:ss)
        // ============================================================
        string ObterDataHoraServidor(string? formato = null);
        Task<DateTime?> ObterDataHoraServidorAsync();
        Task<string> ObterDataHoraServidorFormatadoAsync(string? formato = null);

        // ============================================================
        // MÉTODOS UTC — uso obrigatório para persistência e lógica de negócio
        // Retornam DateTime com Kind = Utc
        // ============================================================

        /// <summary>
        /// Retorna a data/hora UTC atual do servidor PostgreSQL.
        /// Fallback: DateTime.UtcNow do servidor de aplicação.
        /// </summary>
        DateTime ObterDataHoraUtc();

        /// <summary>
        /// Versão assíncrona de ObterDataHoraUtc().
        /// Fallback: DateTime.UtcNow do servidor de aplicação.
        /// </summary>
        Task<DateTime> ObterDataHoraUtcAsync();

        /// <summary>
        /// Converte um DateTime UTC para string no timezone local de exibição.
        /// Padrão: America/Sao_Paulo, formato dd/MM/yyyy HH:mm:ss.
        /// </summary>
        string FormatarUtcParaLocal(DateTime utc, string? formato = null, string timezoneId = "America/Sao_Paulo");
    }
}