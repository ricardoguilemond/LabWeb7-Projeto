using LabWebMvc.MVC.Areas.Utils;

namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    public interface IConnectionService
    {
        string GetConnectionString();

        void SetConnectionString(string connectionString);
    }

    public class ConnectionService : IConnectionService
    {
        private string? _overriddenConnectionString;
        private readonly Lazy<string> _defaultConnectionString;

        public ConnectionService()
        {
            _defaultConnectionString = new(() =>
            {
                var raw = Areas.Utils.Utils.GetValorSetupDoServico("ConexaoPostgreSQL", "PSQLConnectionString");

                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("A string de conexão padrão não foi encontrada no setup.");

                var final = raw
                    .ReformaTexto("usubanco", BasePadrao.UserId)
                    .ReformaTexto("ususenha", BasePadrao.Password);

                if (string.IsNullOrWhiteSpace(final) || !final.Contains("Host="))
                    throw new InvalidOperationException("A string de conexão gerada é inválida.");

                return final;
            });
        }

        public string GetConnectionString()
            => _overriddenConnectionString ?? _defaultConnectionString.Value;

        public void SetConnectionString(string connectionString)
            => _overriddenConnectionString = connectionString;
    }
}