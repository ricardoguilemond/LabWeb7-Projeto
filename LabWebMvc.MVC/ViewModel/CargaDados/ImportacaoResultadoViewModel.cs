namespace LabWebMvc.MVC.ViewModel.CargaDados
{
    public class ImportacaoResultadoViewModel
    {
        public string NomeFirebird { get; set; } = string.Empty;
        public string NomePostgreSQL { get; set; } = string.Empty;
        public long TotalLido { get; set; }
        public long Inseridos { get; set; }
        public long Duplicados { get; set; }
        public long Erros { get; set; }
        /// <summary>
        /// Tempo gasto na importação da tabela, em segundos.
        /// </summary>
        public double TempoGasto { get; set; }
        public bool Concluido { get; set; }
        public string? MensagemErro { get; set; }
    }

    public class ImportacaoFinalViewModel
    {
        public List<ImportacaoResultadoViewModel> Resultados { get; set; } = new();
        /// <summary>
        /// Tempo total da importação, em segundos.
        /// </summary>
        public double TempoTotal { get; set; }
        public bool ModoSimulacao { get; set; }
        public string? MensagemFinal { get; set; }
    }
}
