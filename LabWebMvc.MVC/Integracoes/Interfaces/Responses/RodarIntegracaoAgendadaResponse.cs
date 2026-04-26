namespace LabWebMvc.MVC.Integracoes.Interfaces.Responses
{
    public class RodarIntegracaoAgendadaResponse
    {
        public virtual List<string>? Log { get; set; }
        public bool? HasErrors { get; set; }
        public ICollection<object>? Errors { get; set; }
    }
}