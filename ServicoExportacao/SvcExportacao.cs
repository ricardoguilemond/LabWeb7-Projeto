using ExtensionsMethods.Genericos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Integracoes.Interfaces.Responses;
using LabWebMvc.MVC.Models;

namespace ServicoExportacao
{
    public class SvcExportacao : BackgroundService
    {
        #region Declarações

        private readonly ILogger<SvcExportacao> _logger;
        private bool servicoEmExecucao = false;
        private readonly Db _db;
        private readonly IConnectionService _connectionService;

        #endregion Declarações

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/></param>
        public SvcExportacao(ILogger<SvcExportacao> logger, Db db, IConnectionService connectionService)
        {
            _logger = logger;
            _db = db;
            _connectionService = connectionService;
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        /// Executes when the service has started.
        /// </summary>
        /// <param name="stoppingToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="Task"/></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (servicoEmExecucao)
                {
//                    _logger.LogInformation("O Serviço de Exportação já está em execução.");
                    LoggerFile.Write("O Serviço de Exportação já está em execução.");
                    return;
                }
                servicoEmExecucao = true;

                //_logger.LogInformation("** SERVIÇO INICIADO **");
                LoggerFile.Write("** SERVICO INICIADO **");

                try
                {
                    IntegracaoService srv = new IntegracaoService(_db, _connectionService);
                    RodarIntegracaoAgendadaResponse response = srv.RodarIntegracaoAgendada();
                    if (response.Log != null)
                    {
                        foreach (string item in response.Log)
                        {
                            LoggerFile.Write(item.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                    RodarIntegracaoAgendadaResponse response = new();
                    response.Errors?.Add(ex.Message.ToString());
                    //_logger.LogInformation("** SERVIÇO COM FALHA(S) GRAVE(S) **");
                    LoggerFile.Write("** SERVIÇO COM FALHA(S) GRAVE(S) **");
                    if (response.Errors != null)
                    {
                        foreach (object item in response.Errors)
                        {
                            //_logger.LogInformation(item.ToString());
                            LoggerFile.Write((string)item);
                        }
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
            finally
            {
                //_logger.LogInformation("** SERVIÇO PARADO **");
                LoggerFile.Write("** SERVIÇO PARADO **");
                servicoEmExecucao = false;
            }
        }

        /// <summary>
        /// Executes when the service is ready to start.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="Task"/></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Iniciando o serviço...");
            LoggerFile.Write("Iniciando o serviço...");

            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Executes when the service is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="Task"/></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Parando o serviço...");
            LoggerFile.Write("Parando o serviço...");

            return base.StopAsync(cancellationToken);
        }

        #endregion Methods
    }
}