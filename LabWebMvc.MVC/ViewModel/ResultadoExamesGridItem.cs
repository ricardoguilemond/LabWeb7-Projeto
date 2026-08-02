namespace LabWebMvc.MVC.ViewModel
{
    /// <summary>
    /// Item do grid de Resultado de Exames (server-side processing).
    /// </summary>
    public class ResultadoExamesGridItem
    {
        public int Id { get; set; }
        public string NomePaciente { get; set; } = string.Empty;
        public string SiglaInstituicao { get; set; } = string.Empty;
        public string SiglaTabela { get; set; } = string.Empty;
        public string NomePosto { get; set; } = string.Empty;
        public int Sequencial { get; set; }
        public DateTime? DataFim { get; set; }
        public string NomeMedico { get; set; } = string.Empty;
        public string CRM { get; set; } = string.Empty;
        public int Situacao { get; set; }
        public int TotalImpresso { get; set; }
    }
}
