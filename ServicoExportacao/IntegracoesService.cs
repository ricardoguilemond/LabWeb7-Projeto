using ExtensionsMethods.Genericos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Integracoes;
using LabWebMvc.MVC.Integracoes.Interfaces.Responses;
using LabWebMvc.MVC.Models;

namespace ServicoExportacao
{
    public class IntegracaoService : IDisposable
    {
        private Db db;
        private readonly IConnectionService _connectionService;

        public IntegracaoService(Db db, IConnectionService connectionService)
        {
            this.db = db;
            _connectionService = connectionService;
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public RodarIntegracaoAgendadaResponse RodarIntegracaoAgendada()
        {
            RodarIntegracaoAgendadaResponse response = new();

            try
            {
                if (db == null) //para atender quando vier pelo Windows Service, pois estrategicamente vem sem o DBContext.
                {
                    db = new Db(_connectionService);
                }

                Integracoes bt = new(db);
                response = bt.RodarIntegracaoAgendada();
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;

                LoggerFile.Write(ex.Message);

                response = new RodarIntegracaoAgendadaResponse();
                response.Errors?.Add(ex.Message.ToString());
            }

            return response;
        }
    }
}