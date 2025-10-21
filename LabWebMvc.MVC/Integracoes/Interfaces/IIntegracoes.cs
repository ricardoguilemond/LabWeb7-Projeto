using LabWebMvc.MVC.Integracoes.Interfaces.Parameters;
using LabWebMvc.MVC.Integracoes.Interfaces.Responses;

namespace LabWebMvc.MVC.Integracoes.Interfaces
{
    /*
     * INTERFACE DAS DECLARAÇÕES DOS MÉTODOS DE RESPONSE
     *
     */

    public interface IIntegracoes
    {
        #region Methods

        ObterConfiguracoesServicoResponse ObterConfiguracoesServico(ObterConfiguracoesServicoParameter parameter);

        GravarIntegracaoDadosExecucaoResponse GravarIntegracaoDadosExecucao(GravarIntegracaoDadosExecucaoParameter parameter);

        RodarIntegracaoAgendadaResponse RodarIntegracaoAgendada(RodarIntegracaoAgendadaParameter parameter);

        #endregion Methods
    }
}