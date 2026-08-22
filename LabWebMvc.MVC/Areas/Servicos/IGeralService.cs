namespace LabWebMvc.MVC.Areas.Servicos
{
    /* Feito pelo Qoder em 22/08/2026 — IGeralService (Dívida Técnica §1, opção A).
     *
     * Os utilitários de data/hora/fuso que antes viviam em GeralController (uma classe que herdava
     * de Controller apenas para servir de "biblioteca de funções") agora vivem neste serviço puro:
     *   - não depende de infraestrutura MVC (HttpContext, ViewData, roteamento);
     *   - pode ser injetado em qualquer classe (controllers, repositórios, serviços de background);
     *   - é testável isoladamente.
     *
     * Os métodos de renderização/validação de sessão (sobrecargas "Validacao") permanecem no
     * GeralController porque são inerentes ao MVC (resolução de View pelo ControllerContext).
     */
    public interface IGeralService
    {
        /// <summary>
        /// Data/hora do servidor formatada. iso=true → yyyy/MM/ddTHH:mm:ss.fffZ; senão dd/MM/yyyy HH:mm:ss.
        /// </summary>
        string ObterDataHoraServidor(bool iso = false);

        /// <summary>
        /// Retorna DateTime UTC para uso em persistência.
        /// Fonte: PostgreSQL (NOW()). Fallback: DateTime.UtcNow.
        /// NUNCA use DateTime.Now ou dados do cliente para timestamps de criacao.
        /// </summary>
        DateTime ObterDataHoraUtc();

        /// <summary>
        /// Retorna DateTime no timezone local (America/Sao_Paulo) com Kind=Unspecified.
        /// Uso: exibição e lógica de negócio local.
        /// NÃO use para persistência — use ObterDataHoraUtc() para isso.
        /// NÃO use como parâmetro de query EF Core com colunas timestamptz — use ConverterDataLocalParaRangeUtc().
        /// </summary>
        DateTime ObterDataHoraLocal();

        /// <summary>
        /// Retorna o range do dia atual em UTC (Kind=Utc), pronto para uso em
        /// queries EF Core que comparam com colunas timestamptz.
        ///
        /// IMPORTANTE: No Npgsql 8.x (sem legacy behavior), DateTimeKind.Unspecified
        /// causa InvalidOperationException ao comparar com timestamptz.
        /// Este método converte meia-noite local (America/Sao_Paulo) para UTC corretamente.
        /// </summary>
        (DateTime inicioUtc, DateTime fimUtc) ObterRangeDiaUtc();

        /// <summary>
        /// Converte uma data local (meia-noite America/Sao_Paulo) para range UTC (Kind=Utc),
        /// pronto para uso em queries EF Core com colunas timestamptz.
        ///
        /// Use este método quando o filtro vem de input do usuário (string dd/MM/yyyy)
        /// ou de DateTime.Parse. NUNCA passe DateTime.Kind=Unspecified diretamente ao PostgreSQL.
        /// </summary>
        (DateTime inicioUtc, DateTime fimUtc) ConverterDataLocalParaRangeUtc(DateTime dataLocal);

        /// <summary>
        /// Converte um DateTime local (America/Sao_Paulo, Kind=Unspecified) para UTC (Kind=Utc),
        /// pronto para gravação em colunas timestamptz.
        /// </summary>
        DateTime ConverterLocalParaUtc(DateTime dataLocal);
    }
}
