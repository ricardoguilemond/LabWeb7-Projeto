using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Integracoes.Interfaces.Responses;
using ServicoExportacao;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Threading;

namespace WindowsService
{
    [SupportedOSPlatform("windows")]
    internal class FileWriteService : ServiceBase
    {
        private const string serviceName = "Lab7ServiceIntegracao";
        private readonly IDbFactory _dbFactory;
        private Thread Worker = null!;
        private int _intSleepTime = 1; // pausa em minutos

        public FileWriteService(IDbFactory dbFactory)
        {
            _dbFactory = dbFactory;

            ServiceName = serviceName;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = false;

            EventLog.Source = ServiceName;
            EventLog.Log = "Application";
            EventLog.WriteEntry("Iniciando o serviço 'Lab7ServiceIntegracao'...", EventLogEntryType.Information);
        }

        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry($"OnStart() acionado para '{ServiceName}' com pausa de {_intSleepTime} minuto(s)", EventLogEntryType.Information);

            ThreadStart start = new(Working);
            Worker = new Thread(start);
            Worker.Start();
        }

        protected override void OnPause()
        {
            if (Worker != null && Worker.IsAlive)
            {
                EventLog.WriteEntry($"OnPause() o serviço {ServiceName} está em pausa...", EventLogEntryType.Information);
                base.OnPause();
            }
        }

        protected override void OnContinue()
        {
            if (Worker != null && Worker.IsAlive)
            {
                EventLog.WriteEntry($"OnContinue() o serviço {ServiceName} foi continuado...", EventLogEntryType.Information);
                base.OnContinue();
            }
        }

        protected override void OnStop()
        {
            if (Worker != null && Worker.IsAlive)
            {
                EventLog.WriteEntry($"Windows Service '{ServiceName}' parado em {DateTime.UtcNow}", EventLogEntryType.Information);
            }
        }

        public void Working()
        {
            int nsleep = _intSleepTime > 0 ? _intSleepTime * 60000 : 60000;

            while (true)
            {
                EventLog.WriteEntry($"Ciclo do Windows Service '{ServiceName}' iniciado em {DateTime.UtcNow}", EventLogEntryType.Information);

                Thread.Sleep(nsleep);
                EventLog.WriteEntry($"Serviço fez uma pausa esperada de {_intSleepTime} minuto(s) em {DateTime.UtcNow}", EventLogEntryType.Information);

                try
                {
                    EventLog.WriteEntry($"Serviço '{ServiceName}' vai iniciar processo de integração agendado em {DateTime.UtcNow}", EventLogEntryType.Information);

                    var db = _dbFactory.Create();
                    using var srv = new IntegracaoService(db);
                    var response = srv.RodarIntegracaoAgendada();

                    if (response.Log != null)
                    {
                        foreach (string item in response.Log)
                        {
                            EventLog.WriteEntry($"{item} ::: {DateTime.UtcNow}", EventLogEntryType.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                    var response = new RodarIntegracaoAgendadaResponse();
                    response.Errors?.Add(ex.Message);

                    EventLog.WriteEntry($"*** SERVIÇO '{ServiceName}' COM FALHAS GRAVES ::: {DateTime.UtcNow}", EventLogEntryType.Error);

                    if (response.Errors != null)
                    {
                        foreach (var item in response.Errors)
                        {
                            EventLog.WriteEntry($"{item?.ToString() ?? "Erro desconhecido"} ::: {DateTime.UtcNow}", EventLogEntryType.Error);
                        }
                    }
                }
                finally
                {
                    EventLog.WriteEntry($"Ciclo do serviço '{ServiceName}' terminou em {DateTime.UtcNow}", EventLogEntryType.Information);
                }
            }
        }

        public void OnDebug(string[] args)
        {
            OnStart(args);
        }
    }
}