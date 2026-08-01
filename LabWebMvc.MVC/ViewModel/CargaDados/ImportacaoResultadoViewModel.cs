namespace LabWebMvc.MVC.ViewModel.CargaDados
{
    public class ErroRegistroViewModel
    {
        public string Tabela { get; set; } = string.Empty;
        public string? Chave { get; set; }
        public string? SqlState { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class ImportacaoResultadoViewModel
    {
        public string NomeFirebird { get; set; } = string.Empty;
        public string NomePostgreSQL { get; set; } = string.Empty;
        public long TotalLido { get; set; }
        public long Inseridos { get; set; }
        public long Duplicados { get; set; }
        public long Erros { get; set; }
        public long Ignorados { get; set; }
        /// <summary>
        /// Tempo gasto na importação da tabela, em segundos.
        /// </summary>
        public double TempoGasto { get; set; }
        public bool Concluido { get; set; }
        public string? MensagemErro { get; set; }
        /// <summary>
        /// Observação sobre o resultado da importação da tabela.
        /// Descreve problemas encontrados, registros órfãos, FKs ausentes, etc.
        /// </summary>
        public string? Observacao { get; set; }
        public List<ErroRegistroViewModel> DetalhesErros { get; set; } = new();
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

        //Feito pelo Kiro em 27/07/2026
        /// <summary>
        /// Quantidade de registros duplicados de Pacientes removidos na deduplicação pós-importação.
        /// </summary>
        public int DeduplicacaoPacientes { get; set; }

        /// <summary>
        /// Quantidade de registros duplicados de Médicos removidos na deduplicação pós-importação.
        /// </summary>
        public int DeduplicacaoMedicos { get; set; }
        //..Kiro
    }
}
