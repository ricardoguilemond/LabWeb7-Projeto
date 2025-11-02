using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    //Desserialização Json do objeto response retornado do ReCaptcha
    public class ReCaptchaGoogleResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("score")]
        public float? Score { get; set; }

        [JsonProperty("action")]
        public string? Action { get; set; }

        [JsonProperty("challenge_ts")]
        public DateTime? ChallengeTs { get; set; }

        [JsonProperty("hostname")]
        public string? Hostname { get; set; }

        [JsonProperty("error-codes")]
        public List<string>? ErrorCodes { get; set; }
    }

    public class GoogleReCaptchaSettings
    {
        public bool ExecutaAvaliacaoRecaptcha { get; set; } = false; //se for false, não executa a avaliação do ReCaptcha Enterprise do Google / true = tem custos!.
        public string SiteKey { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public string ProjectID { get; set; } = null!; //Project ID do Google Cloud, onde está o ReCaptcha Enterprise.
    }

    public class ValidacaoGoogleReCaptcha : IValidacaoGoogleReCaptcha
    {
        private readonly GoogleReCaptchaSettings _captchaSettings;
        private readonly CreateAssessmentSample _captchaService;
        private readonly Db _db;
        private readonly IEventLogHelper _eventLog;
        public ValidacaoGoogleReCaptcha(IOptions<GoogleReCaptchaSettings> captchaSettings,
                                        CreateAssessmentSample captchaService,
                                        IEventLogHelper eventLogHelper,
                                        IConnectionService connectionService)
        {
            _captchaSettings = captchaSettings.Value;
            _captchaService = captchaService;
            _eventLog = eventLogHelper;

            var optionsBuilder = new DbContextOptionsBuilder<Db>().UseNpgsql(connectionService.GetConnectionString());
            _db = new Db(optionsBuilder.Options, connectionService, eventLogHelper);
        }

        public bool IsCaptchaValid(vmLogin vm)
        {
            bool isValid = false;
            string errors = string.Empty;

            try
            {
                string url = string.Format(_captchaSettings.SiteKey + "{0}", _captchaSettings.SecretKey);

                HttpClient client = new();
                HttpResponseMessage recaptchaResponse = client.GetAsync(url, (System.Net.Http.HttpCompletionOption)int.Parse("1")).Result;
                string _JsonString = recaptchaResponse.Content.ReadAsStringAsync().Result;

                GenericValidations.ConteudoCompletoReponseCaptchaGoogle = recaptchaResponse;

                ReCaptchaGoogleResponse? googleReCaptchaResponse = null;
                if (!_JsonString.Contains("PLEASE DO NOT COPY AND PASTE THIS CODE"))
                {
                    try
                    {
                        googleReCaptchaResponse = JsonConvert.DeserializeObject<ReCaptchaGoogleResponse>(_JsonString);
                    }
                    catch (Exception ex)
                    {
                        _eventLog.LogEventViewer("IsCaptchaValid ::: exception: " + ex.Message, "wError");
                    }
                }

                if (googleReCaptchaResponse != null)
                {
                    isValid = (recaptchaResponse.IsSuccessStatusCode || googleReCaptchaResponse.Success)
                              && (googleReCaptchaResponse.ErrorCodes == null || googleReCaptchaResponse.ErrorCodes.Count == 0);

                    if (!isValid && googleReCaptchaResponse.ErrorCodes != null)
                        errors = string.Join(",", googleReCaptchaResponse.ErrorCodes);
                }
                else
                {
                    isValid = recaptchaResponse.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                _eventLog.LogEventViewer("IsCaptchaValid - ERRORS: " + errors + " ::: exception: " + ex.Message, "wError");
            }
            return isValid;
        }

        public async Task<bool> IsCaptchaValidAsync(string token)
        {
            using (HttpClient client = new())
            {
                Dictionary<string, string> values = new()
                {
                                    { "secret", _captchaSettings.SecretKey },
                                    { "response", token }
                                 };

                FormUrlEncodedContent content = new(values);

                HttpResponseMessage response = await client.PostAsync(_captchaSettings.SiteKey, content);
                string responseString = await response.Content.ReadAsStringAsync();

                ReCaptchaGoogleResponse? result = JsonConvert.DeserializeObject<ReCaptchaGoogleResponse>(responseString);

                return result != null && result.Success && (result.ErrorCodes == null || result.ErrorCodes.Count == 0);
            }
        }

        public ICollection<string> ValidaReCaptchaResponse(HttpResponseMessage? conteudo, ref ICollection<string> listaErros)
        {
            //Valida a data e hora em que o Captcha fica disponível, mas se vier data nula, então vamos considerar a data now-1dia para forçar data expirada.
            DateTimeOffset dataExpira = conteudo != null && conteudo.Headers.Date != null ? conteudo.Headers.Date.Value : DateTimeOffset.Now.AddDays(-1);
            if (dataExpira.DateTime <= DateTime.UtcNow)
                listaErros.Add("Data do ReCaptcha expirada (" + dataExpira.DateTime.ToString() + ")");

            /* Estamos com pontuação 0.9 (que significa baixo risco = ótimo)
             * A pontuação varia entre 0 e 1, onde 0.5 é razoável/regular (aceito), mas menor que 0.5 tem risco!
             * Quando menor a pontuação mais risco possui, quanto maior a pontuação melhor! Sendo 1 máximo!
             * */
            string mensagem = string.Empty;
            foreach (string item in listaErros)
            {
                mensagem = mensagem + item + "\n";
            }
            mensagem = mensagem + "\n\n" + ":: Pontuação 0.7 -> 0.9 ótimo e 1.0 excelente! \n\n" + ":: Se 0.5 é razoável/regular (aceito) \n\n" + ":: Pontuação menor que 0.5 páginas em risco/provável atividade automatizada na página.";

            _eventLog.LogEventViewer("Validação ReCaptcha Google ::: " + mensagem, "wWarning");
            return listaErros;
        }
    }
}