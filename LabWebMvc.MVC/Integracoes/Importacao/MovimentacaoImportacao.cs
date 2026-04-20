using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
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
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"SELECT column_name FROM information_schema.columns WHERE table_name = @Tabela", conn);
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

            string[] colunasCsv = headerLine.Split(';');

            // Descobre colunas reais da tabela no PostgreSQL
            var colunasBanco = ObterColunasBanco(tabela);

            // Filtra apenas colunas que existem no banco
            var colunasValidas = colunasCsv
                .Select(c => c.Trim())
                .Where(c => colunasBanco.Contains(c, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (colunasValidas.Count == 0)
                throw new Exception("Nenhuma coluna do CSV corresponde às colunas da tabela no banco.");

            // Monta os índices das colunas válidas no CSV
            var indicesColunas = colunasValidas
                .Select(c => Array.FindIndex(colunasCsv, col => col.Trim().Equals(c, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Lê todas as linhas de dados
            var linhas = new List<string[]>();
            while (!reader.EndOfStream)
            {
                var linha = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(linha)) continue;
                linhas.Add(linha.Split(';'));
            }

            if (linhas.Count == 0) return;

            // Insere via COPY (bulk insert nativo do PostgreSQL)
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var colunasFormatadas = string.Join(", ", colunasValidas.Select(c => $"\"{c}\""));

            using var writer = conn.BeginTextImport($"COPY \"{tabela}\" ({colunasFormatadas}) FROM STDIN WITH (FORMAT csv, DELIMITER ';', NULL '')");

            foreach (var valores in linhas)
            {
                var valoresFiltrados = indicesColunas
                    .Select(i => i < valores.Length ? valores[i].Trim() : "")
                    .ToArray();

                writer.WriteLine(string.Join(";", valoresFiltrados));
            }
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
