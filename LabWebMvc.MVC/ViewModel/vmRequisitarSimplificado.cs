namespace LabWebMvc.MVC.ViewModel
{
    public class vmRequisitarSimplificado
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string? NomePaciente { get; set; }
        public string? Nascimento { get; set; }
        public string? NomeInstituicao { get; set; }
        public string? NomePosto { get; set; }
        public string? NomeTabela { get; set; }
        public string? LaboratorioApoio { get; set; }
        public string? DataIni { get; set; }
        public string? DataEntregaParcial { get; set; }
    }

    public class CupomRequisicaoViewModel
    {
        public int IdPaciente { get; set; }
        public DateTime? Data { get; set; }
    }

}
