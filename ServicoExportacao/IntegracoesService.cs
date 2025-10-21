using ExtensionsMethods.Genericos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Integracoes;
using LabWebMvc.MVC.Integracoes.Interfaces.Responses;
using LabWebMvc.MVC.Models;

namespace ServicoExportacao
{
    public class IntegracaoService : IDisposable
    {
        private readonly Db _db;
        public IntegracaoService(Db db)
        {
            _db = db;
        }
        public void Dispose()
        {
            _db?.Dispose();
        }

        public RodarIntegracaoAgendadaResponse RodarIntegracaoAgendada()
        {
            var response = new RodarIntegracaoAgendadaResponse();

            try
            {
                var integracoes = new Integracoes(_db);
                response = integracoes.RodarIntegracaoAgendada();
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                    ex = ex.InnerException;

                LoggerFile.Write(ex.Message);
                response.Errors?.Add(ex.Message);
            }

            return response;
        }
    }
}