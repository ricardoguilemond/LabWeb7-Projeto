using ExtensionsMethods.EventViewerHelper;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.RecaptchaEnterprise.V1;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public class CreateAssessmentSample
    {
        private readonly GoogleReCaptchaSettings _captchaSettings;
        private readonly Db _db;
        public CreateAssessmentSample(IOptions<GoogleReCaptchaSettings> captchaSettings,
                                      IConnectionService connectionService,
                                      IEventLogHelper eventLogHelper)
        {
            _captchaSettings = captchaSettings.Value;

            var optionsBuilder = new DbContextOptionsBuilder<Db>().UseNpgsql(connectionService.GetConnectionString());
            _db = new Db(optionsBuilder.Options, connectionService, eventLogHelper);
        }

        /* **************************************************************************************************************
         * PARA ESTA ROTINA DE VALIDAÇÃO DO GOOGLE FUNCIONAR É PRECISO INSTALAR O "GOOGLE CLOUD SDK" (EXECUTÁVEL)
         * QUE PODE SER BAIXADO NO SITE DO GOOGLE, NA SUA CONTA PARTICULAR DO GOOGLE CLOUD.
         * O ARQUIVO SE CHAMADO: GoogleCloudSDKInstaller.EXE
         * *************************************************
         * ESSA INSTALAÇÃO É OBRIGATÓRIA PARA CRIAR A CREDENCIAL NECESSÁRIA PARA AS VALIDAÇÕES QUE COLOCA O
         * SITE/SISTEMA/APLICATIVO NOS RELATÓRIOS DE AVALIAÇÃO ACEITÁVEIS COMO SEGURO PELO RECAPTCHA DO GOOGLE.
         * *************************************************************************************************************
         * IMPORTANTE ::: Instale ou inicilize a CLI CLOUD (GoogleCloudSDKInstaller.EXE), conforme descrito a seguir:
         *
         * INPORTANTE ::: DEPOIS DE INSTALADO, DEVE-SE EXECUTAR NA LINHA DE COMANDO DO PROMPT: (Sempre que o serviço for reiniciado)
         * >>     gcloud auth application-default login
         *
         * PARA PODER CRIAR O LOGIN AUTOMÁTICO NECESSÁRIO PARA O CONTROLE DAS CREDENCIAIS DO CLOUD/RECAPTCHA.
         * Essa CREDENCIAL refere-se ao conteúdo intitulado "credenciais local usado pelo ADC".
         * (https://cloud.google.com/docs/authentication/provide-credentials-adc#local-dev)
         * (Será pedido uma conta para uso dessa credencial, estou usando rguilemond@gmail.com)
         * (usar preferencialmente um conta do GMAIL ou OUTLOOK para todos os serviços referentes ao Google)
         * DOS NÍVEIS DE PONTUAÇÃO PELA AVALIAÇÃO DO GOOGLE:
         * Dos 11 níveis, apenas os quatro níveis de pontuação a seguir estão
         * disponíveis por padrão: 0.1, 0.3, 0.7 e 0.9. Para os demais níveis é preciso solicitar análise do Google.
         * Sendo satisfatório a partir do nível '.5', e melhor a partir de '.7'.
         * *************************************************************************************************************
         * Nome do Projeto: no appsetting.json
         * Número do projeto: 131388738053
         * Id do Projeto: labwebmvc
         * Chave Pública Google ReCaptcha: 6Le_QQYiAAAAANuFhenHQ5DpfJCGIfa2X1O51ltB
         */
        // Create an assessment to analyze the risk of an UI action.
        // projectID: GCloud Project ID.
        // recaptchaSiteKey: Site key obtained by registering a domain/app to use recaptcha.
        // token: The token obtained from the client on passing the recaptchaSiteKey.
        // recaptchaAction: Action name corresponding to the token.
        /* *************************************************************************************************************
            TODAS AS INFORMAÇÕES E PARâMETROS DAS CONFIGURAÇÕES ESTÃO NA MINHA CONTA RECAPTCHA DO GOOGLE ENTERPRISE
            ************************************************************************************************************
            public void CreateAssessment(string projectID = "project-id",
                                         string token = "action-token",
                                         string recaptchaAction = "action-name")
            -----------------------------------------------------------------------------------------------------------
            Validar a configuração correta das credenciais da conta do Google Cloud para ReCaptcha e tokens
            e contabilização dos acessos, quando ultrapassar 10.000 (dez mil) acessos no mês, passa a
            pagar $1.00 (um dólar) a cada mil acessos realizados.
            -----------------------------------------------------------------------------------------------------------
         */

        public async Task<ICollection<string>> CreateAssessment(string token = "token-obtido-do-retorno-google", string projectID = "labwebmvc", string recaptchaAction = "login")
        {
            List<string> mensagens = [];

            //-------------------------------------------------------------------------------------------------------------
            // Create the client.
            // TODO: To avoid memory issues, move this client generation outside
            // of this example, and cache it (recommended) or call client.close()
            // before exiting this method.
            RecaptchaEnterpriseServiceClient clientGoogle = RecaptchaEnterpriseServiceClient.Create();
            string logMessage = string.Empty;

            try
            {
                ProjectName projectName = new(projectID);

                // Build the assessment request.
                CreateAssessmentRequest createAssessmentRequest = new()
                {
                    Assessment = new Assessment()
                    {
                        // Set the properties of the event to be tracked.
                        Event = new Event()
                        {
                            // The token obtained from the client on passing the recaptchaSiteKey.
                            Token = token,
                            SiteKey = _captchaSettings.SecretKey,      //curiosamente o "SiteKey" é o SecretKey de chave pública, e não a privada!
                            ExpectedAction = recaptchaAction
                        },
                    },
                    ParentAsProjectName = projectName
                };

                logMessage = "Criando avaliação do ReCaptcha Enterprise com os seguintes parâmetros: " +
                             "\n\nProjectID: " + projectID +
                             "\n\nToken: " + token +
                             "\n\nAction: " + recaptchaAction;

                Assessment response = new Assessment();

                //Recupera os dados do Google ReCaptcha com sua pontuação e possíveis erros de validação.
                try
                {
                    response = await clientGoogle.CreateAssessmentAsync(createAssessmentRequest);
                }
                catch (Grpc.Core.RpcException ex)
                {
                    // Log detalhado
                    mensagens.Add("Erro ao criar avaliação do ReCaptcha Enterprise: " + ex);
                }
                if (response == null)
                {
                    mensagens.Add(@"ERRO: A chamada ao Google ReCaptcha Enterprise não retornou nenhum resultado.");
                }
                else if (response.TokenProperties.Valid == false)
                {
                    mensagens.Add(@"ERRO: token inválido - motivo: " + response.TokenProperties.InvalidReason.ToString());
                }
                else if (response.TokenProperties.Action == null)
                {
                    mensagens.Add(@"ERRO: A ação do token é nula ou vazia, o que não é esperado.");
                }
                else if (response.RiskAnalysis == null)
                {
                    mensagens.Add(@"ERRO: A análise de risco retornada pelo ReCaptcha é nula.");
                }
                else if (response.RiskAnalysis.Score >= 0.7f)
                {
                    mensagens.Add(@"O Google ReCaptcha reconheceu esta validação como sendo segura para este acesso.");
                }
                else if (response.RiskAnalysis.Score <= 0.3f)
                {
                    mensagens.Add(@"O Google ReCaptcha reconheceu esta validação como sendo extremamente insegura para este acesso. Possível automatização de robôs.");
                }
                else if (response.RiskAnalysis.Score > 0.3f && response.RiskAnalysis.Score < 0.5f)
                {
                    mensagens.Add(@"O Google ReCaptcha reconheceu esta validação como sendo insegura para este acesso.");
                }
                else if (response.TokenProperties.Action != recaptchaAction)
                {
                    if (string.IsNullOrEmpty(response.TokenProperties.Action)) response.TokenProperties.Action = "vazio/nula";
                    mensagens.Add(@"ERRO: The action attribute in reCAPTCHA tag is: " + response.TokenProperties.Action.ToString());
                    mensagens.Add(@"ERRO: The action attribute in the reCAPTCHA tag does not match the action you are expecting to score.");
                }

                // Get the risk score and the reason(s).
                // For more information on interpreting the assessment,
                // see: https://cloud.google.com/recaptcha-enterprise/docs/interpret-assessment
                // -------------------------------------------------------------------------------------------------
                if (response != null && response.RiskAnalysis != null)
                {
                    mensagens.Add(logMessage);
                    mensagens.Add(@"O reCAPTCHA score para este Site é: " + ((decimal)response.RiskAnalysis.Score) + ", sendo satisfatório a partir de '.5' e ótimo a partir de '.7'");
                    mensagens.Add(@"Abaixo 'pode' ter uma lista da análise de possíveis riscos retornados pelo ReCaptcha:");

                    foreach (RiskAnalysis.Types.ClassificationReason reason in response.RiskAnalysis.Reasons)
                    {
                        mensagens.Add(reason.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                mensagens.Add(ex.Message);
            }
            finally //nunca vamos interromper o fluxo, independente dos problemas, pois trata-se apenas de análise do ReCaptcha!
            { }

            return mensagens;
        }

        //USO EXEMPLO:
        //public static void Main(string[] args)
        //{
        //    new CreateAssessmentSample().createAssessment(); //pode chamar somente essa linha em qualquer interface.
        //}
    }// fim da Classe: CreateAssessmentSample
}