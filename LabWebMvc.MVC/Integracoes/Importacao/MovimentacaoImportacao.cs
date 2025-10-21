using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;

namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public class MovimentacaoImportacao : IMovimentacaoImportacao
    {
        private readonly Db _db;
        private readonly string _connectionString;

        public MovimentacaoImportacao(Db db, IConfiguration configuration)
        {
            _db = db;
            _connectionString = configuration.GetSection("ConexaoPostgreSQL")
                                             .GetSection("PSQLConnectionString").Value!;
        }

        private List<string> ObterColunasBanco(string tabela)
        {
            var colunas = new List<string>();
            using var conn = new NpgsqlConnection (_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @Tabela", conn);
            cmd.Parameters.AddWithValue("@Tabela", tabela);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                colunas.Add(reader.GetString(0));

            return colunas;
        }

        private void ProcessaArquivo(string arquivo, string tabela)
        {
            using var reader = new StreamReader(arquivo);

            // Lê cabeçalho
            string? headerLine = reader.ReadLine();
            if (string.IsNullOrEmpty(headerLine))
                throw new Exception("Arquivo CSV sem cabeçalho.");

            string[] colunasCsv = headerLine.Split(';'); // ajuste o separador se necessário

            // Descobre colunas reais da tabela no SQL Server
            var colunasBanco = ObterColunasBanco(tabela);

            // Cria DataTable somente com colunas que existem no banco
            DataTable dt = new DataTable();
            foreach (var coluna in colunasCsv)
            {
                if (colunasBanco.Contains(coluna.Trim(), StringComparer.OrdinalIgnoreCase))
                    dt.Columns.Add(coluna.Trim());
            }

            // Lê dados
            while (!reader.EndOfStream)
            {
                var linha = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(linha)) continue;

                string[] valores = linha.Split(';');

                // Cria linha respeitando apenas colunas do banco
                DataRow row = dt.NewRow();
                for (int i = 0; i < colunasCsv.Length && i < valores.Length; i++)
                {
                    var coluna = colunasCsv[i].Trim();
                    if (dt.Columns.Contains(coluna))
                        row[coluna] = valores[i].Trim();
                }
                dt.Rows.Add(row);
            }

            // Bulk insert
            using var bulkCopy = new SqlBulkCopy(_connectionString)
            {
                DestinationTableName = tabela
            };

            foreach (DataColumn coluna in dt.Columns)
            {
                bulkCopy.ColumnMappings.Add(coluna.ColumnName, coluna.ColumnName);
            }
            bulkCopy.WriteToServer(dt);
        }

        public void ProcessaMovimentacao(MovimentacaoImportacaoParameter parameter)
        {
            if (parameter.NomeTabela == "Pacientes")
                parameter.NomeTabela = "Clientes";

            string pathOrigem = Path.Combine(Utils.GetLocalPathTemp(), "Importacao");
            string full = Path.Combine(pathOrigem, parameter.NomeTabela + ".csv");

            if (File.Exists(full))
            {
                ProcessaArquivo(full, parameter.NomeTabela!);
            }
            else
            {
                throw new FileNotFoundException("Arquivo não encontrado para importação", full);
            }
        }


    } //fim
}