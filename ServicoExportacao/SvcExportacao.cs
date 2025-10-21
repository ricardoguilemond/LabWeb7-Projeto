using ExtensionsMethods.Genericos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Integracoes.Interfaces.Responses;
using LabWebMvc.MVC.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServicoExportacao
{
    public class SvcExportacao : BackgroundService
    {
        private readonly ILogger<SvcExportacao> _logger;
        private readonly IDbFactory _dbFactory;
        private bool servicoEmExecucao = false;

        public SvcExportacao(ILogger<SvcExportacao> logger, IDbFactory dbFactory)
        {
            _logger = logger;
            _dbFactory = dbFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (servicoEmExecucao)
                {
                    LoggerFile.Write("O Servi�o de Exporta��o j� est� em execu��o.");
                    return;
                }
                servicoEmExecucao = true;

                LoggerFile.Write("** SERVICO INICIADO **");

                try
                {
                    var db = _dbFactory.Create(); //usa a f�brica corretamente

                    var srv = new IntegracaoService(db); //ajustado para aceitar apenas Db
                    var response = srv.RodarIntegracaoAgendada();

                    if (response.Log != null)
                    {
                        foreach (string item in response.Log)
                        {
                            LoggerFile.Write(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;

                    var response = new RodarIntegracaoAgendadaResponse();
                    response.Errors?.Add(ex.Message);
                    LoggerFile.Write("** SERVI�O COM FALHA(S) GRAVE(S) **");

                    if (response.Errors != null)
                    {
                        foreach (var item in response.Errors)
                        {
                            LoggerFile.Write(item?.ToString() ?? "Erro desconhecido"); //evita erro de convers�o
                        }
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
            finally
            {
                LoggerFile.Write("** SERVI�O PARADO **");
                servicoEmExecucao = false;
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            LoggerFile.Write("Iniciando o servi�o...");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            LoggerFile.Write("Parando o servi�o...");
            return base.StopAsync(cancellationToken);
        }
    }
}