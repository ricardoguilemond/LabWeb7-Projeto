using ExtensionsMethods.EventViewerHelper;
using FirebirdSql.Data.FirebirdClient;
using Npgsql;
using System.Data.Common;

namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public class ColunaInfo
    {
        public string Nome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? Tamanho { get; set; }
        public int? Precisao { get; set; }
        public int? Escala { get; set; }
        public bool Nullable { get; set; }
    }

    public class MapeamentoColunas
    {
        public string NomeFirebird { get; set; } = string.Empty;
        public string NomePostgreSQL { get; set; } = string.Empty;
        public ColunaInfo? ColunaFirebird { get; set; }
        public ColunaInfo? ColunaPostgreSQL { get; set; }
        public bool Compativel { get; set; }
        public string? Incompatibilidade { get; set; }
        public string? Aviso { get; set; }
    }

    public class SchemaComparisonResult
    {
        public string TabelaFirebird { get; set; } = string.Empty;
        public string TabelaPostgreSQL { get; set; } = string.Empty;
        public List<MapeamentoColunas> Colunas { get; set; } = new();
        public List<string> ColunasApenasFirebird { get; set; } = new();
        public List<string> ColunasApenasPostgreSQL { get; set; } = new();
        public bool PodeImportar => Colunas.Any(c => c.Compativel);
        public List<string> ErrosBloqueantes => Colunas.Where(c => !string.IsNullOrEmpty(c.Incompatibilidade)).Select(c => $"{c.NomeFirebird}: {c.Incompatibilidade}").ToList();
        public List<string> Avisos => Colunas.Where(c => !string.IsNullOrEmpty(c.Aviso)).Select(c => $"{c.NomeFirebird}: {c.Aviso}").ToList();
    }

    public interface ISchemaComparer
    {
        Task<SchemaComparisonResult> CompararSchemasAsync(string firebirdConnectionString, string postgresConnectionString, string tabelaFirebird, string tabelaPostgreSQL, CancellationToken cancellationToken = default);
        Task<long> ContarRegistrosFirebirdAsync(string firebirdConnectionString, string tabelaFirebird, CancellationToken cancellationToken = default);
        Task<long> ContarRegistrosExistentesAsync(string postgresConnectionString, string tabelaPostgreSQL, string primaryKeyColumn, CancellationToken cancellationToken = default);
    }

    public class SchemaComparer : ISchemaComparer
    {
        private readonly IEventLogHelper _eventLog;

        public SchemaComparer(IEventLogHelper eventLog)
        {
            _eventLog = eventLog;
        }

        public async Task<SchemaComparisonResult> CompararSchemasAsync(string firebirdConnectionString, string postgresConnectionString, string tabelaFirebird, string tabelaPostgreSQL, CancellationToken cancellationToken = default)
        {
            var resultado = new SchemaComparisonResult
            {
                TabelaFirebird = tabelaFirebird,
                TabelaPostgreSQL = tabelaPostgreSQL
            };

            LogEmArquivo($"[SchemaComparer] Iniciando comparacao {tabelaFirebird}->{tabelaPostgreSQL}");
            LogEmArquivo($"[SchemaComparer] Firebird connection string (sem senha): {MascararConnectionString(firebirdConnectionString)}");
            LogEmArquivo($"[SchemaComparer] PostgreSQL connection string (sem senha): {MascararConnectionString(postgresConnectionString)}");

            await LogarTabelasFirebirdAsync(firebirdConnectionString, tabelaFirebird, cancellationToken);

            var colunasFirebird = await ObterColunasFirebirdAsync(firebirdConnectionString, tabelaFirebird, cancellationToken);
            var colunasPostgres = await ObterColunasPostgreSQLAsync(postgresConnectionString, tabelaPostgreSQL, cancellationToken);

            var mensagem = $"[SchemaComparer] Tabela {tabelaFirebird}->{tabelaPostgreSQL}: {colunasFirebird.Count} colunas Firebird, {colunasPostgres.Count} colunas PostgreSQL.";
            _eventLog.LogEventViewer(mensagem, "wInfo");
            LogEmArquivo(mensagem);
            if (colunasFirebird.Any())
            {
                _eventLog.LogEventViewer($"[SchemaComparer] Colunas Firebird: {string.Join(", ", colunasFirebird.Select(c => c.Nome))}", "wInfo");
                LogEmArquivo($"[SchemaComparer] Colunas Firebird: {string.Join(", ", colunasFirebird.Select(c => c.Nome))}");
            }
            if (colunasPostgres.Any())
            {
                _eventLog.LogEventViewer($"[SchemaComparer] Colunas PostgreSQL: {string.Join(", ", colunasPostgres.Select(c => c.Nome))}", "wInfo");
                LogEmArquivo($"[SchemaComparer] Colunas PostgreSQL: {string.Join(", ", colunasPostgres.Select(c => c.Nome))}");
            }

            var nomesFirebird = colunasFirebird.Select(c => c.Nome.ToUpperInvariant()).ToHashSet();
            var nomesPostgres = colunasPostgres.Select(c => c.Nome.ToUpperInvariant()).ToHashSet();

            resultado.ColunasApenasFirebird = colunasFirebird.Where(c => !nomesPostgres.Contains(c.Nome.ToUpperInvariant())).Select(c => c.Nome).ToList();
            resultado.ColunasApenasPostgreSQL = colunasPostgres.Where(c => !nomesFirebird.Contains(c.Nome.ToUpperInvariant())).Select(c => c.Nome).ToList();

            foreach (var colunaFb in colunasFirebird)
            {
                var colunaPg = colunasPostgres.FirstOrDefault(c => c.Nome.Equals(colunaFb.Nome, StringComparison.OrdinalIgnoreCase));

                var mapeamento = new MapeamentoColunas
                {
                    NomeFirebird = colunaFb.Nome,
                    NomePostgreSQL = colunaPg?.Nome ?? colunaFb.Nome,
                    ColunaFirebird = colunaFb,
                    ColunaPostgreSQL = colunaPg,
                    Compativel = colunaPg != null
                };

                if (colunaPg == null)
                {
                    mapeamento.Incompatibilidade = $"Coluna '{colunaFb.Nome}' existe no Firebird mas não no PostgreSQL";
                }
                else if (!TiposCompativeis(colunaFb.Tipo, colunaPg.Tipo))
                {
                    mapeamento.Aviso = $"Tipos diferentes: Firebird={colunaFb.Tipo}, PostgreSQL={colunaPg.Tipo}. Conversão automática será tentada.";
                }

                resultado.Colunas.Add(mapeamento);
            }

            return resultado;
        }

        public async Task<long> ContarRegistrosFirebirdAsync(string firebirdConnectionString, string tabelaFirebird, CancellationToken cancellationToken = default)
        {
            using var conn = new FbConnection(firebirdConnectionString);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new FbCommand($"SELECT COUNT(*) FROM \"{tabelaFirebird}\"", conn);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }

        public async Task<long> ContarRegistrosExistentesAsync(string postgresConnectionString, string tabelaPostgreSQL, string primaryKeyColumn, CancellationToken cancellationToken = default)
        {
            using var conn = new NpgsqlConnection(postgresConnectionString);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{tabelaPostgreSQL}\"", conn);
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }

        private async Task<List<ColunaInfo>> ObterColunasFirebirdAsync(string connectionString, string tabela, CancellationToken cancellationToken)
        {
            var colunas = new List<ColunaInfo>();

            using var conn = new FbConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            // Busca case-insensitive. No Firebird, nomes sem aspas ficam em maiusculo;
            // nomes com aspas (delimited identifiers) preservam o case.
            const string sql = @"
                SELECT 
                    TRIM(r.rdb$field_name) AS nome,
                    CASE f.rdb$field_type
                        WHEN 7 THEN 'SMALLINT'
                        WHEN 8 THEN 'INTEGER'
                        WHEN 10 THEN 'FLOAT'
                        WHEN 12 THEN 'DATE'
                        WHEN 13 THEN 'TIME'
                        WHEN 14 THEN 'CHAR'
                        WHEN 16 THEN 'BIGINT'
                        WHEN 27 THEN 'DOUBLE'
                        WHEN 35 THEN 'TIMESTAMP'
                        WHEN 37 THEN 'VARCHAR'
                        WHEN 40 THEN 'CSTRING'
                        WHEN 45 THEN 'BLOB_ID'
                        WHEN 261 THEN 'BLOB'
                        ELSE 'UNKNOWN'
                    END AS tipo,
                    f.rdb$field_length AS tamanho,
                    f.rdb$field_precision AS precisao,
                    f.rdb$field_scale AS escala,
                    CASE WHEN r.rdb$null_flag = 1 THEN 0 ELSE 1 END AS nullable
                FROM rdb$relation_fields r
                JOIN rdb$fields f ON f.rdb$field_name = r.rdb$field_source
                WHERE UPPER(TRIM(r.rdb$relation_name)) = UPPER(TRIM(@tabela))
                ORDER BY r.rdb$field_position";

            using var cmd = new FbCommand(sql, conn);
            cmd.Parameters.AddWithValue("tabela", tabela);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                colunas.Add(new ColunaInfo
                {
                    Nome = reader.GetString(0),
                    Tipo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Tamanho = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Precisao = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Escala = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Nullable = reader.GetInt32(5) == 1
                });
            }

            LogEmArquivo($"[SchemaComparer] Firebird tabela {tabela}: {colunas.Count} colunas.");

            return colunas;
        }

        private async Task LogarTabelasFirebirdAsync(string connectionString, string tabela, CancellationToken cancellationToken)
        {
            try
            {
                using var conn = new FbConnection(connectionString);
                await conn.OpenAsync(cancellationToken);

                using var cmdDb = new FbCommand("SELECT rdb$get_context('SYSTEM', 'DB_NAME') FROM rdb$database", conn);
                var dbName = await cmdDb.ExecuteScalarAsync(cancellationToken);
                LogEmArquivo($"[SchemaComparer] Firebird banco conectado: {dbName}");

                const string sql = @"
                    SELECT TRIM(rdb$relation_name) AS nome
                    FROM rdb$relations
                    WHERE rdb$view_blr IS NULL
                      AND UPPER(TRIM(rdb$relation_name)) = UPPER(TRIM(@tabela))
                    ORDER BY rdb$relation_name";

                using var cmd = new FbCommand(sql, conn);
                cmd.Parameters.AddWithValue("tabela", tabela);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var encontradas = new List<string>();
                while (await reader.ReadAsync(cancellationToken))
                    encontradas.Add(reader.GetString(0));

                LogEmArquivo($"[SchemaComparer] Firebird tabelas que casam com '{tabela}': {(encontradas.Any() ? string.Join(", ", encontradas) : "(nenhuma)")}");
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[SchemaComparer] Erro ao diagnosticar tabelas Firebird: {ex.Message}");
            }
        }

        private async Task<List<ColunaInfo>> ObterColunasPostgreSQLAsync(string connectionString, string tabela, CancellationToken cancellationToken)
        {
            var colunas = new List<ColunaInfo>();

            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            // Busca em todos os schemas do banco (exceto catálogos do sistema).
            // Prioriza o schema atual da conexao, mas nao depende exclusivamente dele.
            const string sql = @"
                SELECT 
                    a.attname AS column_name,
                    pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type,
                    NULL::integer AS character_maximum_length,
                    NULL::integer AS numeric_precision,
                    NULL::integer AS numeric_scale,
                    CASE WHEN a.attnotnull THEN 'NO' ELSE 'YES' END AS is_nullable,
                    n.nspname AS schema_name
                FROM pg_catalog.pg_attribute a
                JOIN pg_catalog.pg_class c ON c.oid = a.attrelid
                JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relname ILIKE @tabela
                  AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                  AND a.attnum > 0
                  AND NOT a.attisdropped
                ORDER BY 
                    CASE WHEN n.nspname = current_schema() THEN 0 ELSE 1 END,
                    n.nspname,
                    a.attnum";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("tabela", tabela);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            string? schemaEncontrado = null;
            while (await reader.ReadAsync(cancellationToken))
            {
                var tipo = reader.GetString(1);
                // Simplifica tipos como character varying(50) -> character varying
                var tipoBase = tipo.Split('(')[0].Trim();
                schemaEncontrado ??= reader.GetString(6);

                colunas.Add(new ColunaInfo
                {
                    Nome = reader.GetString(0),
                    Tipo = tipoBase,
                    Tamanho = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Precisao = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Escala = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Nullable = reader.GetString(5).Equals("YES", StringComparison.OrdinalIgnoreCase)
                });
            }

            LogEmArquivo($"[SchemaComparer] PostgreSQL tabela {tabela}: schema encontrado = {schemaEncontrado ?? "(nenhum)"}, {colunas.Count} colunas.");

            return colunas;
        }

        private static void LogEmArquivo(string mensagem)
        {
            try
            {
                var caminho = Path.Combine(AppContext.BaseDirectory, "LogsCargaDados", $"diagnostico_{DateTime.UtcNow:yyyyMMdd}.txt");
                var diretorio = Path.GetDirectoryName(caminho);
                if (!string.IsNullOrWhiteSpace(diretorio) && !Directory.Exists(diretorio))
                    Directory.CreateDirectory(diretorio);

                var linha = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {mensagem}{Environment.NewLine}";
                File.AppendAllText(caminho, linha);
            }
            catch
            {
                // Falha silenciosa no log auxiliar para nao quebrar a importacao
            }
        }

        private static string MascararConnectionString(string connectionString)
        {
            try
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
                if (builder.ContainsKey("Password"))
                    builder["Password"] = "***";
                if (builder.ContainsKey("pwd"))
                    builder["pwd"] = "***";
                return builder.ToString();
            }
            catch
            {
                return "(invalid connection string)";
            }
        }

        private static bool TiposCompativeis(string tipoFirebird, string tipoPostgreSQL)
        {
            var fb = tipoFirebird.ToUpperInvariant();
            var pg = tipoPostgreSQL.ToUpperInvariant();

            if (fb.Contains("VARCHAR") || fb.Contains("CHAR"))
                return pg.Contains("VARCHAR") || pg.Contains("CHAR") || pg.Contains("TEXT");

            if (fb.Contains("INTEGER") || fb.Contains("SMALLINT") || fb.Contains("BIGINT"))
                return pg.Contains("INT") || pg.Contains("SMALLINT") || pg.Contains("BIGINT") || pg.Contains("SERIAL");

            if (fb.Contains("NUMERIC") || fb.Contains("DECIMAL") || fb.Contains("DOUBLE") || fb.Contains("FLOAT"))
                return pg.Contains("NUMERIC") || pg.Contains("DECIMAL") || pg.Contains("DOUBLE") || pg.Contains("REAL") || pg.Contains("FLOAT");

            if (fb.Contains("TIMESTAMP"))
                return pg.Contains("TIMESTAMP") || pg.Contains("DATE") || pg.Contains("TIME");

            if (fb.Contains("DATE") && !fb.Contains("TIMESTAMP"))
                return pg.Contains("DATE") || pg.Contains("TIMESTAMP");

            if (fb.Contains("TIME") && !fb.Contains("TIMESTAMP"))
                return pg.Contains("TIME") || pg.Contains("TIMESTAMP");

            if (fb.Contains("BLOB"))
                return pg.Contains("BYTEA") || pg.Contains("TEXT");

            if (fb.Contains("BOOLEAN"))
                return pg.Contains("BOOL");

            return true;
        }
    }
}
