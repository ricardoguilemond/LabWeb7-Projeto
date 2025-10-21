namespace LabWebMvc.MVC.Integracoes.Interfaces.Responses
{
    public class GravarIntegracaoDadosExecucaoResponse
    {
        #region Properties

        public virtual bool Sucesso { get; set; }

        public virtual string? Mensagem { get; set; }

        #endregion Properties
    }
}