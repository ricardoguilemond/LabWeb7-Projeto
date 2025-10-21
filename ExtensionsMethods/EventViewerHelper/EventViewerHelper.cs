using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ExtensionsMethods.EventViewerHelper
{
    public class EventLogHelper : IEventLogHelper
    {
        public Func<string?>? ObterCNPJ { get; set; }
        public Func<string?>? ObterNomeEmpresa { get; set; }

        public enum CustomLogEntryType
        {
            Information,
            Warning,
            Error,
            SuccessAudit,
            FailureAudit
        }

        private readonly Dictionary<string, CustomLogEntryType> CustomEventTypeMapping = new()
        {
            { "wInfo", CustomLogEntryType.Information },
            { "wWarning", CustomLogEntryType.Warning },
            { "wError", CustomLogEntryType.Error },
            { "wSuccessAudit", CustomLogEntryType.SuccessAudit },
            { "wFailureAudit", CustomLogEntryType.FailureAudit }
        };

        private readonly string LinuxLogPath = "/var/log/labwebmvc/custom_log.txt";

        public void LogEventViewer(string mensagem, string tipoEvento = "wInfo", string origem = "LabWebMvc")
        {
            if (!OperatingSystem.IsWindows())
                return; // ignora silenciosamente em não-Windows

            RegistraLogEventViewer(mensagem, tipoEvento, origem);
        }

        [SupportedOSPlatform("windows")]
        private void RegistraLogEventViewer(string mensagem, string tipoEvento = "wInfo", string origem = "LabWebMvc")
        {
            string identificacao = "[" + (ObterCNPJ?.Invoke() ?? "") + " - " + (ObterNomeEmpresa?.Invoke() ?? "") + "]";
            string mensagemFinal = $"{identificacao}[LabWebMvc - Sistema Laboratorial] ::: {mensagem}, em {DateTime.Now}";

            if (!CustomEventTypeMapping.TryGetValue(tipoEvento, out CustomLogEntryType tipo))
            {
                Console.WriteLine("Tipo de log não reconhecido.");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                LogNoEventViewer(mensagemFinal, tipo, origem);
            }
            else
            {
                LogEmArquivo(mensagemFinal, tipo, origem);
            }
        }

        [SupportedOSPlatform("windows")]
        private void LogNoEventViewer(string mensagem, CustomLogEntryType tipo, string origem)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(origem) || string.IsNullOrWhiteSpace(mensagem))
                {
                    throw new ArgumentException("Origem ou mensagem do log não pode ser nula ou vazia.");
                }

                if (!EventLog.SourceExists(origem))
                {
                    EventLog.CreateEventSource(new EventSourceCreationData(origem, "Application"));
                }

                EventLogEntryType tipoEvento = tipo switch
                {
                    CustomLogEntryType.Information => EventLogEntryType.Information,
                    CustomLogEntryType.Warning => EventLogEntryType.Warning,
                    CustomLogEntryType.Error => EventLogEntryType.Error,
                    CustomLogEntryType.SuccessAudit => EventLogEntryType.SuccessAudit,
                    CustomLogEntryType.FailureAudit => EventLogEntryType.FailureAudit,
                    _ => EventLogEntryType.Information
                };

                EventLog.WriteEntry(origem, mensagem, tipoEvento);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar log no Windows: {ex.Message}");
            }
        }

        private void LogEmArquivo(string mensagem, CustomLogEntryType tipo, string origem)
        {
            try
            {
                string linha = $"[{DateTime.Now}] Origem: {origem}, Tipo: {tipo}, Mensagem: {mensagem}{Environment.NewLine}";
                string? diretorio = Path.GetDirectoryName(LinuxLogPath);

                if (!Directory.Exists(diretorio))
                {
                    Directory.CreateDirectory(diretorio!);
                }

                File.AppendAllText(LinuxLogPath, linha);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gravar log em arquivo: {ex.Message}");
            }
        }

        public async Task LogEventViewerAsync(string mensagem, string tipoEvento = "wInfo", string origem = "LabWebMvc")
        {
            if (!OperatingSystem.IsWindows())
                return; // ignora silenciosamente em não-Windows

            await RegistraLogEventViewerAsync(mensagem, tipoEvento, origem);
        }

        [SupportedOSPlatform("windows")]
        private async Task LogNoEventViewerAsync(string mensagem, CustomLogEntryType tipo, string origem)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(origem) || string.IsNullOrWhiteSpace(mensagem))
                {
                    throw new ArgumentException("Origem ou mensagem do log não pode ser nula ou vazia.");
                }

                // Executa a checagem e criação da fonte do log em thread separada
                await Task.Run(() =>
                {
                    if (!EventLog.SourceExists(origem))
                    {
                        EventLog.CreateEventSource(new EventSourceCreationData(origem, "Application"));
                    }
                });

                EventLogEntryType tipoEvento = tipo switch
                {
                    CustomLogEntryType.Information => EventLogEntryType.Information,
                    CustomLogEntryType.Warning => EventLogEntryType.Warning,
                    CustomLogEntryType.Error => EventLogEntryType.Error,
                    CustomLogEntryType.SuccessAudit => EventLogEntryType.SuccessAudit,
                    CustomLogEntryType.FailureAudit => EventLogEntryType.FailureAudit,
                    _ => EventLogEntryType.Information
                };

                // Escreve a entrada do log de forma assíncrona
                await Task.Run(() =>
                {
                    EventLog.WriteEntry(origem, mensagem, tipoEvento);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar log no Windows: {ex.Message}");
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task RegistraLogEventViewerAsync(string mensagem, string tipoEvento = "wInfo", string origem = "LabWebMvc")
        {
            string identificacao = "[" + (ObterCNPJ?.Invoke() ?? "") + " - " + (ObterNomeEmpresa?.Invoke() ?? "") + "]";
            string mensagemFinal = $"{identificacao}[LabWebMvc - Sistema Laboratorial] ::: {mensagem}, em {DateTime.Now}";

            if (!CustomEventTypeMapping.TryGetValue(tipoEvento, out CustomLogEntryType tipo))
            {
                Console.WriteLine("Tipo de log não reconhecido.");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await LogNoEventViewerAsync(mensagemFinal, tipo, origem);
            }
            else
            {
                await LogEmArquivoAsync(mensagemFinal, tipo, origem);
            }
        }

        private async Task LogEmArquivoAsync(string mensagem, CustomLogEntryType tipo, string origem)
        {
            try
            {
                string linha = $"[{DateTime.Now}] Origem: {origem}, Tipo: {tipo}, Mensagem: {mensagem}{Environment.NewLine}";
                string? diretorio = Path.GetDirectoryName(LinuxLogPath);

                if (!string.IsNullOrWhiteSpace(diretorio) && !Directory.Exists(diretorio))
                {
                    Directory.CreateDirectory(diretorio);
                }

                await File.AppendAllTextAsync(LinuxLogPath, linha);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gravar log em arquivo: {ex.Message}");
            }
        }
    }//Fim da classe
}