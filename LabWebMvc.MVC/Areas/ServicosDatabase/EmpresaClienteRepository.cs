using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.Controllers;
using LabWebMvc.MVC.Areas.Utils;
using Npgsql;

namespace LabWebMvc.MVC.Areas.ServicosDatabase
{
    public class EmpresaClienteRepository
    {
        private string _connectionString;
        private readonly GeralController _geralController;
        private readonly IEventLogHelper _eventLog;

        public EmpresaClienteRepository(string connectionString = "", GeralController geralController = null!, IEventLogHelper eventLogHelper = null!)
        {
            _connectionString = connectionString;
            _geralController = geralController;
            _eventLog = eventLogHelper;
        }

        public (string SQL, NpgsqlParameter[] Parametros) RetornaSelectEmails(string emailCliente = "")
        {
            string tipoBanco = DatabaseHelper.IdentificaTipoDeBancoDeDados(_connectionString);
            string SQL;
            var parametros = new[] { new NpgsqlParameter("@Email", emailCliente) };

            if (tipoBanco == "MSSQL")
                SQL = "SELECT TOP 1 * FROM Emails WHERE Email = @Email";
            else if (tipoBanco == "Oracle")
                SQL = "SELECT * FROM Emails WHERE Email = @Email AND ROWNUM = 1";
            else if (tipoBanco == "MySQL")
                SQL = "SELECT * FROM Emails WHERE Email = @Email LIMIT 1";
            else if (tipoBanco == "PostgreSQL")
                SQL = @"SELECT * FROM ""Emails"" WHERE ""Email"" = @Email LIMIT 1";
            else
                SQL = "SELECT TOP 1 * FROM Emails WHERE Email = @Email";

            return (SQL, parametros);
        }

        public (string SQL, NpgsqlParameter[] Parametros) RetornaSelectEmpresaCliente(string filtro = "", string campo = "Email")
        {
            string tipoBanco = DatabaseHelper.IdentificaTipoDeBancoDeDados(_connectionString);
            string SQL;
            // campo nunca vem de input do usuário (sempre "Email", "CNPJ" ou "Id" — hardcoded nos callers)
            // Quando campo == "Id", o parâmetro deve ser int para evitar "operator does not exist: integer = text"
            NpgsqlParameter[] parametros;
            if (string.IsNullOrEmpty(filtro))
            {
                parametros = Array.Empty<NpgsqlParameter>();
            }
            else if (campo == "Id" && int.TryParse(filtro, out int filtroInt))
            {
                parametros = new[] { new NpgsqlParameter<int>("@Filtro", filtroInt) };
            }
            else
            {
                parametros = new[] { new NpgsqlParameter("@Filtro", filtro) };
            }

            if (tipoBanco == "MSSQL")
                SQL = string.IsNullOrEmpty(filtro)
                    ? "SELECT TOP 1 * FROM EmpresaCliente"
                    : $"SELECT TOP 1 * FROM EmpresaCliente WHERE {campo} = @Filtro";

            else if (tipoBanco == "Oracle")
                SQL = string.IsNullOrEmpty(filtro)
                    ? "SELECT * FROM EmpresaCliente WHERE ROWNUM = 1"
                    : $"SELECT * FROM EmpresaCliente WHERE {campo} = @Filtro AND ROWNUM = 1";

            else if (tipoBanco == "MySQL")
                SQL = string.IsNullOrEmpty(filtro)
                    ? "SELECT * FROM EmpresaCliente LIMIT 1"
                    : $"SELECT * FROM EmpresaCliente WHERE {campo} = @Filtro LIMIT 1";

            else if (tipoBanco == "PostgreSQL")
                SQL = string.IsNullOrEmpty(filtro)
                    ? @"SELECT * FROM ""EmpresaCliente"" LIMIT 1"
                    : $@"SELECT * FROM ""EmpresaCliente"" WHERE ""{campo}"" = @Filtro LIMIT 1";

            else // padrão MSSQL
                SQL = string.IsNullOrEmpty(filtro)
                    ? "SELECT TOP 1 * FROM EmpresaCliente"
                    : $"SELECT TOP 1 * FROM EmpresaCliente WHERE {campo} = @Filtro";

            return (SQL, parametros);
        }

        public EmpresaCliente ObterEmpresaCliente(string emailCliente = "")
        {
            /*
             * Lógica: Quando passar pela primeira vez (está no Db.cs), ANTES do LOGIN, o emailCliente estará vazio, e desta forma,
             *         fará a leitura da EmpresaCliente (padrão de testes/anotações dos clientes existentes!)
             *         Quando passar pela segunda vez (está no ValidacoesDeSenhas.cs), o cliente terá FEITO o LOGIN, e então o emailCliente virá preenchido, e desta forma,
             *         fará a leitura da EmpresaCliente com a "conexão string" e dados da empresa verdadeira do cliente!
             */
            var (SQL, parametros) = RetornaSelectEmpresaCliente(emailCliente, "Email");

            using (NpgsqlConnection  conexao = new(_connectionString))
            {
                conexao.Open();

                using (NpgsqlCommand comando = new(SQL, conexao))
                {
                    foreach (var p in parametros)
                        comando.Parameters.Add(p);

                    using (NpgsqlDataReader reader = comando.ExecuteReader())
                    {
                        if (reader.Read()) // Lê o primeiro registro
                        {
                            return new EmpresaCliente
                            {
                                Id = reader["Id"].ToString() ?? "0",
                                CNPJ = reader["CNPJ"].ToString() ?? "",
                                Email = reader["Email"].ToString() ?? "",
                                StringConexao = reader["StringConexao"].ToString() ?? "",
                                LimiteUsuarios = reader["LimiteUsuarios"].ToString() ?? "0",
                                //Feito pelo Kiro em 03/05/2026 — migrado para UTC (timestamptz)
                                DataExpira = string.IsNullOrEmpty(reader["DataExpira"].ToString()) ? _geralController.ObterDataHoraUtc().AddDays(30) : Convert.ToDateTime(reader["DataExpira"])
                                //..Kiro
                            };
                        }
                    }
                }
            }
            _eventLog.LogEventViewer("[ValidacoesDeSenhas] Nenhuma empresa/cliente encontrada com o email da tentiva de login: " + emailCliente + ", é possível que este cliente ainda não esteja cadastrado.", "wError");
            throw new Exception("O Banco de Dados pode estar inoperante neste momento.");
        }
    }

    public class EmpresaCliente
    {
        public string Id { get; set; } = null!;
        public string CNPJ { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string StringConexao { get; set; } = null!;
        public string LimiteUsuarios { get; set; } = null!;
        public DateTime? DataExpira { get; set; }
    }
}