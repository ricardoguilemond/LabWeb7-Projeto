namespace LabWebMvc.MVC.Integracoes.Interfaces.Responses
{
    public class ObterConfiguracoesServicoResponse
    {
        #region Properties

        public virtual string? PastaSaida { get; set; }

        public virtual string? PastaEntrada { get; set; }

        public virtual string? PastaEntradaProcessado { get; set; }

        public virtual string? PastaEntradaProcessadoErro { get; set; }

        #endregion Properties
    }
}