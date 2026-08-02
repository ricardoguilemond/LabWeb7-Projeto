namespace LabWebMvc.MVC.ViewModel
{
    /// <summary>
    /// Item do grid de Consultar Exames (server-side processing).
    /// Representa tanto ExamesRealizados quanto ExamesRealizadosAM de forma unificada.
    /// </summary>
    public class ConsultarExamesGridItem
    {
        public int Id { get; set; }
        public string SiglaTabela { get; set; } = string.Empty;
        public string SiglaInstituicao { get; set; } = string.Empty;
        public string NomeInstituicao { get; set; } = string.Empty;
        public string SiglaPosto { get; set; } = string.Empty;
        public string NomePosto { get; set; } = string.Empty;
        public string NomePaciente { get; set; } = string.Empty;
        public DateTime Nascimento { get; set; }
        public int Sequencial { get; set; }
        public DateTime DataIni { get; set; }
        public int Liberacao { get; set; }
        public int Baixado { get; set; }
        public string SituacaoExame { get; set; } = string.Empty;
    }
}
