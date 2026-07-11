using ExtensionsMethods.EventViewerHelper;
using FirebirdSql.Data.FirebirdClient;
using LabWebMvc.MVC.ViewModel.CargaDados;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Text;

namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public interface IFirebirdImporter
    {
        string MontarStringConexao(FirebirdConnectionViewModel viewModel);
        string MontarStringConexaoODBC(FirebirdConnectionViewModel viewModel);
        Task<(bool Sucesso, string MensagemErro)> TestarConexaoAsync(string firebirdConnectionString, CancellationToken cancellationToken = default);
        Task<(bool Sucesso, string MensagemErro)> TestarConexaoODBCAsync(string odbcConnectionString, CancellationToken cancellationToken = default);
        Task<EstimativaViewModel> GerarEstimativaAsync(string firebirdConnectionString, string postgresConnectionString, List<string> tabelasSelecionadas, int tamanhoLote, bool modoSimulacao, CancellationToken cancellationToken = default);
        Task<ImportacaoFinalViewModel> ImportarAsync(ImportacaoConfiguracao configuracao, string postgresConnectionString, CancellationToken cancellationToken = default);
    }

    public class FirebirdImporter : IFirebirdImporter
    {
        private readonly ISchemaComparer _schemaComparer;
        private readonly ITypeConverter _typeConverter;
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private readonly IEventLogHelper _eventLog;

        // Ordem de importação respeitando dependências de FK
        private static readonly List<(string Firebird, string Postgres, string Descricao)> TabelasSuportadas = new()
        {
            ("ClasseExames", "ClasseExames", "Classes de exames"),
            ("TabelaExames", "TabelaExames", "Tabelas de exames"),
            ("SituacaoExames", "SituacaoExames", "Situações de exames"),
            ("Logradouro", "Logradouro", "Logradouros"),
            ("Instituicao", "Instituicao", "Instituições"),
            ("Medicos", "Medicos", "Médicos"),
            ("Clientes", "Pacientes", "Pacientes (Clientes no Firebird)"),
            ("PlanoExames", "PlanoExames", "Planos de exames"),
            ("RequisicaoOriginal", "Requisitar", "Requisições"),
            ("TextosProntos", "TextosProntos", "Textos prontos"),
            ("FichasInternas", "FichasInternas", "Fichas internas"),
            ("FichasLotes", "FichasLotes", "Lotes de fichas"),
            ("FichasPlanilhas", "FichasPlanilhas", "Planilhas de fichas"),
            ("ExamesRealizados", "ExamesRealizados", "Exames realizados"),
            ("ItensExamesRealizados", "ItensExamesRealizados", "Itens de exames realizados"),
            ("ExamesRealizadosAM", "ExamesRealizadosAM", "Exames realizados (arquivados)"),
            ("ItensExamesRealizadosAM", "ItensExamesRealizadosAM", "Itens de exames realizados (arquivados)")
        };

        public FirebirdImporter(ISchemaComparer schemaComparer, ITypeConverter typeConverter, IHubContext<ImportProgressHub> hubContext, IEventLogHelper eventLog)
        {
            _schemaComparer = schemaComparer;
            _typeConverter = typeConverter;
            _hubContext = hubContext;
            _eventLog = eventLog;
        }

        public static List<(string Firebird, string Postgres, string Descricao)> ObterTabelasSuportadas()
        {
            return TabelasSuportadas;
        }

        public string MontarStringConexao(FirebirdConnectionViewModel viewModel)
        {
            var builder = new FbConnectionStringBuilder
            {
                DataSource = viewModel.Servidor,
                Port = viewModel.Porta,
                Database = viewModel.CaminhoBanco,
                UserID = viewModel.Usuario,
                Password = viewModel.Senha,
                Charset = viewModel.Charset,
                Dialect = 3,
                ServerType = 0 // TCP/IP padrão. Não usa XNET nem Embedded.
            };
            return builder.ToString();
        }

        public string MontarStringConexaoODBC(FirebirdConnectionViewModel viewModel)
        {
            var builder = new StringBuilder();
            builder.Append($"DSN={viewModel.NomeDSN};");
            builder.Append($"UID={viewModel.Usuario};");
            builder.Append($"PWD={viewModel.Senha};");
            return builder.ToString();
        }

        public async Task<(bool Sucesso, string MensagemErro)> TestarConexaoODBCAsync(string odbcConnectionString, CancellationToken cancellationToken = default)
        {
            try
            {
                using var conn = new OdbcConnection(odbcConnectionString);
                await conn.OpenAsync(cancellationToken);
                using var cmd = new OdbcCommand("SELECT 1 FROM RDB$DATABASE", conn);
                await cmd.ExecuteScalarAsync(cancellationToken);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _eventLog.LogEventViewer($"[CargaDados] Falha ao conectar no Firebird via ODBC: {ex.Message}", "wError");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Sucesso, string MensagemErro)> TestarConexaoAsync(string firebirdConnectionString, CancellationToken cancellationToken = default)
        {
            try
            {
                var infoDll = ObterInfoFbClient();
                _eventLog.LogEventViewer($"[CargaDados] Diagnóstico - FBCLIENT: {infoDll}", "wInfo");
                _eventLog.LogEventViewer($"[CargaDados] Diagnóstico - ConnectionString: {firebirdConnectionString}", "wInfo");

                using var conn = new FbConnection(firebirdConnectionString);
                await conn.OpenAsync(cancellationToken);
                using var cmd = new FbCommand("SELECT 1 FROM RDB$DATABASE", conn);
                await cmd.ExecuteScalarAsync(cancellationToken);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                var detalhes = ObterDetalhesExcecao(ex);
                _eventLog.LogEventViewer($"[CargaDados] Falha ao conectar no Firebird: {detalhes}", "wError");
                return (false, detalhes);
            }
        }

        private static string ObterDetalhesExcecao(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.AppendLine($"Inner: {inner.Message}");
                inner = inner.InnerException;
            }
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            return sb.ToString();
        }

        private static string ObterInfoFbClient()
        {
            try
            {
                var caminhos = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "fbclient.dll"),
                    Path.Combine(Environment.CurrentDirectory, "fbclient.dll"),
                    @"C:\Windows\System32\fbclient.dll",
                    @"C:\Windows\SysWOW64\fbclient.dll"
                };

                foreach (var caminho in caminhos)
                {
                    if (File.Exists(caminho))
                    {
                        var info = FileVersionInfo.GetVersionInfo(caminho);
                        var arquitetura = ObterArquitetura(caminho);
                        return $"{caminho} | Versão: {info.FileVersion} | Arquitetura: {arquitetura}";
                    }
                }
                return "fbclient.dll não encontrada em nenhum local conhecido";
            }
            catch (Exception ex)
            {
                return $"Erro ao inspecionar fbclient.dll: {ex.Message}";
            }
        }

        private static string ObterArquitetura(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);
                fs.Position = 0x3C;
                int peOffset = br.ReadInt32();
                fs.Position = peOffset + 4;
                ushort machine = br.ReadUInt16();
                return machine switch
                {
                    0x14c => "x86 (32 bits)",
                    0x8664 => "x64 (64 bits)",
                    0xAA64 => "ARM64",
                    _ => $"0x{machine:X4}"
                };
            }
            catch
            {
                return "desconhecida";
            }
        }

        public async Task<EstimativaViewModel> GerarEstimativaAsync(string firebirdConnectionString, string postgresConnectionString, List<string> tabelasSelecionadas, int tamanhoLote, bool modoSimulacao, CancellationToken cancellationToken = default)
        {
            var estimativa = new EstimativaViewModel
            {
                StringConexaoFirebird = firebirdConnectionString,
                TamanhoLote = tamanhoLote,
                ModoSimulacao = modoSimulacao
            };

            long totalGeral = 0;
            long totalNovos = 0;

            foreach (var tabela in TabelasSuportadas.Where(t => tabelasSelecionadas.Contains(t.Firebird)))
            {
                var item = new EstimativaTabelaViewModel
                {
                    NomeFirebird = tabela.Firebird,
                    NomePostgreSQL = tabela.Postgres,
                    Ordem = TabelasSuportadas.IndexOf(tabela)
                };

                try
                {
                    item.TotalRegistros = await _schemaComparer.ContarRegistrosFirebirdAsync(firebirdConnectionString, tabela.Firebird, cancellationToken);
                    item.RegistrosExistentes = await _schemaComparer.ContarRegistrosExistentesAsync(postgresConnectionString, tabela.Postgres, "Id", cancellationToken);
                    item.RegistrosNovos = Math.Max(0, item.TotalRegistros - item.RegistrosExistentes);

                    var comparacao = await _schemaComparer.CompararSchemasAsync(firebirdConnectionString, postgresConnectionString, tabela.Firebird, tabela.Postgres, cancellationToken);
                    item.Incompatibilidades = comparacao.ErrosBloqueantes;
                    item.Avisos = comparacao.Avisos;

                    // Estimativa: 1000 registros por segundo como base conservadora
                    double segundos = item.TotalRegistros / 1000.0;
                    item.TempoEstimado = TimeSpan.FromSeconds(Math.Max(1, segundos));

                    totalGeral += item.TotalRegistros;
                    totalNovos += item.RegistrosNovos;
                }
                catch (Exception ex)
                {
                    item.Incompatibilidades.Add($"Erro ao estimar: {ex.Message}");
                    estimativa.ErrosBloqueantes.Add($"{tabela.Firebird}: {ex.Message}");
                }

                estimativa.Tabelas.Add(item);
            }

            estimativa.TotalRegistros = totalGeral;
            estimativa.TotalNovos = totalNovos;
            estimativa.TempoTotalEstimado = TimeSpan.FromSeconds(Math.Max(1, totalGeral / 1000.0));
            estimativa.PodeProsseguir = !estimativa.ErrosBloqueantes.Any() && estimativa.Tabelas.Any();

            return estimativa;
        }

        public async Task<ImportacaoFinalViewModel> ImportarAsync(ImportacaoConfiguracao configuracao, string postgresConnectionString, CancellationToken cancellationToken = default)
        {
            var resultadoFinal = new ImportacaoFinalViewModel
            {
                ModoSimulacao = configuracao.ModoSimulacao
            };

            var tabelasParaImportar = TabelasSuportadas
                .Where(t => configuracao.TabelasSelecionadas.Contains(t.Firebird))
                .ToList();

            var stopwatchTotal = Stopwatch.StartNew();
            var tabelasImportadas = new List<string>();

            foreach (var tabela in tabelasParaImportar)
            {
                var resultadoTabela = await ImportarTabelaAsync(
                    configuracao.StringConexaoFirebird,
                    postgresConnectionString,
                    tabela.Firebird,
                    tabela.Postgres,
                    configuracao.TamanhoLote,
                    configuracao.ModoSimulacao,
                    configuracao.ConnectionId,
                    configuracao.IgnorarErros,
                    tabelasParaImportar.Count,
                    tabelasImportadas.Count,
                    cancellationToken);

                resultadoFinal.Resultados.Add(resultadoTabela);
                tabelasImportadas.Add(tabela.Postgres);

                if (!string.IsNullOrEmpty(resultadoTabela.MensagemErro) && !configuracao.IgnorarErros)
                {
                    resultadoFinal.MensagemFinal = $"Importação interrompida na tabela {tabela.Firebird}: {resultadoTabela.MensagemErro}";
                    break;
                }
            }

            stopwatchTotal.Stop();
            resultadoFinal.TempoTotal = stopwatchTotal.Elapsed.TotalSeconds;

            if (string.IsNullOrEmpty(resultadoFinal.MensagemFinal))
            {
                resultadoFinal.MensagemFinal = configuracao.ModoSimulacao
                    ? "Simulação concluída com sucesso. Nenhum dado foi gravado."
                    : "Importação concluída com sucesso.";
            }

            await _hubContext.Clients.Client(configuracao.ConnectionId).SendAsync("ReceberConclusao", resultadoFinal, cancellationToken);
            return resultadoFinal;
        }

        private async Task<ImportacaoResultadoViewModel> ImportarTabelaAsync(
            string firebirdConnectionString,
            string postgresConnectionString,
            string tabelaFirebird,
            string tabelaPostgres,
            int tamanhoLote,
            bool modoSimulacao,
            string connectionId,
            bool ignorarErros,
            int totalTabelas,
            int tabelasConcluidas,
            CancellationToken cancellationToken)
        {
            var resultado = new ImportacaoResultadoViewModel
            {
                NomeFirebird = tabelaFirebird,
                NomePostgreSQL = tabelaPostgres
            };

            var stopwatch = Stopwatch.StartNew();
            long registrosProcessados = 0;
            long totalRegistros = 0;

            try
            {
                await EnviarProgresso(connectionId, tabelaPostgres, 0, 0, "Iniciando leitura do Firebird", totalTabelas, tabelasConcluidas, cancellationToken);

                var comparacao = await _schemaComparer.CompararSchemasAsync(firebirdConnectionString, postgresConnectionString, tabelaFirebird, tabelaPostgres, cancellationToken);
                var colunasMapeadas = comparacao.Colunas.Where(c => c.Compativel).ToList();

                if (!colunasMapeadas.Any())
                {
                    resultado.MensagemErro = $"Nenhuma coluna compatível encontrada entre Firebird e PostgreSQL para a tabela {tabelaFirebird}";
                    resultado.Concluido = false;
                    return resultado;
                }

                totalRegistros = await _schemaComparer.ContarRegistrosFirebirdAsync(firebirdConnectionString, tabelaFirebird, cancellationToken);

                using var connFb = new FbConnection(firebirdConnectionString);
                await connFb.OpenAsync(cancellationToken);

                var colunasSelect = string.Join(", ", colunasMapeadas.Select(c => $"\"{c.NomeFirebird}\""));
                using var cmdFb = new FbCommand($"SELECT {colunasSelect} FROM \"{tabelaFirebird}\"", connFb);
                using var readerFb = await cmdFb.ExecuteReaderAsync(cancellationToken);

                var colunasInsert = colunasMapeadas.Select(c => $"\"{c.NomePostgreSQL}\"").ToList();
                var nomesColunas = string.Join(", ", colunasInsert);

                // Para controle de duplicidade por Id
                string? colunaId = colunasMapeadas.FirstOrDefault(c => c.NomeFirebird.Equals("Id", StringComparison.OrdinalIgnoreCase))?.NomePostgreSQL;

                var loteAtual = new List<Dictionary<string, object?>>();
                int numeroLote = 0;

                while (await readerFb.ReadAsync(cancellationToken))
                {
                    var registro = new Dictionary<string, object?>();
                    foreach (var coluna in colunasMapeadas)
                    {
                        var valorOrigem = readerFb[coluna.NomeFirebird];
                        var valorConvertido = _typeConverter.Converter(
                            valorOrigem,
                            coluna.ColunaFirebird?.Tipo ?? "VARCHAR",
                            coluna.ColunaPostgreSQL?.Tipo ?? "VARCHAR",
                            coluna.ColunaPostgreSQL?.Tamanho,
                            out var aviso);

                        registro[coluna.NomePostgreSQL] = valorConvertido;

                        if (!string.IsNullOrEmpty(aviso))
                        {
                            _eventLog.LogEventViewer($"[CargaDados] {tabelaPostgres}.{coluna.NomePostgreSQL}: {aviso}", "wWarning");
                        }
                    }

                    loteAtual.Add(registro);

                    if (loteAtual.Count >= tamanhoLote)
                    {
                        numeroLote++;
                        var resultadoLote = await ProcessarLoteAsync(
                            postgresConnectionString,
                            tabelaPostgres,
                            colunasInsert,
                            colunasMapeadas,
                            loteAtual,
                            colunaId,
                            modoSimulacao,
                            cancellationToken);

                        resultado.Inseridos += resultadoLote.Inseridos;
                        resultado.Duplicados += resultadoLote.Duplicados;
                        resultado.Erros += resultadoLote.Erros;

                        registrosProcessados += loteAtual.Count;
                        await EnviarProgresso(connectionId, tabelaPostgres, registrosProcessados, totalRegistros, $"Processando lote {numeroLote}", totalTabelas, tabelasConcluidas, cancellationToken);

                        loteAtual.Clear();

                        if (resultadoLote.Erros > 0 && !ignorarErros)
                        {
                            resultado.MensagemErro = $"Erro no lote {numeroLote} da tabela {tabelaFirebird}";
                            break;
                        }
                    }
                }

                // Processa lote residual
                if (loteAtual.Any() && string.IsNullOrEmpty(resultado.MensagemErro))
                {
                    numeroLote++;
                    var resultadoLote = await ProcessarLoteAsync(
                        postgresConnectionString,
                        tabelaPostgres,
                        colunasInsert,
                        colunasMapeadas,
                        loteAtual,
                        colunaId,
                        modoSimulacao,
                        cancellationToken);

                    resultado.Inseridos += resultadoLote.Inseridos;
                    resultado.Duplicados += resultadoLote.Duplicados;
                    resultado.Erros += resultadoLote.Erros;
                    registrosProcessados += loteAtual.Count;
                }

                resultado.TotalLido = totalRegistros;
                stopwatch.Stop();
                resultado.TempoGasto = stopwatch.Elapsed.TotalSeconds;
                resultado.Concluido = string.IsNullOrEmpty(resultado.MensagemErro);

                await EnviarProgresso(connectionId, tabelaPostgres, registrosProcessados, totalRegistros, resultado.Concluido ? "Concluído" : "Interrompido", totalTabelas, tabelasConcluidas + 1, cancellationToken);
            }
            catch (Exception ex)
            {
                resultado.MensagemErro = ex.Message;
                resultado.Concluido = false;
                _eventLog.LogEventViewer($"[CargaDados] Erro ao importar {tabelaFirebird}: {ex.Message}", "wError");
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceberErro", new { Tabela = tabelaPostgres, Erro = ex.Message }, cancellationToken);
            }

            return resultado;
        }

        private async Task<(long Inseridos, long Duplicados, long Erros)> ProcessarLoteAsync(
            string postgresConnectionString,
            string tabelaPostgres,
            List<string> colunasInsert,
            List<MapeamentoColunas> colunasMapeadas,
            List<Dictionary<string, object?>> registros,
            string? colunaId,
            bool modoSimulacao,
            CancellationToken cancellationToken)
        {
            long inseridos = 0;
            long duplicados = 0;
            long erros = 0;

            if (modoSimulacao)
            {
                return (registros.Count, 0, 0);
            }

            using var connPg = new NpgsqlConnection(postgresConnectionString);
            await connPg.OpenAsync(cancellationToken);

            using var transaction = await connPg.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var registro in registros)
                {
                    try
                    {
                        // Verifica duplicidade por Id
                        if (!string.IsNullOrEmpty(colunaId) && registro.TryGetValue(colunaId, out var idValue) && idValue != null && idValue != DBNull.Value)
                        {
                            using var cmdExists = new NpgsqlCommand($"SELECT 1 FROM \"{tabelaPostgres}\" WHERE \"{colunaId}\" = @id", connPg, transaction);
                            cmdExists.Parameters.AddWithValue("id", idValue);
                            var exists = await cmdExists.ExecuteScalarAsync(cancellationToken);
                            if (exists != null)
                            {
                                duplicados++;
                                continue;
                            }
                        }

                        var parametros = colunasInsert.Select((c, i) => $"@p{i}").ToList();
                        var sql = $"INSERT INTO \"{tabelaPostgres}\" ({string.Join(", ", colunasInsert)}) VALUES ({string.Join(", ", parametros)})";

                        using var cmdInsert = new NpgsqlCommand(sql, connPg, transaction);
                        int idx = 0;
                        foreach (var coluna in colunasMapeadas)
                        {
                            var valor = registro[coluna.NomePostgreSQL];
                            if (valor == null || valor == DBNull.Value)
                            {
                                cmdInsert.Parameters.AddWithValue($"p{idx}", DBNull.Value);
                            }
                            else
                            {
                                cmdInsert.Parameters.AddWithValue($"p{idx}", valor);
                            }
                            idx++;
                        }

                        await cmdInsert.ExecuteNonQueryAsync(cancellationToken);
                        inseridos++;
                    }
                    catch (Exception ex)
                    {
                        erros++;
                        _eventLog.LogEventViewer($"[CargaDados] Erro ao inserir registro em {tabelaPostgres}: {ex.Message}", "wError");
                    }
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                erros += registros.Count;
                _eventLog.LogEventViewer($"[CargaDados] Rollback do lote em {tabelaPostgres}: {ex.Message}", "wError");
                throw;
            }

            return (inseridos, duplicados, erros);
        }

        private async Task EnviarProgresso(
            string connectionId,
            string tabelaAtual,
            long processados,
            long total,
            string status,
            int totalTabelas,
            int tabelasConcluidas,
            CancellationToken cancellationToken)
        {
            double porcentagemTabela = total > 0 ? (processados / (double)total) * 100 : 0;
            double porcentagemGeral = (tabelasConcluidas / (double)totalTabelas * 100) + (porcentagemTabela / totalTabelas);
            int porcentagem = Math.Min(100, Math.Max(1, (int)porcentagemGeral));

            var progresso = new
            {
                PorcentagemTotal = porcentagem,
                TabelaAtual = tabelaAtual,
                RegistrosProcessados = processados,
                TotalRegistros = total,
                Status = status,
                EmExecucao = true
            };

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceberProgresso", progresso, cancellationToken);
        }
    }
}
