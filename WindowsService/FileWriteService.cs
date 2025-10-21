using LabWebMvc.MVC.Integracoes.Interfaces.Responses;
using ServicoExportacao;
using System.Diagnostics;
using System.Runtime.Versioning; // necessário para o atributo:  [SupportedOSPlatform("windows")]
using System.ServiceProcess;

namespace WindowsService
{
    [SupportedOSPlatform("windows")]
    internal class FileWriteService : ServiceBase
    {
        private const string serviceName = "Lab7ServiceIntegracao";
        public Thread Worker = null!;
        public int _intSleepTime = 1;  //argumento passado é para pausa no serviço em minutos

        public FileWriteService()
        {
            ServiceName = serviceName;

            CanStop = true;
            CanPauseAndContinue = false;  //para este serviço não está acontecendo a pausa, então vamos omitir a opção de pausa

            //Setup logging
            AutoLog = false;
            EventLog.WriteEntry("Iniciando o serviço 'Lab7ServiceIntegracao'...", EventLogEntryType.Information);
            EventLog.Source = ServiceName;
            EventLog.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            EventLog.WriteEntry(string.Format("OnStart() acionado para '{0}' com pausa de {1} minuto(s) a cada evento", ServiceName, _intSleepTime), EventLogEntryType.Information);

            ThreadStart start = new(Working);
            Worker = new Thread(start);
            Worker.Start();
        }

        protected override void OnPause()
        {
            try
            {
                if ((Worker != null) && Worker.IsAlive)
                {
                    EventLog.WriteEntry(string.Format("OnPause() o serviço {0} está em pausa...", ServiceName), EventLogEntryType.Information);
                    base.OnPause();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override void OnContinue()
        {
            try
            {
                if ((Worker != null) && Worker.IsAlive)
                {
                    EventLog.WriteEntry(string.Format("OnContinue() o serviço {0} foi continuado...", ServiceName), EventLogEntryType.Information);
                    base.OnContinue();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                if ((Worker != null) && Worker.IsAlive)
                {
                    EventLog.WriteEntry(string.Format("Windows Service '{0}' parado em {1} ", "Lab7ServiceIntegracao", DateTime.Now), EventLogEntryType.Information);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Working()
        {
            int nsleep = 60000; //declarando intervalo de 1 minuto = 60 mil milissegundos!
            if (_intSleepTime > 0) nsleep = _intSleepTime * 60000;

            try
            {
                while (true)
                {
                    EventLog.WriteEntry(string.Format("Ciclo do Windows Service '{0}' iniciado em {1} ", "Lab7ServiceIntegracao", DateTime.Now), EventLogEntryType.Information);

                    Thread.Sleep(nsleep);  //Faz uma pausa de 1 minuto até recomeçar o serviço para não sobrecarregar a demanda...
                    EventLog.WriteEntry(string.Format("{0} '{1}' minuto(s) em {2}", "Serviço fez uma pausa esperada de ", _intSleepTime, DateTime.Now), EventLogEntryType.Information);

                    //Chama o outro serviço (Serviço de Integração da Aplicação) sem passar o DbContext
                    try
                    {
                        EventLog.WriteEntry(string.Format("Serviço '{0}' vai iniciar processo de integração agendado em {1}", ServiceName, DateTime.Now), EventLogEntryType.Information);

                        IntegracaoService srv = new IntegracaoService(null!, null!);
                        RodarIntegracaoAgendadaResponse response = srv.RodarIntegracaoAgendada();
                        if (response.Log != null)
                        {
                            foreach (string item in response.Log)
                            {
                                EventLog.WriteEntry(string.Format("{0} ::: {1}", item.ToString(), DateTime.Now), EventLogEntryType.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        while (ex.InnerException != null)
                            ex = ex.InnerException;

                        RodarIntegracaoAgendadaResponse response = new();
                        response.Errors?.Add(ex.Message.ToString());

                        EventLog.WriteEntry(string.Format("*** SERVIÇO '{0}' COM FALHAS GRAVES ::: {1}", "Lab7ServiceIntegracao", DateTime.Now), EventLogEntryType.Error);

                        if (response.Errors != null)
                        {
                            foreach (object item in response.Errors)
                            {
                                //LoggerFile.Write(string.Format("{0} ::: {1}", (string)item, DateTime.Now));
                                EventLog.WriteEntry(string.Format("{0} ::: {1}", (string)item, DateTime.Now), EventLogEntryType.Error);
                            }
                        }
                    }
                    finally
                    {
                        EventLog.WriteEntry(string.Format("Ciclo do serviço '{0}' terminou em {1} ", "Lab7ServiceIntegracao", DateTime.Now), EventLogEntryType.Information);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void OnDebug(string[] args)
        {
            string[] valor = new string[] { "2" };
            OnStart(valor);
        }
    }
}