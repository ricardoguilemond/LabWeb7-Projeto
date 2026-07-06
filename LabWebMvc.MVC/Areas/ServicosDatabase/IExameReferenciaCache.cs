namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    /// <summary>
    /// Interface para o cache de referências de exames migradas do Delphi.
    /// </summary>
    public interface IExameReferenciaCache
    {
        /// <summary>
        /// Obtém o conteúdo binário da referência para um ContaExame.
        /// Se houver duplicados, retorna lista ordenada por DataCriacao.
        /// Retorna null se não encontrado (silencioso).
        /// </summary>
        List<ExameReferenciaItem>? ObterReferencias(string contaExame);

        /// <summary>
        /// Carrega todas as referências do banco para o cache.
        /// Chamado no login do usuário.
        /// </summary>
        Task CarregarCacheAsync(string nomeBanco);

        /// <summary>
        /// Verifica se o cache já foi carregado para o banco especificado.
        /// </summary>
        bool CacheCarregado(string nomeBanco);
    }

    public class ExameReferenciaItem
    {
        public byte[] ConteudoBinario { get; set; } = null!;
        public string FormatoOrigem { get; set; } = "RTF";
        public int AlinhaLaudo { get; set; }
    }
}
