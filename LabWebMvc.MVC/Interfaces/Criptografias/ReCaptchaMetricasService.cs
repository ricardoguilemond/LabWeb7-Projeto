using ExtensionsMethods.EventViewerHelper;
using Google.Api.Gax.Grpc;
using Google.Api.Gax.ResourceNames;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Monitoring.V3;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public interface IReCaptchaMetricasService
    {
        Task<long?> ObterTotalAvaliacoesMesAtualAsync(string projectId);
    }

    public class ReCaptchaMetricasService : IReCaptchaMetricasService
    {
        private readonly GoogleReCaptchaSettings _captchaSettings;
        private readonly IEventLogHelper _eventLog;

        public ReCaptchaMetricasService(IOptions<GoogleReCaptchaSettings> captchaSettings, IEventLogHelper eventLog)
        {
            _captchaSettings = captchaSettings.Value;
            _eventLog = eventLog;
        }

        private async Task<MetricServiceClient> CriarClientAsync()
        {
            string? credentialsPath = _captchaSettings.CredentialsPath;

            if (!string.IsNullOrWhiteSpace(credentialsPath) && File.Exists(credentialsPath))
            {
                _eventLog.LogEventViewer($"[ReCaptchaMetricas] Usando Service Account: {credentialsPath}", "wInfo");
                GoogleCredential credential = (await GoogleCredential.FromFileAsync(credentialsPath, CancellationToken.None))
                    .CreateScoped(MetricServiceClient.DefaultScopes);

                return new MetricServiceClientBuilder
                {
                    Credential = credential
                }.Build();
            }

            _eventLog.LogEventViewer("[ReCaptchaMetricas] Usando Application Default Credentials (ADC).", "wInfo");
            return await MetricServiceClient.CreateAsync();
        }

        public async Task<long?> ObterTotalAvaliacoesMesAtualAsync(string projectId)
        {
            try
            {
                MetricServiceClient client = await CriarClientAsync();

                DateTime agora = DateTime.UtcNow;
                DateTime inicioMes = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                Timestamp inicio = Timestamp.FromDateTime(inicioMes);
                Timestamp fim = Timestamp.FromDateTime(agora.AddMinutes(1)); // margem pequena

                string projectName = $"projects/{projectId}";

                ListTimeSeriesRequest request = new()
                {
                    Name = projectName,
                    Filter = "metric.type=\"recaptchaenterprise.googleapis.com/assessment_count\"",
                    Interval = new TimeInterval
                    {
                        StartTime = inicio,
                        EndTime = fim
                    },
                    View = ListTimeSeriesRequest.Types.TimeSeriesView.Full,
                    Aggregation = new Aggregation
                    {
                        AlignmentPeriod = Duration.FromTimeSpan(TimeSpan.FromDays(1)),
                        PerSeriesAligner = Aggregation.Types.Aligner.AlignSum
                    }
                };

                var resultados = client.ListTimeSeries(request);
                long total = 0;
                int serieCount = 0;
                int pontoCount = 0;

                foreach (var serie in resultados)
                {
                    serieCount++;
                    foreach (var ponto in serie.Points)
                    {
                        pontoCount++;
                        if (ponto.Value.ValueCase == TypedValue.ValueOneofCase.Int64Value)
                            total += ponto.Value.Int64Value;
                    }
                }

                _eventLog.LogEventViewer($"[ReCaptchaMetricas] Consulta concluída para {projectId}. Séries: {serieCount}, Pontos: {pontoCount}, Total acumulado: {total}", "wInfo");
                return total;
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer($"[ReCaptchaMetricas] Erro ao consultar métricas do Google: {ex.Message}", "wError");
                return null;
            }
        }
    }
}
