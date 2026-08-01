using ExtensionsMethods.EventViewerHelper;
using FirebirdSql.Data.FirebirdClient;
using LabWebMvc.MVC.ViewModel.CargaDados;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using System.Data;
using System.Data.Common;
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

        // Ordem de limpeza (DELETE) respeitando dependencias de FK (filhas antes dos pais),
        // conforme ordem de DROP do SQL Tabelas_Vazias.sql.
        // A importacao real inverte esta ordem (pais antes dos filhos).
        private static readonly List<(string Firebird, string Postgres, string Descricao)> TabelasSuportadas = new()
        {
            ("TextosProntos", "TextosProntos", "Textos prontos"),
            ("SituacaoExames", "SituacaoExames", "Situações de exames"),
            ("RequisicaoOriginal", "Requisitar", "Requisições"),
            ("PlanoExames", "PlanoExames", "Planos de exames"),
            ("Logradouro", "Logradouro", "Logradouros"),
            ("FichasPlanilhas", "FichasPlanilhas", "Planilhas de fichas"),
            ("FichasLotes", "FichasLotes", "Lotes de fichas"),
            ("FichasInternas", "FichasInternas", "Fichas internas"),
            ("ExamesExportados", "ExamesExportados", "Exames exportados"),
            ("ItensExamesRealizados", "ItensExamesRealizados", "Itens de exames realizados"),
            ("ItensExamesRealizadosAM", "ItensExamesRealizadosAM", "Itens de exames realizados (arquivados)"),
            ("ExamesRealizados", "ExamesRealizados", "Exames realizados"),
            ("ExamesRealizadosAM", "ExamesRealizadosAM", "Exames realizados (arquivados)"),
            ("ExamesPendentes", "ExamesPendentes", "Exames pendentes"),
            ("ExamesImpressos", "ExamesImpressos", "Exames impressos"),
            ("TabelaExames", "TabelaExames", "Tabelas de exames"),
            ("Medicos", "Medicos", "Médicos"),
            ("Clientes", "Pacientes", "Pacientes (Clientes no Firebird)"),
            ("ClasseExames", "ClasseExames", "Classes de exames"),
            ("Postos", "Postos", "Postos de coleta"),
            ("Instituicao", "Instituicao", "Instituições")
        };

        // Ordem fixa e absoluta das tabelas existentes no Firebird (Serão usadas para ordenar a limpeza/DELETE): filhas antes dos pais.
        // Nao depende de metadados do PostgreSQL nem de ordenacao topologica.
        // A importacao usa a ordem inversa desta lista (pais antes dos filhos).
        private static readonly List<string> OrdemFirebirdHardcoded = new()
        {
            "TextosProntos",
            "SituacaoExames",
            "RequisicaoOriginal",
            "PlanoExames",
            "Logradouro",
            "FichasPlanilhas",
            "FichasLotes",
            "FichasInternas",
            "ExamesExportados",
            "ItensExamesRealizados",
            "ItensExamesRealizadosAM",
            "ExamesRealizados",
            "ExamesRealizadosAM",
            "ExamesPendentes",
            "ExamesImpressos",
            "TabelaExames",
            "Medicos",
            "Clientes",
            "ClasseExames",
            "Postos",
            "Instituicao"
        };

        // Mapeamento de nomes de colunas Firebird -> PostgreSQL quando os nomes diferem.
        // A PK "Codigo" (ou variantes) do Firebird e preservada como "Id" no PostgreSQL.
        private static readonly Dictionary<string, Dictionary<string, string>> MapeamentosColunas = new()
        {
            ["Clientes"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["NomeCliente"] = "NomePaciente"
            },
            ["Medicos"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id"
            },
            ["TabelaExames"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id"
            },
            ["ClasseExames"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id"
            },
            ["Instituicao"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["Instituicao"] = "Sigla"
            },
            ["Postos"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["Instituicao"] = "InstituicaoId"
            },
            ["Logradouro"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id"
            },
            ["SituacaoExames"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id"
            },
            ["TextosProntos"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id"
            },
            ["PlanoExames"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["SiglaTabela"] = "TabelaExamesId"
                // ClasseExamesId é derivado da ContaExame (dígitos 3-4) no pós-processamento.
                // CodigoItem do Firebird NÃO é a folha — é o código do item dentro da folha.
            },
            ["ExamesRealizados"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CodigoExame"] = "Id",
                ["CodigoCliente"] = "PacienteId",
                ["Instituicao"] = "InstituicaoId",
                ["MedicoResp"] = "MedicoId",
                ["SiglaTabela"] = "TabelaExamesId"
            },
            ["ExamesRealizadosAM"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CodigoExame"] = "Id",
                ["SiglaTabela"] = "TabelaExamesId",
                ["CodigoCliente"] = "PacienteId",
                ["Instituicao"] = "InstituicaoId",
                ["MedicoResp"] = "MedicoId",
                ["CodigoCabecalhoFolha"] = "OrigemId"
            },
            ["ItensExamesRealizados"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["CodigoCliente"] = "PacienteId",
                ["CodigoExame"] = "ExameRealizadoId",
                ["SiglaTabela"] = "TabelaExamesId",
                ["CodigoItem"] = "ClasseExamesId",
                ["Instituicao"] = "InstituicaoId"
            },
            ["ItensExamesRealizadosAM"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CodigoUnico"] = "Id",
                ["Codigo"] = "OrigemAmid",
                ["CodigoCliente"] = "PacienteId",
                ["CodigoExame"] = "ExameRealizadoAMId",
                ["SiglaTabela"] = "TabelaExamesId",
                ["CodigoItem"] = "ClasseExamesId",
                ["Instituicao"] = "InstituicaoId"
            },
            ["FichasInternas"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["CodigoExame"] = "ExamesRealizadosId",
                ["CodigoCliente"] = "PacienteId",
                ["CodigoMedico"] = "MedicoId",
                ["Instituicao"] = "InstituicaoId"
            },
            ["FichasLotes"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                // CodigoExame removido: NAO e FK para ExamesRealizados no Firebird
                // (valores 47075-47246 nao existem em ExamesRealizados 56333-89766)
                ["CodigoCliente"] = "PacienteId",
                ["CodigoMedico"] = "MedicoId",
                ["Instituicao"] = "InstituicaoId",
                ["SiglaTabela"] = "TabelaExamesId"
            },
            ["FichasPlanilhas"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["CodigoExame"] = "ExamesRealizadosId",
                ["CodigoCliente"] = "PacienteId",
                ["CodigoMedico"] = "MedicoId",
                ["Instituicao"] = "InstituicaoId",
                ["SiglaTabela"] = "TabelaExamesId"
            },
            ["RequisicaoOriginal"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["CodigoCliente"] = "PacienteId",
                ["CodigoExame"] = "ExameRealizadoId",
                ["SiglaTabela"] = "TabelaExamesId",
                ["CodigoItem"] = "ClasseExamesId",
                ["Instituicao"] = "InstituicaoId"
            },
            ["ExamesPendentes"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CodigoExame"] = "Id",
                ["CodigoCliente"] = "PacienteId",
                ["Instituicao"] = "InstituicaoId",
                ["MedicoResp"] = "MedicoId",
                ["SiglaTabela"] = "TabelaExamesId"
            },
            ["ExamesImpressos"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CodigoExame"] = "Id",
                ["CodigoCliente"] = "PacienteId",
                ["Instituicao"] = "InstituicaoId",
                ["SiglaTabela"] = "TabelaExamesId"
            },
            ["ExamesExportados"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Codigo"] = "Id",
                ["CodigoExame"] = "ExameId",
                ["CodigoCliente"] = "PacienteId",
                ["Instituicao"] = "InstituicaoId",
                ["MedicoResp"] = "MedicoId",
                ["SiglaTabela"] = "TabelaExamesId"
            }
        };

        // Colunas FK que no Firebird sao texto (sigla/nome) e no PostgreSQL sao int (ID).
        // Essas colunas precisam que o valor string original seja preservado para o
        // pós-processamento fazer o lookup texto -> ID. Sem isso, o TypeConverter converte
        // strings não-numéricas para 0, impedindo o lookup e violando a FK.
        private static readonly HashSet<string> ColunasLookupTextoParaId = new(StringComparer.OrdinalIgnoreCase)
        {
            "InstituicaoId",
            "TabelaExamesId"
        };

        // Defaults especificos para colunas obrigatorias do PostgreSQL ausentes no Firebird
        // na tabela Instituicao.
        private static readonly Dictionary<string, object> DefaultsInstituicao = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sigla"] = string.Empty,
            ["Nome"] = string.Empty,
            ["CNPJ"] = string.Empty,
            ["Sequencial"] = 0,
            ["Email"] = string.Empty,
            ["CarimboSN"] = 0,
            ["TimbreSN"] = 0,
            ["Contato"] = string.Empty,
            ["Telefone"] = string.Empty
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
                //Feito pelo Kiro em 26/07/2026
                // NETProvider 10.x é 100% managed (wire protocol puro, sem fbclient.dll nativa).
                // Charset NONE na conexão para schema comparison e contagem de registros.
                // A importação de dados textuais usa ODBC com Charset=NONE (reconexão por tabela).
                // NETProvider com CAST CHARACTER SET NONE NÃO preserva acentos (bug 10.3.2 — retorna U+FFFD).
                // ODBC com Charset=NONE é a ÚNICA forma que preserva acentos (confirmado).
                Charset = "NONE",
                //..Kiro
                Dialect = 3,
                ServerType = 0 // TCP/IP padrão. Não usa XNET nem Embedded.
            };
            return builder.ToString();
        }

        public string MontarStringConexaoODBC(FirebirdConnectionViewModel viewModel)
        {
            //Feito pelo Kiro em 26/07/2026
            // Connection string ODBC DSN-less — usada para:
            // 1. Teste de conexão ODBC (TestarConexaoODBCAsync)
            // 2. Importação de dados textuais (ImportarTabelaAsync) — ODBC com Charset=NONE
            //    é a ÚNICA forma que preserva acentos pt-BR (confirmado).
            // NETProvider NÃO funciona para encoding nem com CAST NONE (bug 10.3.2 — retorna U+FFFD).
            // Cada tabela abre/fecha sua própria conexão ODBC (evita crash em tabelas grandes).
            var builder = new StringBuilder();
            builder.Append($"Driver={{Firebird/InterBase(r) driver}};");
            builder.Append($"Dbname={viewModel.Servidor}/{viewModel.Porta}:{viewModel.CaminhoBanco};");
            builder.Append($"Uid={viewModel.Usuario};");
            builder.Append($"Pwd={viewModel.Senha};");
            builder.Append("Charset=NONE;");
            return builder.ToString();
            //..Kiro
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

            var tabelasFirebird = await ObterNomesTabelasFirebirdAsync(firebirdConnectionString, cancellationToken);

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
                    // Verifica se a tabela existe no Firebird. Se nao existir, sera limpa no PostgreSQL
                    // mas nao importada, e nao deve bloquear a estimativa.
                    bool tabelaExisteNoFirebird = tabelasFirebird.Contains(tabela.Firebird);
                    if (!tabelaExisteNoFirebird)
                    {
                        item.TotalRegistros = 0;
                        item.RegistrosExistentes = 0;
                        item.RegistrosNovos = 0;
                        item.TempoEstimado = TimeSpan.FromSeconds(1);
                        item.Incompatibilidades.Add($"Tabela '{tabela.Firebird}' não encontrada no Firebird. Será apenas limpa no PostgreSQL.");
                        estimativa.Tabelas.Add(item);
                        continue;
                    }

                    item.TotalRegistros = await _schemaComparer.ContarRegistrosFirebirdAsync(firebirdConnectionString, tabela.Firebird, cancellationToken);

                    // Todas as tabelas sao limpas antes da importacao; nao ha registros existentes.
                    item.RegistrosExistentes = 0;
                    item.RegistrosNovos = item.TotalRegistros;

                    //Feito pelo Kiro em 26/07/2026
                    // Estimativa de tempo baseada no tamanho do lote configurado pelo usuário.
                    // Taxa base: 500 reg/s com lote padrão de 1000 registros.
                    // Lotes maiores reduzem overhead de commit/transação proporcionalmente.
                    // Fórmula: taxaEfetiva = taxaBase * (tamanhoLote / loteReferencia)
                    // Limitada entre 300 (mínimo realista) e 1200 (máximo com lotes muito grandes).
                    const double taxaBase = 500.0;
                    const int loteReferencia = 1000;
                    double fatorLote = (double)tamanhoLote / loteReferencia;
                    double taxaEfetiva = Math.Clamp(taxaBase * fatorLote, 300.0, 1200.0);
                    double segundos = item.TotalRegistros / taxaEfetiva;
                    item.TempoEstimado = TimeSpan.FromSeconds(Math.Max(1, segundos));
                    //..Kiro

                    totalGeral += item.TotalRegistros;
                    totalNovos += item.RegistrosNovos;
                }
                catch (Exception ex)
                {
                    var mensagemTela = FormatarMensagemErroTela(ex);
                    var detalhado = $"{ex.GetType().Name}: {ex.Message} | Inner: {ex.InnerException?.Message} | StackTrace: {ex.StackTrace}";
                    LogEmArquivo($"[Estimativa] ERRO em {tabela.Firebird}: {detalhado}", erro: true);
                    _eventLog.LogEventViewer($"[Estimativa] ERRO em {tabela.Firebird}: {detalhado}", "wError");
                    item.Incompatibilidades.Add($"Erro ao estimar: {mensagemTela}");
                    estimativa.ErrosBloqueantes.Add($"{tabela.Firebird}: {mensagemTela}");
                }

                estimativa.Tabelas.Add(item);
            }

            estimativa.TotalRegistros = totalGeral;
            estimativa.TotalNovos = totalNovos;
            //Feito pelo Kiro em 26/07/2026
            // Tempo total usando mesma fórmula com fator de lote
            double fatorLoteTotal = (double)tamanhoLote / 1000;
            double taxaEfetivaTotal = Math.Clamp(500.0 * fatorLoteTotal, 300.0, 1200.0);
            estimativa.TempoTotalEstimado = TimeSpan.FromSeconds(Math.Max(1, totalGeral / taxaEfetivaTotal));
            //..Kiro
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

            // Verifica quais tabelas selecionadas realmente existem no Firebird.
            // Tabelas ausentes no Firebird serao limpas no PostgreSQL, mas nao importadas.
            var tabelasFirebird = await ObterNomesTabelasFirebirdAsync(configuracao.StringConexaoFirebird, cancellationToken);
            var tabelasExistentesNoFirebird = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tabela in tabelasParaImportar)
            {
                if (tabelasFirebird.Contains(tabela.Firebird))
                {
                    tabelasExistentesNoFirebird.Add(tabela.Firebird);
                }
            }

            var stopwatchTotal = Stopwatch.StartNew();
            var tabelasImportadas = new List<string>();

            // Mapeamentos de nomes Firebird <-> PostgreSQL para ordenação topológica.
            var firebirdParaPostgres = TabelasSuportadas
                .ToDictionary(t => t.Firebird, t => t.Postgres, StringComparer.OrdinalIgnoreCase);
            var postgresParaFirebird = TabelasSuportadas
                .ToDictionary(t => t.Postgres, t => t.Firebird, StringComparer.OrdinalIgnoreCase);

            // Carrega dependências reais de FK do PostgreSQL.
            var dependenciasFk = await _schemaComparer.ObterDependenciasFkAsync(postgresConnectionString, cancellationToken);

            // Fase 1: limpar todas as tabelas na ordem inversa das dependencias de FK
            // (tabelas filhas antes das tabelas pais), para evitar erros 23503 ao deletar registros
            // referenciados por chaves estrangeiras RESTRICT.
            // A limpeza inclui automaticamente tabelas filhas nao selecionadas que referenciam as tabelas escolhidas.
            var (tabelasLimpezaPostgres, tabelasAdicionadasLimpeza) = await LimparTodasTabelasPreImportacaoAsync(
                postgresConnectionString,
                tabelasParaImportar,
                dependenciasFk,
                firebirdParaPostgres,
                postgresParaFirebird,
                configuracao.ConnectionId,
                tabelasParaImportar.Count,
                configuracao.TamanhoLote,
                cancellationToken);

            // Fase 2: importar na ordem inversa da limpeza (pais antes dos filhos),
            // garantindo que as chaves estrangeiras sejam respeitadas durante a insercao.
            // Usa ordenação topológica baseada nas FKs reais do PostgreSQL.
            var tabelasImportacaoPostgres = tabelasParaImportar
                .Where(t => tabelasExistentesNoFirebird.Contains(t.Firebird))
                .Select(t => t.Postgres)
                .ToList();

            var ordemImportacaoPostgres = _schemaComparer.OrdenarParaImportacao(tabelasImportacaoPostgres, dependenciasFk)
                .Where(postgresParaFirebird.ContainsKey)
                .ToList();

            var mapeamentoFirebirdParaPostgres = tabelasParaImportar
                .ToDictionary(t => t.Firebird, t => t, StringComparer.OrdinalIgnoreCase);

            // Carrega lookup de Instituicao (texto -> Id) para converter as FKs textuais
            // do Firebird em IDs numericos do PostgreSQL.
            // Tenta primeiro do Firebird; se a tabela nao existir la, carrega do PostgreSQL.
            var lookupInstituicao = await CarregarLookupInstituicaoAsync(configuracao.StringConexaoFirebird, postgresConnectionString, cancellationToken);
            var lookupTabelaExames = await CarregarLookupTabelaExamesAsync(configuracao.StringConexaoFirebird, postgresConnectionString, cancellationToken);
            HashSet<long>? idsClasseExamesValidos = null;

            //Feito pelo Kiro em 26/07/2026
            // Fase de Preparação via FbConnection (NETProvider 10.x — 100% managed, wire protocol puro):
            // faz schema comparison e contagem de registros para TODAS as tabelas.
            // NETProvider 10.x NÃO usa fbclient.dll — é wire protocol managed, sem crash nativo.
            // A contagem usa _schemaComparer.ContarRegistrosFirebirdAsync (FbConnection interna).
            var schemaCache = new Dictionary<string, SchemaComparisonResult>(StringComparer.OrdinalIgnoreCase);
            var contagemCache = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            foreach (var tabela in tabelasParaImportar)
            {
                if (!tabelasExistentesNoFirebird.Contains(tabela.Firebird))
                    continue;

                MapeamentosColunas.TryGetValue(tabela.Firebird, out var mapeamentoColunas);
                var comparacao = await _schemaComparer.CompararSchemasAsync(
                    configuracao.StringConexaoFirebird, postgresConnectionString,
                    tabela.Firebird, tabela.Postgres, mapeamentoColunas, cancellationToken);
                schemaCache[tabela.Firebird] = comparacao;

                // Contagem via FbConnection (NETProvider managed — sem fbclient.dll)
                contagemCache[tabela.Firebird] = await _schemaComparer.ContarRegistrosFirebirdAsync(
                    configuracao.StringConexaoFirebird, tabela.Firebird, cancellationToken);
            }
            //..Kiro

            foreach (var nomePostgres in ordemImportacaoPostgres)
            {
                var nomeFirebird = postgresParaFirebird[nomePostgres];
                var tabela = mapeamentoFirebirdParaPostgres[nomeFirebird];

                if (!tabelasExistentesNoFirebird.Contains(tabela.Firebird))
                {
                    resultadoFinal.Resultados.Add(new ImportacaoResultadoViewModel
                    {
                        NomeFirebird = tabela.Firebird,
                        NomePostgreSQL = tabela.Postgres,
                        TotalLido = 0,
                        Inseridos = 0,
                        Concluido = true,
                        MensagemErro = "Tabela não encontrada no Firebird. Registros do PostgreSQL foram limpos, mas nenhum dado foi importado."
                    });
                    tabelasImportadas.Add(tabela.Postgres);
                    continue;
                }

                //Feito pelo Kiro em 26/07/2026
                // try/catch global por tabela: protege contra exceções inesperadas
                // que poderiam interromper toda a importação.
                ImportacaoResultadoViewModel? resultadoTabela = null;
                try
                {
                    resultadoTabela = await ImportarTabelaAsync(
                        configuracao.StringConexaoFirebird,
                        postgresConnectionString,
                        tabela.Firebird,
                        tabela.Postgres,
                        configuracao.TamanhoLote,
                        configuracao.ModoSimulacao,
                        schemaCache[tabela.Firebird],
                        contagemCache[tabela.Firebird],
                        configuracao.ConnectionId,
                        configuracao.IgnorarErros,
                        tabelasParaImportar.Count,
                        tabelasImportadas.Count,
                        lookupInstituicao,
                        lookupTabelaExames,
                        dependenciasFk,
                        idsClasseExamesValidos,
                        cancellationToken);

                    resultadoFinal.Resultados.Add(resultadoTabela);
                }
                catch (Exception exFatal)
                {
                    var msgFatal = $"Erro fatal na tabela {tabela.Firebird}: {exFatal.GetType().Name}: {exFatal.Message}";
                    LogEmArquivo(msgFatal, erro: true);
                    _eventLog.LogEventViewer($"[CargaDados] {msgFatal}", "wError");
                    resultadoTabela = new ImportacaoResultadoViewModel
                    {
                        NomeFirebird = tabela.Firebird,
                        NomePostgreSQL = tabela.Postgres,
                        MensagemErro = msgFatal,
                        Concluido = false
                    };
                    resultadoFinal.Resultados.Add(resultadoTabela);
                }
                //..Kiro
                tabelasImportadas.Add(tabela.Postgres);

                // Apos importar ClasseExames, carrega os IDs validos para filtrar registros orfaos nas tabelas filhas.
                if (tabela.Postgres.Equals("ClasseExames", StringComparison.OrdinalIgnoreCase)
                    && !configuracao.ModoSimulacao
                    && string.IsNullOrEmpty(resultadoTabela?.MensagemErro))
                {
                    idsClasseExamesValidos = await CarregarIdsClasseExamesAsync(postgresConnectionString, cancellationToken);
                }

                //Feito pelo Kiro em 01/08/2026
                // Após importar PlanoExames, cria registros de Folha (0000000) ausentes.
                // No Firebird, nem todas as tabelas de preço tinham o registro de cabeçalho da Folha.
                // A Folha é obrigatória para exibição hierárquica no grid (Folha → Principal → Itens).
                if (tabela.Postgres.Equals("PlanoExames", StringComparison.OrdinalIgnoreCase)
                    && !configuracao.ModoSimulacao
                    && string.IsNullOrEmpty(resultadoTabela?.MensagemErro)
                    && resultadoTabela?.Inseridos > 0)
                {
                    try
                    {
                        var folhasCriadas = await CriarFolhasAusentesPlanoExamesAsync(postgresConnectionString, cancellationToken);
                        if (folhasCriadas > 0)
                        {
                            LogEmArquivo($"[CargaDados] PlanoExames: {folhasCriadas} registros de Folha ausentes criados automaticamente.", erro: true);
                            _eventLog.LogEventViewer($"[CargaDados] PlanoExames: {folhasCriadas} registros de Folha ausentes criados.", "wInfo");
                        }
                    }
                    catch (Exception exFolha)
                    {
                        LogEmArquivo($"[CargaDados] AVISO: falha ao criar Folhas ausentes do PlanoExames: {exFolha.Message}", erro: true);
                    }
                }
                //..Kiro

                if (!string.IsNullOrEmpty(resultadoTabela?.MensagemErro))
                {
                    var mensagemErro = $"[ImportarAsync] ERRO na tabela {tabela.Firebird}: {resultadoTabela.MensagemErro}";
                    LogEmArquivo(mensagemErro, erro: true);
                    _eventLog.LogEventViewer(mensagemErro, "wError");
                    // Nao interrompe a importacao; prossegue com as demais tabelas.
                }
            }

            //Feito pelo Kiro em 27/07/2026
            // Fase 3: Deduplicação pós-importação (Pacientes e Médicos).
            // Após importar todas as tabelas, elimina duplicatas mantendo o maior Id
            // e migrando FKs das tabelas filhas para o registro sobrevivente.
            // Critérios: Pacientes por NomePaciente+Nascimento, Médicos por NomeMedico+CRM.
            if (!configuracao.ModoSimulacao)
            {
                var tabelasImportadasComSucesso = resultadoFinal.Resultados
                    .Where(r => r.Concluido && r.Inseridos > 0)
                    .Select(r => r.NomePostgreSQL)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                bool importouPacientes = tabelasImportadasComSucesso.Contains("Pacientes");
                bool importouMedicos = tabelasImportadasComSucesso.Contains("Medicos");

                if (importouPacientes || importouMedicos)
                {
                    await EnviarProgresso(configuracao.ConnectionId, "Deduplicação", 0, 0,
                        "Iniciando deduplicação pós-importação...",
                        tabelasParaImportar.Count, tabelasImportadas.Count, "Deduplicação", cancellationToken);

                    try
                    {
                        var resultadoDedup = await DeduplicarPosImportacaoAsync(
                            postgresConnectionString, importouPacientes, importouMedicos,
                            configuracao.ConnectionId, tabelasParaImportar.Count, tabelasImportadas.Count,
                            cancellationToken);

                        resultadoFinal.DeduplicacaoPacientes = resultadoDedup.PacientesRemovidos;
                        resultadoFinal.DeduplicacaoMedicos = resultadoDedup.MedicosRemovidos;
                    }
                    catch (Exception exDedup)
                    {
                        var msgDedup = $"Aviso: falha na deduplicação pós-importação: {exDedup.Message}";
                        LogEmArquivo($"[CargaDados] {msgDedup}", erro: true);
                        _eventLog.LogEventViewer($"[CargaDados] {msgDedup}", "wWarning");
                    }
                }
            }
            //..Kiro

            stopwatchTotal.Stop();
            resultadoFinal.TempoTotal = stopwatchTotal.Elapsed.TotalSeconds;

            var tabelasComErro = resultadoFinal.Resultados
                .Where(r => !string.IsNullOrEmpty(r.MensagemErro) || r.Erros > 0)
                .Select(r => r.NomeFirebird)
                .ToList();

            var totalErros = resultadoFinal.Resultados.Sum(r => r.Erros);
            var totalInseridos = resultadoFinal.Resultados.Sum(r => r.Inseridos);
            var totalDuplicados = resultadoFinal.Resultados.Sum(r => r.Duplicados);

            // Extrai o charset da string de conexão para incluir na mensagem final
            string charsetUsado = "ODBC/NONE";
            var sufixoCharset = $" Driver: {charsetUsado}.";

            if (tabelasComErro.Any())
            {
                resultadoFinal.MensagemFinal = $"Importação concluída com ressalvas. Tabelas com erro: {string.Join(", ", tabelasComErro)}. Total de registros com erro: {totalErros}. Inseridos: {totalInseridos}. Duplicados/Ignorados: {totalDuplicados}.{sufixoCharset}";
            }
            else if (string.IsNullOrEmpty(resultadoFinal.MensagemFinal))
            {
                resultadoFinal.MensagemFinal = configuracao.ModoSimulacao
                    ? $"Simulação concluída com sucesso. Nenhum dado foi gravado. (Inseridos simulados: {totalInseridos}){sufixoCharset}"
                    : $"Importação concluída com sucesso. Inseridos: {totalInseridos}. Duplicados/Ignorados: {totalDuplicados}.{sufixoCharset}";
            }

            //Feito pelo Kiro em 27/07/2026
            // Adiciona informação da deduplicação à mensagem final quando houver remoções
            if (resultadoFinal.DeduplicacaoPacientes > 0 || resultadoFinal.DeduplicacaoMedicos > 0)
            {
                var partes = new List<string>();
                if (resultadoFinal.DeduplicacaoPacientes > 0)
                    partes.Add($"Pacientes: {resultadoFinal.DeduplicacaoPacientes}");
                if (resultadoFinal.DeduplicacaoMedicos > 0)
                    partes.Add($"Médicos: {resultadoFinal.DeduplicacaoMedicos}");
                resultadoFinal.MensagemFinal += $" Deduplicação: {string.Join(", ", partes)} duplicatas removidas.";
            }
            //..Kiro

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
            SchemaComparisonResult comparacaoPreparada,
            long totalRegistrosPreparado,
            string connectionId,
            bool ignorarErros,
            int totalTabelas,
            int tabelasConcluidas,
            Dictionary<string, int> lookupInstituicao,
            Dictionary<string, int> lookupTabelaExames,
            List<DependenciaFk> dependenciasFk,
            HashSet<long>? idsClasseExamesValidos,
            CancellationToken cancellationToken)
        {
            var resultado = new ImportacaoResultadoViewModel
            {
                NomeFirebird = tabelaFirebird,
                NomePostgreSQL = tabelaPostgres
            };

            var stopwatch = Stopwatch.StartNew();
            long registrosProcessados = 0;
            long totalRegistros = totalRegistrosPreparado;

            try
            {
                LogEmArquivo($"[DIAG-CRASH] ====== INÍCIO ImportarTabelaAsync: {tabelaFirebird} → {tabelaPostgres} (totalRegistros={totalRegistrosPreparado}) ======", erro: true);
                await EnviarProgresso(connectionId, tabelaPostgres, 0, 0, "Iniciando leitura do Firebird", totalTabelas, tabelasConcluidas, "Importação", cancellationToken);

                //Feito pelo Kiro em 26/07/2026
                // Usa schema comparison e contagem pré-computadas na fase de preparação
                // (FbConnection já fechada). Evita abrir FbConnection durante a importação ODBC.
                var comparacao = comparacaoPreparada;
                //..Kiro
                var colunasMapeadas = comparacao.Colunas.Where(c => c.Compativel).ToList();

                // Adiciona colunas obrigatórias do PostgreSQL que não existem no Firebird.
                // Essas colunas serão preenchidas com valores padrão compatíveis com o tipo.
                var colunasMapeadasNomes = colunasMapeadas.Select(c => c.NomePostgreSQL.ToUpperInvariant()).ToHashSet();
                var colunasObrigatoriasAusentes = comparacao.ColunasPostgreSQL
                    .Where(pg => !colunasMapeadasNomes.Contains(pg.Nome.ToUpperInvariant()) && !pg.Nullable && !pg.IsAutoIncrement)
                    .Select(pg => new MapeamentoColunas
                    {
                        NomeFirebird = pg.Nome,
                        NomePostgreSQL = pg.Nome,
                        ColunaFirebird = null,
                        ColunaPostgreSQL = pg,
                        Compativel = true
                    })
                    .ToList();

                if (colunasObrigatoriasAusentes.Any())
                {
                    _eventLog.LogEventViewer($"[CargaDados] {tabelaPostgres}: colunas obrigatórias ausentes no Firebird serão preenchidas com padrão: {string.Join(", ", colunasObrigatoriasAusentes.Select(c => c.NomePostgreSQL))}", "wInfo");
                    colunasMapeadas.AddRange(colunasObrigatoriasAusentes);
                }

                _eventLog.LogEventViewer($"[CargaDados] DIAGNOSTICO {tabelaPostgres}: colunas mapeadas = {string.Join(", ", colunasMapeadas.Select(c => $"{c.NomePostgreSQL}(nullable:{c.ColunaPostgreSQL?.Nullable.ToString() ?? "?"})"))}", "wInfo");

                if (!colunasMapeadas.Any())
                {
                    resultado.MensagemErro = $"Nenhuma coluna compatível encontrada entre Firebird e PostgreSQL para a tabela {tabelaFirebird}";
                    resultado.Concluido = false;
                    return resultado;
                }

                // Abre conexao PostgreSQL para a tabela. A limpeza das demais tabelas
                // ja foi feita na fase de pre-limpeza (ordem inversa das FKs).
                using var connPgExterna = new NpgsqlConnection(postgresConnectionString);
                await connPgExterna.OpenAsync(cancellationToken);

                // Configura keepalive e desativa timeout de transacao ociosa para evitar
                // fechamento da conexao durante importacoes longas.
                await using (var cmdConfig = new NpgsqlCommand(
                    "SET idle_in_transaction_session_timeout = 0; " +
                    "SET statement_timeout = 0;", connPgExterna))
                {
                    await cmdConfig.ExecuteNonQueryAsync(cancellationToken);
                }

                //Feito pelo Kiro em 26/07/2026
                // Importação via ODBC com Charset=NONE — ÚNICA forma que preserva acentos (confirmado).
                // NETProvider 10.x com CAST CHARACTER SET NONE NÃO funciona (retorna U+FFFD — bug 10.3.2).
                // ODBC com Charset=NONE preserva acentos CORRETAMENTE.
                //
                // LEITURA PAGINADA (FIRST/SKIP do Firebird 2.5):
                // Cada página abre/fecha sua própria conexão ODBC fresca, lê um lote de N registros
                // (usando SELECT FIRST N SKIP M), fecha a conexão e grava no PostgreSQL.
                // Isso evita crash nativo do fbclient.dll que ocorria ao ler muitos registros
                // numa única query (tabelas grandes: ExamesRealizados, ItensExamesRealizados).
                //
                // CommandTimeout = 0 (sem limite) — cada página é rápida (2500 registros).

                var fbBuilder = new FbConnectionStringBuilder(firebirdConnectionString);
                var odbcConnStr = $"Driver={{Firebird/InterBase(r) driver}};" +
                    $"Dbname={fbBuilder.DataSource}/{fbBuilder.Port}:{fbBuilder.Database};" +
                    $"Uid={fbBuilder.UserID};" +
                    $"Pwd={fbBuilder.Password};" +
                    $"Charset=NONE;";

                //Feito pelo Kiro em 26/07/2026
                // LEITURA PAGINADA ODBC (FIRST/SKIP) — evita crash nativo do fbclient.dll
                // em tabelas grandes. Cada página abre/fecha conexão ODBC fresca, lê um lote
                // de registros (usando FIRST N SKIP M do Firebird 2.5), fecha a conexão,
                // e grava no PostgreSQL. Isso isola o driver ODBC de acumulação de handles
                // que causava crash em ExamesRealizados/ItensExamesRealizados.

                var colunasFirebird = colunasMapeadas.Where(c => c.ColunaFirebird != null).ToList();

                //Feito pelo Kiro em 26/07/2026
                // Separa colunas BLOB das colunas normais.
                // Campos BLOB causam crash nativo no driver ODBC/fbclient.dll.
                // Serão importados numa passada separada via NETProvider (managed).
                var colunasBlob = colunasFirebird
                    .Where(c => c.ColunaFirebird!.Tipo.ToUpperInvariant().Contains("BLOB"))
                    .ToList();
                var colunasNaoBlob = colunasFirebird
                    .Where(c => !c.ColunaFirebird!.Tipo.ToUpperInvariant().Contains("BLOB"))
                    .ToList();
                //..Kiro

                // SELECT ODBC usa apenas colunas não-BLOB
                var colunasSelect = string.Join(", ", colunasNaoBlob.Select(c => $"\"{c.NomeFirebird}\""));

                // Determina coluna PK do Firebird para ORDER BY deterministico no SKIP.
                // Se a tabela tem coluna mapeada para "Id", usa-a como ORDER BY.
                var colunaPkFirebird = colunasFirebird.FirstOrDefault(c =>
                    c.NomePostgreSQL.Equals("Id", StringComparison.OrdinalIgnoreCase));
                var orderByClause = colunaPkFirebird != null
                    ? $" ORDER BY \"{colunaPkFirebird.NomeFirebird}\""
                    : "";

                //Feito pelo Kiro em 26/07/2026
                // Para a passada 1 (ODBC), usa apenas colunas não-BLOB
                var colunasMapeadasOdbc = colunasMapeadas
                    .Where(c => c.ColunaFirebird == null || !c.ColunaFirebird.Tipo.ToUpperInvariant().Contains("BLOB"))
                    .ToList();
                var colunasInsert = colunasMapeadasOdbc.Select(c => $"\"{c.NomePostgreSQL}\"").ToList();
                //..Kiro

                int numeroLote = 0;

                // Rastreia siglas ja utilizadas na tabela Instituicao para evitar duplicidade
                // quando o Firebird possuir Sequencial repetido.
                var siglasUtilizadasInstituicao = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Fallback IDs: quando o lookup texto -> ID falha (texto nao encontrado),
                // usa o menor ID disponivel para evitar violacao de FK. ID 0 nao existe
                // nas tabelas de referencia, entao 0 causaria SqlState 23503.
                var fallbackInstituicaoId = lookupInstituicao.Values.OrderBy(v => v).FirstOrDefault();
                if (fallbackInstituicaoId <= 0) fallbackInstituicaoId = 1;
                var fallbackTabelaExamesId = lookupTabelaExames.Values.OrderBy(v => v).FirstOrDefault();
                if (fallbackTabelaExamesId <= 0) fallbackTabelaExamesId = 1;

                // Deduplicacao de Id: algumas tabelas do Firebird tem PK composta (ex: CodigoExame + DataImpresso),
                // mas no PostgreSQL o Id é PK simples (auto-incremento). Quando o Firebird tem multiplos registros
                // com o mesmo CodigoExame (mapeado para Id), o segundo INSERT falha com 23505. Para resolver,
                // rastreia os IDs ja vistos e gera um novo Id via nextval da sequence para duplicatas.
                string? nomeSequenceId = null;
                var colunaIdAuto = colunasMapeadas.FirstOrDefault(c =>
                    c.NomePostgreSQL.Equals("Id", StringComparison.OrdinalIgnoreCase)
                    && c.ColunaPostgreSQL?.IsAutoIncrement == true);
                if (colunaIdAuto != null)
                {
                    try
                    {
                        using var cmdSeq = new NpgsqlCommand(
                            $@"SELECT pg_get_serial_sequence('""{tabelaPostgres}""', 'Id')", connPgExterna);
                        nomeSequenceId = (await cmdSeq.ExecuteScalarAsync(cancellationToken))?.ToString();
                    }
                    catch (Exception ex)
                    {
                        LogEmArquivo($"[ImportarTabelaAsync] AVISO: nao foi possivel obter sequence de Id para {tabelaPostgres}: {ex.Message}", erro: true);
                    }
                }
                var idsJaVistos = new HashSet<long>();
                bool logDiagnosticoEncodingFeito = false;

                // Loop paginado: cada página abre/fecha conexão ODBC fresca
                int tamanhoPageOdbc = tamanhoLote;
                long skip = 0;
                bool temMaisRegistros = true;

                LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: INÍCIO loop paginado. totalRegistros={totalRegistros}, tamanhoPage={tamanhoPageOdbc}", erro: true);

                while (temMaisRegistros && string.IsNullOrEmpty(resultado.MensagemErro))
                {
                    var loteAtualPage = new List<Dictionary<string, object?>>();

                    LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Antes de abrir ODBC. skip={skip}", erro: true);

                    // Abre conexão ODBC fresca para esta página
                    using (var connOdbcPage = new OdbcConnection(odbcConnStr))
                    {
                        try
                        {
                            await connOdbcPage.OpenAsync(cancellationToken);
                            LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: ODBC aberta com sucesso. skip={skip}", erro: true);
                        }
                        catch (OdbcException exOdbc)
                        {
                            // Tentativa de reconexão UMA vez antes de desistir
                            _eventLog.LogEventViewer($"[CargaDados] ODBC falhou ao abrir página (skip={skip}) para {tabelaFirebird}: {exOdbc.Message}. Tentando reconectar...", "wWarning");
                            await Task.Delay(500, cancellationToken);
                            try
                            {
                                await connOdbcPage.OpenAsync(cancellationToken);
                            }
                            catch (Exception exRetry)
                            {
                                resultado.MensagemErro = $"Falha ao abrir conexão ODBC para {tabelaFirebird} página skip={skip} (após retry): {exRetry.Message}";
                                resultado.Concluido = false;
                                break;
                            }
                        }

                        // Firebird 2.5: FIRST N SKIP M (antes das colunas do SELECT)
                        var sqlPage = $"SELECT FIRST {tamanhoPageOdbc} SKIP {skip} {colunasSelect} FROM \"{tabelaFirebird}\"{orderByClause}";
                        LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Antes ExecuteReader. SQL={sqlPage[..Math.Min(sqlPage.Length, 120)]}", erro: true);
                        using var cmdOdbcPage = new OdbcCommand(sqlPage, connOdbcPage);
                        cmdOdbcPage.CommandTimeout = 0;
                        using var readerFb = await cmdOdbcPage.ExecuteReaderAsync(cancellationToken);
                        LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: ExecuteReader OK. skip={skip}", erro: true);

                        int countPage = 0;
                        while (await readerFb.ReadAsync(cancellationToken))
                        {
                            countPage++;
                            if (!logDiagnosticoEncodingFeito)
                            {
                                logDiagnosticoEncodingFeito = true;
                                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                                var win1252 = Encoding.GetEncoding("Windows-1252");
                                var amostras = colunasMapeadasOdbc
                                    .Where(c => c.ColunaFirebird != null &&
                                                (c.ColunaFirebird.Tipo.ToUpperInvariant().Contains("VARCHAR") ||
                                                 c.ColunaFirebird.Tipo.ToUpperInvariant().Contains("CHAR")))
                                    .Take(3)
                                    .Select(c =>
                                    {
                                        var v = readerFb[c.NomeFirebird];
                                        var tipo = v?.GetType()?.Name ?? "NULL";
                                        var charset = c.ColunaFirebird?.Charset ?? "?";
                                        var amostra = v switch
                                        {
                                            byte[] b => $"[bytes:{b.Length}] {win1252.GetString(b.Take(40).ToArray())}",
                                            string s => s[..Math.Min(s.Length, 40)],
                                            _ => v?.ToString() ?? "NULL"
                                        };
                                        return $"{c.NomeFirebird}(charset={charset},tipo={tipo})='{amostra}'";
                                    });
                                _eventLog.LogEventViewer($"[CargaDados] Diagnóstico encoding {tabelaPostgres}: ODBC Charset=NONE paginado (FIRST/SKIP), amostras=[{string.Join(" | ", amostras)}]", "wInfo");
                            }

                            var registro = new Dictionary<string, object?>();
                            foreach (var coluna in colunasMapeadasOdbc)
                            {
                                object? valorConvertido;
                                string? aviso = null;

                                if (coluna.ColunaFirebird == null)
                                {
                                    if (tabelaPostgres.Equals("Instituicao", StringComparison.OrdinalIgnoreCase) &&
                                        DefaultsInstituicao.TryGetValue(coluna.NomePostgreSQL, out var valorInstituicao))
                                    {
                                        valorConvertido = valorInstituicao;
                                    }
                                    else
                                    {
                                        valorConvertido = ObterValorPadraoParaTipo(coluna.ColunaPostgreSQL?.Tipo, coluna.ColunaPostgreSQL?.Escala);
                                    }
                                    aviso = $"Coluna obrigatória ausente no Firebird preenchida com '{valorConvertido}'";
                                }
                                else
                                {
                                    var valorOrigem = readerFb[coluna.NomeFirebird];
                                    bool nullable = coluna.ColunaPostgreSQL?.Nullable ?? true;

                                    if (ColunasLookupTextoParaId.Contains(coluna.NomePostgreSQL))
                                    {
                                        string? strOrigLookup = valorOrigem?.ToString();
                                        if (!string.IsNullOrWhiteSpace(strOrigLookup)
                                            && !long.TryParse(strOrigLookup.Trim(), out _))
                                        {
                                            registro[coluna.NomePostgreSQL] = strOrigLookup.Trim();
                                            continue;
                                        }
                                    }

                                    valorConvertido = _typeConverter.Converter(
                                        valorOrigem,
                                        coluna.ColunaFirebird.Tipo,
                                        coluna.ColunaPostgreSQL?.Tipo ?? "VARCHAR",
                                        coluna.ColunaPostgreSQL?.Tamanho,
                                        out aviso,
                                        coluna.NomePostgreSQL,
                                        coluna.ColunaPostgreSQL?.ValorPadrao,
                                        nullable);
                                }

                                if (valorConvertido == null || valorConvertido == DBNull.Value)
                                {
                                    bool nullable = coluna.ColunaPostgreSQL?.Nullable ?? true;
                                    if (!nullable)
                                    {
                                        _eventLog.LogEventViewer($"[CargaDados] DIAGNOSTICO {tabelaPostgres}.{coluna.NomePostgreSQL}: tipoPg='{coluna.ColunaPostgreSQL?.Tipo}', nullable={nullable}, convertido=DBNull", "wWarning");
                                    }
                                    valorConvertido = ObterValorPadraoParaTipo(coluna.ColunaPostgreSQL?.Tipo, coluna.ColunaPostgreSQL?.Escala);
                                }

                                if (valorConvertido is string strValor)
                                {
                                    if (DeveSerMaiusculo(tabelaPostgres, coluna.NomePostgreSQL))
                                    {
                                        valorConvertido = strValor.ToUpperInvariant();
                                    }
                                }

                                registro[coluna.NomePostgreSQL] = valorConvertido;
                            }

                            // Filtra registros filhos de ClasseExames cujo CodigoItem/ClasseExamesId
                            // nao existe na tabela pai. Evita importar dados orfaos.
                            if (idsClasseExamesValidos != null && idsClasseExamesValidos.Count > 0)
                            {
                                var colunasFkClasseExames = dependenciasFk
                                    .Where(d => d.TabelaPai.Equals("ClasseExames", StringComparison.OrdinalIgnoreCase)
                                             && d.TabelaFilha.Equals(tabelaPostgres, StringComparison.OrdinalIgnoreCase))
                                    .Select(d => d.ColunaFilha)
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                                foreach (var colunaFk in colunasFkClasseExames)
                                {
                                    if (registro.TryGetValue(colunaFk, out var valorFk)
                                        && valorFk != null
                                        && valorFk != DBNull.Value
                                        && long.TryParse(valorFk.ToString(), out var idFk)
                                        && idFk > 0
                                        && !idsClasseExamesValidos.Contains(idFk))
                                    {
                                        resultado.Ignorados++;
                                        registro.Clear();
                                        break;
                                    }
                                }
                            }

                            if (registro.Count == 0)
                            {
                                continue;
                            }

                            // Pós-processamento especial para Instituicao: a coluna Sigla possui índice único
                            // e não existe no Firebird. Se ficar vazia, todos os registros falhariam com 23505.
                            if (tabelaPostgres.Equals("Instituicao", StringComparison.OrdinalIgnoreCase))
                            {
                                bool siglaVazia = !registro.TryGetValue("Sigla", out var siglaValor)
                                    || siglaValor == null
                                    || siglaValor == DBNull.Value
                                    || (siglaValor is string s && string.IsNullOrWhiteSpace(s));

                                if (siglaVazia)
                                {
                                    string? baseSigla = null;
                                    if (registro.TryGetValue("Sequencial", out var seqValor)
                                        && seqValor != null
                                        && seqValor != DBNull.Value
                                        && !string.IsNullOrWhiteSpace(seqValor.ToString())
                                        && seqValor.ToString() != "0")
                                    {
                                        baseSigla = seqValor.ToString()!;
                                    }
                                    else if (registro.TryGetValue("Nome", out var nomeValor)
                                        && nomeValor is string nomeStr
                                        && !string.IsNullOrWhiteSpace(nomeStr))
                                    {
                                        var nomeLimpo = LimparStringPostgreSQL(nomeStr, 20);
                                        baseSigla = nomeLimpo.Length > 20 ? nomeLimpo.Substring(0, 20) : nomeLimpo;
                                    }

                                    if (!string.IsNullOrWhiteSpace(baseSigla))
                                    {
                                        var siglaFinal = GarantirSiglaUnica(baseSigla, siglasUtilizadasInstituicao, 20);
                                        registro["Sigla"] = siglaFinal;
                                    }
                                }
                            }

                            // Pós-processamento para tabelas filhas: converte InstituicaoId texto -> ID numerico.
                            if (!tabelaPostgres.Equals("Instituicao", StringComparison.OrdinalIgnoreCase)
                                && registro.ContainsKey("InstituicaoId"))
                            {
                                var valInst = registro["InstituicaoId"];
                                if (valInst != null && valInst != DBNull.Value)
                                {
                                    if (valInst is string textoInst && !string.IsNullOrWhiteSpace(textoInst))
                                    {
                                        var textoTrim = textoInst.Trim();
                                        if (lookupInstituicao.TryGetValue(textoTrim, out var idInstituicao))
                                        {
                                            registro["InstituicaoId"] = idInstituicao;
                                        }
                                        else if (int.TryParse(textoTrim, out var idDireto))
                                        {
                                            registro["InstituicaoId"] = idDireto;
                                        }
                                        else
                                        {
                                            registro["InstituicaoId"] = fallbackInstituicaoId;
                                        }
                                    }
                                }
                            }

                            // Pós-processamento para TabelaExamesId: converte texto (SiglaTabela) -> ID numerico.
                            if (registro.ContainsKey("TabelaExamesId"))
                            {
                                var valTab = registro["TabelaExamesId"];
                                if (valTab != null && valTab != DBNull.Value)
                                {
                                    if (valTab is string textoTab && !string.IsNullOrWhiteSpace(textoTab))
                                    {
                                        var textoTrimTab = textoTab.Trim();
                                        if (lookupTabelaExames.TryGetValue(textoTrimTab, out var idTabela))
                                        {
                                            registro["TabelaExamesId"] = idTabela;
                                        }
                                        else if (int.TryParse(textoTrimTab, out var idDiretoTab))
                                        {
                                            registro["TabelaExamesId"] = idDiretoTab;
                                        }
                                        else
                                        {
                                            registro["TabelaExamesId"] = fallbackTabelaExamesId;
                                        }
                                    }
                                }
                            }

                            //Feito pelo Kiro em 01/08/2026
                            // Pós-processamento para PlanoExames: deriva ClasseExamesId da ContaExame.
                            // ContaExame formato: TTFFCCCNNNN (11 dígitos). FF (posições 3-4) = Id da Folha = ClasseExames.Id.
                            // No Firebird não existe coluna separada para a folha — é derivada da ContaExame.
                            if (tabelaPostgres.Equals("PlanoExames", StringComparison.OrdinalIgnoreCase)
                                && registro.TryGetValue("ContaExame", out var contaVal)
                                && contaVal is string contaStr
                                && contaStr.Length >= 4)
                            {
                                if (int.TryParse(contaStr.Substring(2, 2), out var folhaId) && folhaId > 0)
                                {
                                    registro["ClasseExamesId"] = folhaId;
                                }
                            }
                            //..Kiro

                            // Deduplica Id: se o Firebird tem PK composta mas o PostgreSQL tem Id simples,
                            // registros com o mesmo CodigoExame (Id) recebem um novo Id gerado pela sequence.
                            if (nomeSequenceId != null && registro.TryGetValue("Id", out var idVal) && idVal != null && idVal != DBNull.Value)
                            {
                                var idLong = Convert.ToInt64(idVal);
                                if (!idsJaVistos.Add(idLong))
                                {
                                    using var cmdNextVal = new NpgsqlCommand($"SELECT nextval('{nomeSequenceId}')", connPgExterna);
                                    var novoId = Convert.ToInt64(await cmdNextVal.ExecuteScalarAsync(cancellationToken));
                                    registro["Id"] = novoId;
                                    idsJaVistos.Add(novoId);
                                }
                            }

                            loteAtualPage.Add(registro);
                        }
                        // Fim do while (readerFb.ReadAsync)
                        LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Leitura página completa. countPage={countPage}, skip={skip}", erro: true);

                        if (countPage < tamanhoPageOdbc)
                            temMaisRegistros = false;
                    }
                    // Conexão ODBC FECHADA (fim do using connOdbcPage)

                    // Gravar lote da página no PostgreSQL
                    if (loteAtualPage.Count > 0 && string.IsNullOrEmpty(resultado.MensagemErro))
                    {
                        numeroLote++;

                        //Feito pelo Kiro em 26/07/2026
                        // Verifica se a conexão PostgreSQL ainda está aberta antes de gravar.
                        // Se morreu (ex: timeout TCP, idle), tenta reabrir.
                        if (connPgExterna.State != System.Data.ConnectionState.Open)
                        {
                            LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Conexão PG morta (State={connPgExterna.State}). Tentando reabrir...", erro: true);
                            try
                            {
                                await connPgExterna.CloseAsync();
                                await connPgExterna.OpenAsync(cancellationToken);
                                await using (var cmdReconfig = new NpgsqlCommand(
                                    "SET idle_in_transaction_session_timeout = 0; SET statement_timeout = 0;", connPgExterna))
                                {
                                    await cmdReconfig.ExecuteNonQueryAsync(cancellationToken);
                                }
                                LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Conexão PG reaberta com sucesso.", erro: true);
                            }
                            catch (Exception exReopen)
                            {
                                resultado.MensagemErro = $"Conexão PostgreSQL perdida e não foi possível reabrir: {exReopen.Message}";
                                resultado.Concluido = false;
                                LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: FALHA ao reabrir PG: {exReopen.Message}", erro: true);
                                break;
                            }
                        }
                        //..Kiro

                        LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Antes BeginTransaction PG. lote={numeroLote}, registros={loteAtualPage.Count}", erro: true);
                        await using var transactionLote = await connPgExterna.BeginTransactionAsync(cancellationToken);

                        LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Antes ProcessarLoteAsync. lote={numeroLote}", erro: true);
                        var resultadoLote = await ProcessarLoteAsync(
                            tabelaPostgres,
                            tabelaFirebird,
                            colunasInsert,
                            colunasMapeadasOdbc,
                            loteAtualPage,
                            modoSimulacao,
                            cancellationToken,
                            numeroLote,
                            connPgExterna,
                            transactionLote,
                            connectionId);

                        if (resultadoLote.Erros > 0 && !ignorarErros)
                        {
                            //Feito pelo Kiro em 26/07/2026
                            // Se a conexão caiu durante o lote (CONNECTION_LOST), reconecta e retenta.
                            if (resultadoLote.MensagemErro == "CONNECTION_LOST")
                            {
                                try { await transactionLote.RollbackAsync(cancellationToken); } catch { }
                                LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: CONNECTION_LOST detectado. Reconectando PG para retry do lote {numeroLote}...", erro: true);

                                try
                                {
                                    await connPgExterna.CloseAsync();
                                    await connPgExterna.OpenAsync(cancellationToken);
                                    await using (var cmdReconfig2 = new NpgsqlCommand(
                                        "SET idle_in_transaction_session_timeout = 0; SET statement_timeout = 0;", connPgExterna))
                                    {
                                        await cmdReconfig2.ExecuteNonQueryAsync(cancellationToken);
                                    }

                                    // Retry: nova transação com o mesmo lote
                                    await using var transactionRetry = await connPgExterna.BeginTransactionAsync(cancellationToken);
                                    var resultadoRetry = await ProcessarLoteAsync(
                                        tabelaPostgres, tabelaFirebird, colunasInsert, colunasMapeadasOdbc,
                                        loteAtualPage, modoSimulacao, cancellationToken, numeroLote,
                                        connPgExterna, transactionRetry, connectionId);

                                    await transactionRetry.CommitAsync(cancellationToken);
                                    LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Retry OK. lote={numeroLote}, inseridos={resultadoRetry.Inseridos}, erros={resultadoRetry.Erros}", erro: true);

                                    resultado.Inseridos += resultadoRetry.Inseridos;
                                    resultado.Duplicados += resultadoRetry.Duplicados;
                                    resultado.Erros += resultadoRetry.Erros;
                                    resultado.Ignorados += resultadoRetry.Ignorados;
                                    resultado.DetalhesErros.AddRange(resultadoRetry.DetalhesErros);
                                }
                                catch (Exception exRetry)
                                {
                                    resultado.MensagemErro = $"Falha no retry do lote {numeroLote}: {exRetry.Message}";
                                    resultado.Concluido = false;
                                    LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Retry FALHOU: {exRetry.Message}", erro: true);
                                }
                            }
                            else
                            {
                                await transactionLote.RollbackAsync(cancellationToken);
                                resultado.MensagemErro = $"Erro no lote {numeroLote} da tabela {tabelaFirebird}: {resultadoLote.MensagemErro}";
                                resultado.Erros += resultadoLote.Erros;
                                resultado.DetalhesErros.AddRange(resultadoLote.DetalhesErros);
                            }
                            //..Kiro
                        }
                        else
                        {
                            await transactionLote.CommitAsync(cancellationToken);
                            LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Commit OK. lote={numeroLote}, inseridos={resultadoLote.Inseridos}, erros={resultadoLote.Erros}", erro: true);

                            resultado.Inseridos += resultadoLote.Inseridos;
                            resultado.Duplicados += resultadoLote.Duplicados;
                            resultado.Erros += resultadoLote.Erros;
                            resultado.Ignorados += resultadoLote.Ignorados;
                            resultado.DetalhesErros.AddRange(resultadoLote.DetalhesErros);
                        }
                    }

                    skip += tamanhoPageOdbc;
                    registrosProcessados += loteAtualPage.Count;

                    // Envia progresso após cada página processada
                    await EnviarProgresso(connectionId, tabelaPostgres, registrosProcessados, totalRegistros,
                        $"Processando lote {numeroLote} ({registrosProcessados:N0} de {totalRegistros:N0})",
                        totalTabelas, tabelasConcluidas, "Importação", cancellationToken);
                }
                // Fim do loop paginado
                //..Kiro

                //Feito pelo Kiro em 26/07/2026
                // Passada 2: importar campos BLOB via NETProvider (managed, sem crash).
                // Apenas se a tabela tem colunas BLOB E registros foram importados na passada 1.
                if (colunasBlob.Count > 0 && resultado.Inseridos > 0 && string.IsNullOrEmpty(resultado.MensagemErro))
                {
                    LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: INÍCIO passada 2 (BLOBs). Colunas={string.Join(",", colunasBlob.Select(c => c.NomeFirebird))}", erro: true);

                    try
                    {
                        using var connFbBlob = new FbConnection(firebirdConnectionString);
                        await connFbBlob.OpenAsync(cancellationToken);

                        // Identifica a coluna PK para o JOIN entre Firebird e PostgreSQL
                        var colunaPkBlob = colunasFirebird.FirstOrDefault(c =>
                            c.NomePostgreSQL.Equals("Id", StringComparison.OrdinalIgnoreCase));

                        if (colunaPkBlob != null)
                        {
                            // Lê PK + BLOBs do Firebird
                            var blobColsSelect = string.Join(", ",
                                new[] { $"\"{colunaPkBlob.NomeFirebird}\"" }
                                .Concat(colunasBlob.Select(c => $"\"{c.NomeFirebird}\"")));

                            var sqlBlob = $"SELECT {blobColsSelect} FROM \"{tabelaFirebird}\"";
                            using var cmdBlob = new FbCommand(sqlBlob, connFbBlob);
                            cmdBlob.CommandTimeout = 300;
                            using var readerBlob = await cmdBlob.ExecuteReaderAsync(cancellationToken);

                            int blobsAtualizados = 0;
                            while (await readerBlob.ReadAsync(cancellationToken))
                            {
                                var pkValue = readerBlob[colunaPkBlob.NomeFirebird];
                                if (pkValue == null || pkValue == DBNull.Value) continue;

                                // Monta UPDATE para cada registro
                                var setClauses = new List<string>();
                                var parametros = new List<NpgsqlParameter>();
                                int paramIdx = 0;

                                foreach (var colBlob in colunasBlob)
                                {
                                    var valorBlob = readerBlob[colBlob.NomeFirebird];

                                    // Converte via TypeConverter (trata BLOB SUB_TYPE TEXT vs binário)
                                    var valorConvertido = _typeConverter.Converter(
                                        valorBlob,
                                        colBlob.ColunaFirebird!.Tipo,
                                        colBlob.ColunaPostgreSQL?.Tipo ?? "BYTEA",
                                        colBlob.ColunaPostgreSQL?.Tamanho,
                                        out _,
                                        colBlob.NomePostgreSQL,
                                        colBlob.ColunaPostgreSQL?.ValorPadrao,
                                        colBlob.ColunaPostgreSQL?.Nullable ?? true);

                                    if (valorConvertido != null && valorConvertido != DBNull.Value)
                                    {
                                        setClauses.Add($"\"{colBlob.NomePostgreSQL}\" = @bp{paramIdx}");
                                        parametros.Add(new NpgsqlParameter($"bp{paramIdx}", valorConvertido));
                                        paramIdx++;
                                    }
                                }

                                if (setClauses.Count > 0)
                                {
                                    var sqlUpdate = $"UPDATE \"{tabelaPostgres}\" SET {string.Join(", ", setClauses)} WHERE \"Id\" = @bpId";
                                    parametros.Add(new NpgsqlParameter("bpId", Convert.ToInt64(pkValue)));

                                    using var cmdUpdate = new NpgsqlCommand(sqlUpdate, connPgExterna);
                                    foreach (var p in parametros)
                                        cmdUpdate.Parameters.Add(p);

                                    await cmdUpdate.ExecuteNonQueryAsync(cancellationToken);
                                    blobsAtualizados++;
                                }
                            }

                            LogEmArquivo($"[DIAG-CRASH] {tabelaFirebird}: Passada 2 concluída. BLOBs atualizados={blobsAtualizados}", erro: true);
                        }
                    }
                    catch (Exception exBlob)
                    {
                        // BLOBs são opcionais — se falhar, loga mas não interrompe
                        var msgBlob = $"Aviso: falha ao importar BLOBs de {tabelaFirebird}: {exBlob.Message}";
                        LogEmArquivo($"[CargaDados] {msgBlob}", erro: true);
                        _eventLog.LogEventViewer($"[CargaDados] {msgBlob}", "wWarning");
                    }
                }
                //..Kiro

                // Atualiza as sequencias (IDENTITY/SERIAL) do PostgreSQL para o maior Id importado,
                // evitando que o PostgreSQL tente reutilizar IDs ja ocupados pelos dados do Firebird.
                if (!modoSimulacao && string.IsNullOrEmpty(resultado.MensagemErro))
                {
                    try
                    {
                        await AtualizarSequenciaPosImportacaoAsync(connPgExterna, tabelaPostgres, cancellationToken);
                    }
                    catch (Exception exSeq)
                    {
                        LogEmArquivo($"[CargaDados] AVISO: nao foi possivel atualizar a sequencia de {tabelaPostgres} pos-importacao: {exSeq.Message}", erro: true);
                    }
                }

                resultado.TotalLido = totalRegistros;
                stopwatch.Stop();
                resultado.TempoGasto = stopwatch.Elapsed.TotalSeconds;
                resultado.Concluido = string.IsNullOrEmpty(resultado.MensagemErro);
                resultado.Observacao = GerarObservacaoTabela(tabelaPostgres, resultado);

                await EnviarProgresso(connectionId, tabelaPostgres, registrosProcessados, totalRegistros, resultado.Concluido ? "Concluído" : "Interrompido", totalTabelas, tabelasConcluidas + 1, "Importação", cancellationToken);
            }
            catch (Exception ex)
            {
                var mensagemTela = FormatarMensagemErroTela(ex);
                var detalhado = $"{ex.GetType().Name}: {ex.Message} | Inner: {ex.InnerException?.Message} | StackTrace: {ex.StackTrace}";
                resultado.MensagemErro = mensagemTela;
                resultado.Concluido = false;
                resultado.Observacao = GerarObservacaoTabela(tabelaPostgres, resultado);
                LogEmArquivo($"[CargaDados] ERRO ao importar {tabelaFirebird}: {detalhado}", erro: true);
                _eventLog.LogEventViewer($"[CargaDados] Erro ao importar {tabelaFirebird}: {detalhado}", "wError");
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceberErro", new { Tabela = tabelaPostgres, Erro = mensagemTela }, cancellationToken);
            }

            return resultado;
        }

        /// <summary>
        /// Gera uma observação descritiva e dinâmica sobre o resultado da importação,
        /// baseada exclusivamente nos erros reais encontrados durante o processo.
        /// Não utiliza valores fixos — reflete a realidade de cada ambiente.
        /// </summary>
        private string? GerarObservacaoTabela(string tabelaPostgres, ImportacaoResultadoViewModel resultado)
        {
            var partes = new List<string>();

            // Agrupa erros por SqlState para descrever causas reais
            if (resultado.DetalhesErros != null && resultado.DetalhesErros.Count > 0)
            {
                var errosPorTipo = resultado.DetalhesErros
                    .GroupBy(e => e.SqlState ?? "?")
                    .OrderByDescending(g => g.Count());

                foreach (var grupo in errosPorTipo)
                {
                    var sqlState = grupo.Key;
                    var qtd = grupo.Count();
                    var tipoErro = sqlState switch
                    {
                        "23503" => "violação de chave estrangeira (FK inexistente)",
                        "23505" => "violação de chave única (registro duplicado)",
                        "23502" => "campo obrigatório nulo (NOT NULL)",
                        "22001" => "valor muito longo para a coluna",
                        "22018" => "erro de conversão de tipo",
                        "22P02" => "sintaxe inválida para o tipo",
                        _ => $"erro SQL {sqlState}"
                    };
                    partes.Add($"{qtd} registro(s) rejeitado(s) por {tipoErro}.");
                }
            }

            // Percentual importado e situação geral
            if (resultado.TotalLido > 0 && resultado.Inseridos < resultado.TotalLido)
            {
                var pct = (resultado.Inseridos * 100.0 / resultado.TotalLido).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(resultado.MensagemErro))
                    partes.Add($"Importação interrompida: {pct}% importado ({resultado.Inseridos} de {resultado.TotalLido}).");
                else if (resultado.Erros > 0)
                    partes.Add($"{pct}% importado ({resultado.Inseridos} de {resultado.TotalLido}).");
            }
            else if (resultado.Erros > 0 && resultado.Inseridos == 0 && resultado.TotalLido > 0)
            {
                partes.Add($"Nenhum registro importado de {resultado.TotalLido} lidos.");
            }
            else if (resultado.TotalLido > 0 && resultado.Inseridos == resultado.TotalLido && resultado.Erros == 0)
            {
                partes.Add("Importação completa sem erros.");
            }
            else if (resultado.TotalLido == 0)
            {
                partes.Add("Tabela vazia no Firebird — nenhum registro para importar.");
            }

            return partes.Count > 0 ? string.Join(" ", partes) : null;
        }

        private async Task<(long Inseridos, long Duplicados, long Erros, long Ignorados, string? MensagemErro, List<ErroRegistroViewModel> DetalhesErros)> ProcessarLoteAsync(
            string tabelaPostgres,
            string tabelaFirebird,
            List<string> colunasInsert,
            List<MapeamentoColunas> colunasMapeadas,
            List<Dictionary<string, object?>> registros,
            bool modoSimulacao,
            CancellationToken cancellationToken,
            int numeroLote,
            NpgsqlConnection connPg,
            NpgsqlTransaction transaction,
            string? connectionId = null)
        {
            long inseridos = 0;
            long jaExistentes = 0;
            long erros = 0;
            long ignorados = 0;
            string? mensagemErro = null;
            var detalhesErros = new List<ErroRegistroViewModel>();

            if (modoSimulacao)
            {
                return (registros.Count, 0, 0, 0, null, detalhesErros);
            }

            var parametrosInsert = colunasInsert.Select((c, i) => $"@p{i}").ToList();

            // Verifica se a tabela possui coluna GENERATED ALWAYS AS IDENTITY.
            // Nesse caso, o INSERT de valor explícito na PK exige OVERRIDING SYSTEM VALUE.
            bool temIdentityAlways = colunasMapeadas.Any(c => c.ColunaPostgreSQL?.IdentityType == "a");
            var overridingClause = temIdentityAlways ? " OVERRIDING SYSTEM VALUE" : "";
            var sqlInsert = $"INSERT INTO \"{tabelaPostgres}\"{overridingClause} ({string.Join(", ", colunasInsert)}) VALUES ({string.Join(", ", parametrosInsert)})";

            for (int idxRegistro = 0; idxRegistro < registros.Count; idxRegistro++)
            {
                var registro = registros[idxRegistro];
                string savepointName = $"sp_reg_{idxRegistro}";
                try
                {
                    var parametrosComando = new List<NpgsqlParameter>();
                    int idx = 0;
                    foreach (var coluna in colunasMapeadas)
                    {
                        var valor = registro[coluna.NomePostgreSQL];

                        // Defesa: limpa strings removendo bytes nulos/invalidos e truncando ao tamanho da coluna.
                        if (valor is string strValor)
                        {
                            var tamanhoMax = coluna.ColunaPostgreSQL?.Tamanho;
                            valor = LimparStringPostgreSQL(strValor, tamanhoMax);
                        }

                        if (valor == null || valor == DBNull.Value)
                        {
                            parametrosComando.Add(new NpgsqlParameter($"p{idx}", DBNull.Value));
                        }
                        else
                        {
                            parametrosComando.Add(new NpgsqlParameter($"p{idx}", valor));
                        }
                        idx++;
                    }

                    // Cria um savepoint para isolar o INSERT deste registro.
                    await CriarSavepointAsync(connPg, transaction, savepointName, cancellationToken);

                    var contexto = new ContextoOperacao(tabelaPostgres, "INSERT", numeroLote, idxRegistro + 1);
                    await ExecutarComandoComLogAsync(connPg, transaction, sqlInsert, parametrosComando, contexto, cancellationToken, logarOperacao: false, logarSucesso: false);
                    inseridos++;
                }
                catch (Exception ex)
                {
                    // Qualquer erro de banco (FK, NOT NULL, tipo invalido, duplicidade, etc.) isola o registro,
                    // loga o problema e continua com os demais registros.
                    erros++;

                    //Feito pelo Kiro em 26/07/2026
                    // Se a conexão PostgreSQL caiu ("Connection is not open"), reconecta e
                    // retenta o lote inteiro. Os registros estão em memória (loteAtualPage),
                    // basta abrir nova transação e inserir novamente.
                    if (ex is InvalidOperationException && ex.Message.Contains("not open", StringComparison.OrdinalIgnoreCase))
                    {
                        LogEmArquivo($"[CargaDados] Conexão PG caiu no registro {idxRegistro + 1} do lote {numeroLote} em {tabelaPostgres}. Interrompendo lote para retry.", erro: true);
                        mensagemErro = "CONNECTION_LOST";
                        break;
                    }
                    //..Kiro

                    await RollbackToSavepointAsync(connPg, transaction, savepointName, cancellationToken);

                    var sqlState = ex is PostgresException pgExErro ? pgExErro.SqlState : null;
                    var chaveRegistro = ObterChaveRegistro(registro, colunasMapeadas);
                    var motivo = ClassificarMotivoErro(sqlState, ex.Message);

                    var erroDetalhado = new ErroRegistroViewModel
                    {
                        Tabela = tabelaPostgres,
                        Chave = chaveRegistro,
                        SqlState = sqlState,
                        Motivo = motivo
                    };
                    detalhesErros.Add(erroDetalhado);

                    string logMsgErro = $"[CargaDados] Registro ignorado em {tabelaPostgres} (Firebird: {tabelaFirebird}) | Chave={chaveRegistro} | SQLState={sqlState} | Motivo={motivo}";
                    try { _eventLog.LogEventViewer(logMsgErro, "wWarning"); } catch { }
                    LogEmArquivo(logMsgErro, erro: true);

                    if (ex is PostgresException pgExString && (pgExString.SqlState == "22001" || pgExString.SqlState == "22021"))
                    {
                        var detalhes = string.Join(" | ", colunasMapeadas
                            .Where(c => c.ColunaPostgreSQL?.Tipo.Contains("CHAR") == true || c.ColunaPostgreSQL?.Tipo.Contains("VARCHAR") == true || c.ColunaPostgreSQL?.Tipo.Contains("TEXT") == true)
                            .Select(c =>
                            {
                                registro.TryGetValue(c.NomePostgreSQL, out var v);
                                var txt = v?.ToString() ?? "NULL";
                                return $"{c.NomePostgreSQL}({c.ColunaPostgreSQL?.Tipo}:{c.ColunaPostgreSQL?.Tamanho})='{txt}'";
                            }));
                        string logMsgDiag = $"[CargaDados] DIAGNOSTICO {pgExString.SqlState} {tabelaPostgres}: {pgExString.Message} :: {detalhes}";
                        try { _eventLog.LogEventViewer(logMsgDiag, "wError"); } catch { }
                        LogEmArquivo(logMsgDiag, erro: true);
                    }

                    continue;
                }
            }

            return (inseridos, jaExistentes, erros, ignorados, mensagemErro, detalhesErros);
        }

        private static async Task CriarSavepointAsync(NpgsqlConnection connPg, NpgsqlTransaction transaction, string savepointName, CancellationToken cancellationToken)
        {
            try
            {
                using var cmd = new NpgsqlCommand($"SAVEPOINT {savepointName}", connPg, transaction);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[CriarSavepointAsync] AVISO: nao foi possivel criar savepoint {savepointName}: {ex.Message}", erro: true);
            }
        }

        private static async Task RollbackToSavepointAsync(NpgsqlConnection connPg, NpgsqlTransaction transaction, string savepointName, CancellationToken cancellationToken)
        {
            try
            {
                using var cmd = new NpgsqlCommand($"ROLLBACK TO SAVEPOINT {savepointName}", connPg, transaction);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[RollbackToSavepointAsync] AVISO: nao foi possivel fazer rollback to savepoint {savepointName}: {ex.Message}", erro: true);
            }
        }

        private static string? ObterChaveRegistro(Dictionary<string, object?> registro, List<MapeamentoColunas> colunasMapeadas)
        {
            // Tenta identificar a PK (coluna "Id") ou a primeira coluna nao nula como referencia.
            var nomesCandidatos = new[] { "Id", "ID", "Codigo", "CodigoInterno", "Sequencial" };
            foreach (var nome in nomesCandidatos)
            {
                if (registro.TryGetValue(nome, out var valor) && valor != null && valor != DBNull.Value)
                    return valor.ToString();
            }

            foreach (var coluna in colunasMapeadas)
            {
                if (registro.TryGetValue(coluna.NomePostgreSQL, out var valor) && valor != null && valor != DBNull.Value)
                    return $"{coluna.NomePostgreSQL}={valor}";
            }

            return null;
        }

        private static string ClassificarMotivoErro(string? sqlState, string mensagemErro)
        {
            if (string.IsNullOrEmpty(sqlState))
                return $"Erro nao classificado: {mensagemErro}";

            return sqlState switch
            {
                "23502" => "Valor nulo em coluna obrigatoria (NOT NULL)",
                "23503" => "Violacao de chave estrangeira (FK inexistente)",
                "23505" => "Violacao de chave unica (registro duplicado)",
                "22001" => "Valor string excede o tamanho maximo da coluna",
                "22003" => "Valor numerico fora da faixa permitida",
                "22007" => "Formato de data/hora invalido",
                "22021" => "Sequencia de caracteres invalida",
                "22P02" => "Sintaxe de entrada invalida para o tipo",
                "42804" => "Tipo de dado incompativel",
                _ => $"Erro PostgreSQL ({sqlState}): {mensagemErro}"
            };
        }

        //Feito pelo Kiro em 26/07/2026
        /// <summary>
        /// Lê o valor de uma coluna Firebird do reader, forçando leitura binária (byte[])
        /// para colunas VARCHAR/CHAR com charset ASCII ou NONE.
        /// O driver FirebirdSql.Data.FirebirdClient com charset de conexão NONE usa o charset
        /// declarado da coluna para decodificar strings. Para colunas ASCII, bytes > 0x7F são
        /// descartados. A leitura binária via GetBytes() preserva todos os bytes, permitindo
        /// que o TypeConverter.ConverterBytesWin1252ParaString() faça a conversão correta.
        /// </summary>
        private static object? LerValorFirebird(DbDataReader reader, MapeamentoColunas coluna)
        {
            var ordinal = reader.GetOrdinal(coluna.NomeFirebird);

            if (reader.IsDBNull(ordinal))
                return DBNull.Value;

            var tipoColuna = coluna.ColunaFirebird?.Tipo?.ToUpperInvariant() ?? string.Empty;
            var charsetColuna = coluna.ColunaFirebird?.Charset?.ToUpperInvariant()?.Trim() ?? string.Empty;

            // Força leitura binária apenas para colunas de texto com charset ASCII ou NONE,
            // onde o driver descartaria bytes > 0x7F ao decodificar como string.
            // Colunas WIN1252 e ISO8859_1 já são lidas corretamente pelo driver com CAST OCTETS
            // (aplicado no SELECT) ou retornam bytes brutos que o TypeConverter trata.
            bool forcarLeituraBinaria = (tipoColuna == "VARCHAR" || tipoColuna == "CHAR")
                && (charsetColuna == "ASCII" || charsetColuna == "NONE" || string.IsNullOrEmpty(charsetColuna));

            if (!forcarLeituraBinaria)
                return reader[coluna.NomeFirebird];

            // Leitura binária: obtém os bytes brutos sem decodificação pelo driver.
            // O FbDataReader.GetBytes() lê o campo como bytes brutos.
            try
            {
                // Primeiro, obtém o tamanho do campo
                long tamanho = reader.GetBytes(ordinal, 0, null, 0, 0);
                if (tamanho <= 0)
                    return string.Empty;

                var buffer = new byte[tamanho];
                reader.GetBytes(ordinal, 0, buffer, 0, (int)tamanho);

                // Retorna como byte[] — o TypeConverter.ConverterParaString() já trata byte[]
                // via ConverterBytesWin1252ParaString()
                return buffer;
            }
            catch
            {
                // Fallback: se GetBytes() falhar (campo não suporta leitura binária),
                // usa a leitura padrão do driver. Neste caso, o SanitizarStringWin1252
                // tentará recuperar o que for possível da string já decodificada.
                return reader[coluna.NomeFirebird];
            }
        }
        //..Kiro

        private static object? ObterValorPadraoParaTipo(string? tipoPostgreSQL, int? escala = null)
        {
            if (string.IsNullOrEmpty(tipoPostgreSQL))
                return DBNull.Value;

            var pg = tipoPostgreSQL.ToUpperInvariant();

            if (pg.Contains("INT") || pg == "SMALLINT" || pg == "BIGINT" || pg.Contains("SERIAL"))
                return 0;

            if (pg.Contains("NUMERIC") || pg.Contains("DECIMAL") || pg.Contains("DOUBLE") || pg.Contains("REAL") || pg.Contains("FLOAT"))
                return escala.HasValue && escala.Value > 0 ? 0.00m : 0m;

            if (pg.Contains("TIMESTAMP"))
                return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).ToUniversalTime();

            if (pg.Contains("DATE") && !pg.Contains("TIMESTAMP"))
                return new DateTime(1900, 1, 1);

            if (pg.Contains("TIME") && !pg.Contains("TIMESTAMP"))
                return TimeSpan.Zero;

            if (pg.Contains("BOOL"))
                return false;

            if (pg.Contains("VARCHAR") || pg.Contains("CHAR") || pg.Contains("TEXT"))
                return string.Empty;

            if (pg.Contains("BYTEA"))
                return Array.Empty<byte>();

            return DBNull.Value;
        }

        private static bool DeveSerMaiusculo(string tabelaPostgres, string nomeColuna)
        {
            if (string.IsNullOrWhiteSpace(nomeColuna))
                return false;

            // Algumas tabelas preservam a formatação original dos textos.
            var tabela = tabelaPostgres.ToUpperInvariant();
            if (tabela == "REQUISITAR"
                || tabela == "PLANOEXAMES"
                || tabela == "ITENSEXAMESREALIZADOS"
                || tabela == "ITENSEXAMESREALIZADOSAM")
            {
                return false;
            }

            var coluna = nomeColuna.ToUpperInvariant();
            return coluna.Contains("SIGLA")
                || coluna.Contains("NOME")
                || coluna.Contains("DESCRICAO")
                || coluna.Contains("DESCRICÃO")
                || coluna.Contains("TITULO")
                || coluna.Contains("TÍTULO")
                || coluna.Contains("SUBTITULO")
                || coluna.Contains("SUBTÍTULO");
        }

        private static string GarantirSiglaUnica(string baseSigla, HashSet<string> siglasUtilizadas, int tamanhoMaximo)
        {
            var sigla = baseSigla.Length > tamanhoMaximo ? baseSigla.Substring(0, tamanhoMaximo) : baseSigla;
            if (!siglasUtilizadas.Contains(sigla))
            {
                siglasUtilizadas.Add(sigla);
                return sigla;
            }

            // Adiciona sufixo numérico enquanto houver duplicidade.
            for (int i = 2; i < int.MaxValue; i++)
            {
                var sufixo = $"_{i}";
                var disponivel = tamanhoMaximo - sufixo.Length;
                if (disponivel <= 0)
                {
                    // Caso extremo: retorna GUID encurtado.
                    return Guid.NewGuid().ToString("N")[..Math.Min(tamanhoMaximo, 32)];
                }

                var prefixo = baseSigla.Length > disponivel ? baseSigla.Substring(0, disponivel) : baseSigla;
                var candidata = prefixo + sufixo;
                if (!siglasUtilizadas.Contains(candidata))
                {
                    siglasUtilizadas.Add(candidata);
                    return candidata;
                }
            }

            return sigla;
        }

        private static string LimparStringPostgreSQL(string valor, int? tamanhoMaximo)
        {
            // Remove bytes nulos e caracteres de controle invalidos, preservando tab, nova linha e retorno.
            var sb = new System.Text.StringBuilder(valor.Length);
            foreach (char c in valor)
            {
                if (c == '\0')
                    continue;
                if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                    continue;
                sb.Append(c);
            }

            var resultado = sb.ToString();

            if (tamanhoMaximo.HasValue && tamanhoMaximo.Value > 0 && resultado.Length > tamanhoMaximo.Value)
                resultado = resultado.Substring(0, tamanhoMaximo.Value);

            return resultado;
        }

        private async Task EnviarProgresso(
            string connectionId,
            string tabelaAtual,
            long processados,
            long total,
            string status,
            int totalTabelas,
            int tabelasConcluidas,
            string fase,
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
                Fase = fase,
                EmExecucao = true
            };

            try
            {
                var envio = _hubContext.Clients.Client(connectionId).SendAsync("ReceberProgresso", progresso, cancellationToken);
                var timeout = Task.Delay(2000, cancellationToken);
                await Task.WhenAny(envio, timeout);
            }
            catch
            {
                // Ignora falhas de comunicação SignalR para não travar a importação.
            }
        }

        private readonly record struct ContextoOperacao(
            string TabelaPostgres,
            string? Operacao = null,
            int? Lote = null,
            long? Registro = null);

        private static string FormatarParametros(IReadOnlyList<NpgsqlParameter> parametros)
        {
            if (parametros == null || parametros.Count == 0)
                return "(nenhum)";

            var sb = new StringBuilder();
            for (int i = 0; i < parametros.Count; i++)
            {
                var p = parametros[i];
                var valor = p.Value == null || p.Value == DBNull.Value ? "NULL" : p.Value.ToString();
                if (valor != null && valor.Length > 100)
                    valor = valor.Substring(0, 100) + "...";
                sb.Append($"{p.ParameterName}={valor}");
                if (i < parametros.Count - 1)
                    sb.Append(" | ");
            }
            return sb.ToString();
        }

        private static void LogExcecaoPostgresDetalhada(ContextoOperacao contexto, string sql, IReadOnlyList<NpgsqlParameter>? parametros, Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[ERRO_TRANSACAO] ============================================");
            sb.AppendLine($"Tabela............. {contexto.TabelaPostgres}");
            if (!string.IsNullOrWhiteSpace(contexto.Operacao))
                sb.AppendLine($"Operação........... {contexto.Operacao}");
            if (contexto.Lote.HasValue)
                sb.AppendLine($"Lote............... {contexto.Lote.Value}");
            if (contexto.Registro.HasValue)
                sb.AppendLine($"Registro........... {contexto.Registro.Value}");
            sb.AppendLine($"SQL................ {sql}");
            sb.AppendLine($"Parâmetros......... {FormatarParametros(parametros ?? new List<NpgsqlParameter>())}");

            if (ex is PostgresException pgEx)
            {
                sb.AppendLine($"SqlState........... {pgEx.SqlState}");
                sb.AppendLine($"Message............ {pgEx.Message}");
                try { sb.AppendLine($"Detail............. {pgEx.Detail}"); } catch { }
                try { sb.AppendLine($"Hint............... {pgEx.Hint}"); } catch { }
            }
            else if (ex is NpgsqlException npgsqlEx)
            {
                sb.AppendLine($"SqlState........... {npgsqlEx.SqlState}");
                sb.AppendLine($"Message............ {npgsqlEx.Message}");
                sb.AppendLine($"Detail............. (indisponivel para NpgsqlException generica)");
                sb.AppendLine($"Hint............... (indisponivel para NpgsqlException generica)");
            }
            else
            {
                sb.AppendLine($"SqlState........... (nao e excecao do PostgreSQL)");
                sb.AppendLine($"Message............ {ex.Message}");
            }

            sb.AppendLine($"ExceptionType...... {ex.GetType().FullName}");
            sb.AppendLine($"StackTrace......... {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                sb.AppendLine($"InnerException..... {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                sb.AppendLine($"InnerStackTrace.... {ex.InnerException.StackTrace}");
            }
            sb.AppendLine("[ERRO_TRANSACAO] ============================================");

            LogEmArquivo(sb.ToString(), erro: true);
        }

        private static async Task<int> ExecutarComandoComLogAsync(
            NpgsqlConnection connPg,
            NpgsqlTransaction transaction,
            string sql,
            IReadOnlyList<NpgsqlParameter>? parametros,
            ContextoOperacao contexto,
            CancellationToken cancellationToken,
            bool logarOperacao = true,
            bool logarSucesso = true)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var cmd = new NpgsqlCommand(sql, connPg, transaction);
                cmd.CommandTimeout = 60;
                if (parametros != null)
                {
                    foreach (NpgsqlParameter p in parametros)
                        cmd.Parameters.Add(p);
                }

                var linhasAfetadas = await cmd.ExecuteNonQueryAsync(cancellationToken);
                stopwatch.Stop();
                return linhasAfetadas;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogExcecaoPostgresDetalhada(contexto, sql, parametros, ex);
                throw;
            }
        }

        private async Task LimparTabelaPostgreSQLAsync(
            NpgsqlConnection connPg,
            string tabelaPostgres,
            Func<string, long, long, Task>? reportarProgressoAsync,
            CancellationToken cancellationToken,
            int tamanhoLote,
            NpgsqlTransaction? transacaoExterna = null)
        {
            const int tamanhoLotePrimeiro = 100;
            // Limite maximo de 500 para limpeza: a performance do DELETE degrade conforme avanca,
            // e lotes muito grandes estouram o CommandTimeout de 300s.
            int tamanhoLotePadrao = Math.Min(tamanhoLote > 0 ? tamanhoLote : 1000, 500);

            bool possuiTransacaoExterna = transacaoExterna != null;

            // 1. Verificar se a tabela existe
            if (!await TabelaExisteAsync(connPg, tabelaPostgres, cancellationToken))
            {
                return;
            }

            // 2. Contagem inicial
            long totalRegistros = 0;
            var transactionContagem = transacaoExterna ?? await connPg.BeginTransactionAsync(cancellationToken);
            try
            {
                await TentarBloquearTabelaAsync(connPg, tabelaPostgres, transactionContagem, cancellationToken);

                var sqlCount = $"SELECT COUNT(*) FROM \"{tabelaPostgres}\"";
                using (var cmdCount = new NpgsqlCommand(sqlCount, connPg, transactionContagem))
                {
                    cmdCount.CommandTimeout = 10;
                    var resultado = await cmdCount.ExecuteScalarAsync(cancellationToken);
                    totalRegistros = resultado == null ? 0 : Convert.ToInt64(resultado);
                }

                if (!possuiTransacaoExterna)
                {
                    await transactionContagem.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                if (!possuiTransacaoExterna)
                {
                    try { await transactionContagem.RollbackAsync(cancellationToken); } catch { }
                }
                throw;
            }
            finally
            {
                if (!possuiTransacaoExterna)
                {
                    await transactionContagem.DisposeAsync();
                }
            }

            // 4. DELETE em lotes
            if (totalRegistros > 0)
            {
                long processados = 0;
                int afetados;
                bool primeiroLote = true;
                int numeroLote = 0;

                _eventLog.LogEventViewer($"[Limpeza] Iniciando DELETE em lotes da tabela {tabelaPostgres}. Total={totalRegistros}, lote={tamanhoLotePadrao}", "wInfo");

                do
                {
                    numeroLote++;
                    int tamanhoLoteAtual = primeiroLote ? tamanhoLotePrimeiro : tamanhoLotePadrao;
                    var swLote = Stopwatch.StartNew();

                    var transactionLote = transacaoExterna ?? await connPg.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var sqlDelete = $"DELETE FROM \"{tabelaPostgres}\" WHERE \"Id\" IN (SELECT \"Id\" FROM \"{tabelaPostgres}\" ORDER BY \"Id\" LIMIT {tamanhoLoteAtual})";
                        var contextoDelete = new ContextoOperacao(tabelaPostgres, "DELETE_LOTE");

                        using (var cmdDelete = new NpgsqlCommand(sqlDelete, connPg, transactionLote))
                        {
                            cmdDelete.CommandTimeout = 300;
                            afetados = await cmdDelete.ExecuteNonQueryAsync(cancellationToken);
                        }
                        processados += afetados;

                        if (!possuiTransacaoExterna)
                        {
                            await transactionLote.CommitAsync(cancellationToken);
                        }
                    }
                    catch
                    {
                        if (!possuiTransacaoExterna)
                        {
                            try { await transactionLote.RollbackAsync(cancellationToken); } catch { }
                        }
                        throw;
                    }
                    finally
                    {
                        if (!possuiTransacaoExterna)
                        {
                            await transactionLote.DisposeAsync();
                        }
                    }

                    swLote.Stop();
                    _eventLog.LogEventViewer($"[Limpeza] Lote {numeroLote} da tabela {tabelaPostgres}: afetados={afetados}, processados={processados}/{totalRegistros}, duracao={swLote.Elapsed.TotalSeconds:F2}s", "wInfo");

                    if (reportarProgressoAsync != null)
                    {
                        await reportarProgressoAsync(tabelaPostgres, processados, totalRegistros);
                    }

                    primeiroLote = false;
                }
                while (afetados > 0);

                _eventLog.LogEventViewer($"[Limpeza] Finalizado DELETE da tabela {tabelaPostgres}. Processados={processados}/{totalRegistros}", "wInfo");
            }
        }

        private static async Task TentarBloquearTabelaAsync(
            NpgsqlConnection connPg,
            string tabelaPostgres,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken)
        {
            var sqlLock = $"LOCK TABLE \"{tabelaPostgres}\" IN ACCESS EXCLUSIVE MODE NOWAIT";
            try
            {
                using (var cmd = new NpgsqlCommand(sqlLock, connPg, transaction))
                {
                    cmd.CommandTimeout = 5;
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "55P03" || pgEx.SqlState == "57014")
            {
                var mensagem = $"A tabela \"{tabelaPostgres}\" esta sendo usada por outro processo. " +
                               "Ninguem deve utilizar a base de dados enquanto a importacao estiver em execucao.";
                LogEmArquivo($"[TentarBloquearTabelaAsync] {mensagem} Erro original: {pgEx.Message}", erro: true);
                throw new InvalidOperationException(mensagem, pgEx);
            }
            catch (Exception ex)
            {
                var mensagem = $"Nao foi possivel obter acesso exclusivo a tabela \"{tabelaPostgres}\". " +
                               "Verifique se outro processo esta usando a base de dados e tente novamente.";
                LogEmArquivo($"[TentarBloquearTabelaAsync] {mensagem} Erro original: {ex.Message}", erro: true);
                throw new InvalidOperationException(mensagem, ex);
            }
        }

        /// <summary>
        /// Atualiza as sequencias IDENTITY/SERIAL da tabela para MAX(Id) apos a importacao.
        /// Usa pg_attrdef para obter o nome da sequencia, evitando problemas de case-sensitivity
        /// do pg_get_serial_sequence com identificadores delimitados por aspas.
        /// </summary>
        private static async Task AtualizarSequenciaPosImportacaoAsync(
            NpgsqlConnection connPg,
            string tabelaPostgres,
            CancellationToken cancellationToken)
        {
            try
            {
                // Obtem o nome da sequencia a partir do default da coluna Id (pg_attrdef).
                var sqlSequencia = @"
                    SELECT pg_get_expr(d.adbin, d.adrelid) AS default_expr
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    JOIN pg_attribute a ON a.attrelid = c.oid
                    JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = a.attnum
                    WHERE c.relname = @tabela
                      AND n.nspname = 'public'
                      AND a.attname = 'Id'
                      AND pg_get_expr(d.adbin, d.adrelid) LIKE 'nextval%'";

                string? sequencia = null;
                using (var cmdSeq = new NpgsqlCommand(sqlSequencia, connPg))
                {
                    cmdSeq.Parameters.AddWithValue("tabela", tabelaPostgres);
                    var resultado = await cmdSeq.ExecuteScalarAsync(cancellationToken);
                    if (resultado != null && resultado != DBNull.Value)
                    {
                        var defaultExpr = resultado.ToString();
                        // Extrai o nome da sequencia de nextval('sequencia'::regclass)
                        var match = System.Text.RegularExpressions.Regex.Match(
                            defaultExpr ?? "", @"nextval\('(.+?)'::regclass\)");
                        if (match.Success)
                            sequencia = match.Groups[1].Value;
                    }
                }

                if (string.IsNullOrWhiteSpace(sequencia))
                    return;

                // Atualiza a sequencia para MAX(Id). Se a tabela estiver vazia, reinicia para 1.
                var sqlSetval = $"SELECT setval('{sequencia}', COALESCE((SELECT MAX(\"Id\") FROM \"{tabelaPostgres}\"), 1), true)";
                using (var cmdSetval = new NpgsqlCommand(sqlSetval, connPg))
                {
                    await cmdSetval.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[AtualizarSequenciaPosImportacao] ERRO ao atualizar sequencia de {tabelaPostgres}: {ex.Message}", erro: true);
            }
        }

        private static async Task ReiniciarSequenciaAsync(
            NpgsqlConnection connPg,
            string tabelaPostgres,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken)
        {
            try
            {
                // Obtem o nome da sequencia a partir do default da coluna Id (pg_attrdef).
                // Esta abordagem evita problemas de case-sensitivity do pg_get_serial_sequence.
                var sqlSequencia = @"
                    SELECT pg_get_expr(d.adbin, d.adrelid) AS default_expr
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    JOIN pg_attribute a ON a.attrelid = c.oid
                    JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = a.attnum
                    WHERE c.relname = @tabela
                      AND n.nspname = 'public'
                      AND a.attname = 'Id'
                      AND pg_get_expr(d.adbin, d.adrelid) LIKE 'nextval%'";

                string? sequencia = null;
                using (var cmd = new NpgsqlCommand(sqlSequencia, connPg, transaction))
                {
                    cmd.Parameters.AddWithValue("@tabela", tabelaPostgres);
                    var resultado = await cmd.ExecuteScalarAsync(cancellationToken);
                    if (resultado != null && resultado != DBNull.Value)
                    {
                        var defaultExpr = resultado.ToString();
                        var match = System.Text.RegularExpressions.Regex.Match(
                            defaultExpr ?? "", @"nextval\('(.+?)'::regclass\)");
                        if (match.Success)
                            sequencia = match.Groups[1].Value;
                    }
                }

                if (string.IsNullOrWhiteSpace(sequencia))
                    return;

                // Usa savepoint para isolar falhas de setval e nao abortar a transacao principal.
                var savepointName = $"sp_reinicio_seq_{Guid.NewGuid():N}";
                try
                {
                    await CriarSavepointAsync(connPg, transaction, savepointName, cancellationToken);

                    var sqlReset = $"SELECT setval('{sequencia}', 1, false)";
                    using (var cmd = new NpgsqlCommand(sqlReset, connPg, transaction))
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    try { await RollbackToSavepointAsync(connPg, transaction, savepointName, cancellationToken); } catch { }
                    LogEmArquivo($"[ReiniciarSequenciaAsync] ERRO ao reiniciar sequencia de {tabelaPostgres}: {ex.Message}", erro: true);
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[ReiniciarSequenciaAsync] ERRO ao reiniciar sequencia de {tabelaPostgres}: {ex.Message}", erro: true);
            }
        }

        private async Task<bool> TabelaExisteAsync(NpgsqlConnection conn, string tabela, CancellationToken cancellationToken)
        {
            const string sql = @"
                               SELECT EXISTS (
                               SELECT 1
                               FROM information_schema.tables
                               WHERE table_schema = current_schema()
                               AND LOWER(table_name) = LOWER(@tabela)
                               )";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("tabela", tabela);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            var exists = result is bool b && b;
            return exists;
        }

        private async Task<(List<string> TabelasLimpeza, List<string> TabelasAdicionadas)> LimparTodasTabelasPreImportacaoAsync(
            string postgresConnectionString,
            List<(string Firebird, string Postgres, string Descricao)> tabelasParaImportar,
            List<DependenciaFk> dependenciasFk,
            Dictionary<string, string> firebirdParaPostgres,
            Dictionary<string, string> postgresParaFirebird,
            string connectionId,
            int totalTabelas,
            int tamanhoLote,
            CancellationToken cancellationToken)
        {
            // Conjunto das tabelas selecionadas (PostgreSQL).
            var tabelasSelecionadasPostgres = tabelasParaImportar
                .Select(t => t.Postgres)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!tabelasSelecionadasPostgres.Any())
                return (new List<string>(), new List<string>());

            // Expande o conjunto incluindo todas as tabelas filhas (diretas e indiretas)
            // que referenciam as tabelas selecionadas. Isso garante que a limpeza da tabela pai
            // não seja bloqueada por FKs de tabelas não selecionadas.
            var tabelasExpandidas = new HashSet<string>(tabelasSelecionadasPostgres, StringComparer.OrdinalIgnoreCase);
            bool houveAdicao;
            do
            {
                houveAdicao = false;
                foreach (var dep in dependenciasFk)
                {
                    if (tabelasExpandidas.Contains(dep.TabelaPai) && !tabelasExpandidas.Contains(dep.TabelaFilha))
                    {
                        tabelasExpandidas.Add(dep.TabelaFilha);
                        houveAdicao = true;
                    }
                }
            }
            while (houveAdicao);

            var tabelasAdicionadas = tabelasExpandidas
                .Where(t => !tabelasSelecionadasPostgres.Contains(t))
                .ToList();

            // Ordena pela ordem topológica (filhas -> pai).
            var ordemFinal = _schemaComparer.OrdenarParaLimpeza(tabelasExpandidas, dependenciasFk);

            if (!ordemFinal.Any())
                return (ordemFinal, tabelasAdicionadas);

            await EnviarProgresso(connectionId, "Preparação", 0, 0, $"Ordem de limpeza: {string.Join(", ", ordemFinal.Select(t => postgresParaFirebird.GetValueOrDefault(t, t)))}", totalTabelas, 0, "Limpeza", cancellationToken);

            using var connPg = new NpgsqlConnection(postgresConnectionString);
            await connPg.OpenAsync(cancellationToken);

            for (int i = 0; i < ordemFinal.Count; i++)
            {
                var tabelaPostgres = ordemFinal[i];
                var tabelaFirebird = postgresParaFirebird.GetValueOrDefault(tabelaPostgres, tabelaPostgres);

                try
                {
                    await EnviarProgresso(connectionId, tabelaFirebird, i, ordemFinal.Count, $"Analisando tabela {tabelaFirebird}...", totalTabelas, 0, "Limpeza", cancellationToken);

                    Func<string, long, long, Task> reportarProgresso = async (t, processados, total) =>
                    {
                        await EnviarProgresso(connectionId, t, processados, total, $"Limpando tabela {t}...", totalTabelas, 0, "Limpeza", cancellationToken);
                    };

                    await LimparTabelaPostgreSQLAsync(connPg, tabelaPostgres, reportarProgresso, cancellationToken, tamanhoLote);
                }
                catch (Exception ex)
                {
                    LogEmArquivo($"[PreLimpeza] ERRO ao limpar {tabelaPostgres}: {ex.Message}", erro: true);
                    throw;
                }
            }

            // Reinicia sequências em transação separada para nao corromper a limpeza em caso de erro.
            foreach (var tabelaPostgres in ordemFinal)
            {
                try
                {
                    await using var transactionSequencia = await connPg.BeginTransactionAsync(cancellationToken);
                    await ReiniciarSequenciaAsync(connPg, tabelaPostgres, transactionSequencia, cancellationToken);
                    await transactionSequencia.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    LogEmArquivo($"[PreLimpeza] AVISO: nao foi possivel reiniciar sequencia de {tabelaPostgres}: {ex.Message}", erro: true);
                }
            }

            return (ordemFinal, tabelasAdicionadas);
        }

        private static string FormatarMensagemErroTela(Exception ex)
        {
            var mensagem = ex.Message;
            if (mensagem.Contains("Exception while reading from stream", StringComparison.OrdinalIgnoreCase))
            {
                mensagem += " Verifique o commit que pode estar pendente na base de dados por algum outro processo.";
            }
            return mensagem;
        }

        private static async Task<HashSet<string>> ObterNomesTabelasFirebirdAsync(string firebirdConnectionString, CancellationToken cancellationToken)
        {
            var tabelas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var connFb = new FbConnection(firebirdConnectionString);
                await connFb.OpenAsync(cancellationToken);
                const string sql = @"SELECT TRIM(RDB$RELATION_NAME) AS NOME FROM RDB$RELATIONS WHERE RDB$VIEW_SOURCE IS NULL AND RDB$SYSTEM_FLAG = 0";
                using var cmd = new FbCommand(sql, connFb);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    tabelas.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[ObterNomesTabelasFirebirdAsync] ERRO ao obter nomes das tabelas: {ex.GetType().Name}: {ex.Message}", erro: true);
            }
            return tabelas;
        }

        private static async Task<Dictionary<string, int>> CarregarLookupInstituicaoAsync(
            string firebirdConnectionString,
            string postgresConnectionString,
            CancellationToken cancellationToken)
        {
            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 1. Tenta carregar do Firebird. Se a tabela nao existir, cai no fallback do PostgreSQL.
            try
            {
                using var connFb = new FbConnection(firebirdConnectionString);
                await connFb.OpenAsync(cancellationToken);

                // Verifica se a tabela Instituicao existe no Firebird (case-insensitive).
                var sqlExiste = "SELECT COUNT(*) FROM RDB$RELATIONS WHERE UPPER(TRIM(RDB$RELATION_NAME)) = UPPER('Instituicao')";
                using var cmdExiste = new FbCommand(sqlExiste, connFb);
                var count = Convert.ToInt32(await cmdExiste.ExecuteScalarAsync(cancellationToken));

                if (count > 0)
                {
                    // Usa aspas duplas nos nomes de tabela e colunas porque o Firebird preserva
                    // case-sensitivity para delimited identifiers.
                    const string sql = @"SELECT ""Codigo"", ""Instituicao"", ""Nome"" FROM ""Instituicao""";
                    using var cmd = new FbCommand(sql, connFb);
                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var codigo = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        var sigla = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)?.Trim() ?? string.Empty;
                        var nome = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)?.Trim() ?? string.Empty;

                        if (codigo > 0)
                        {
                            if (!string.IsNullOrWhiteSpace(sigla))
                                lookup[sigla] = codigo;
                            if (!string.IsNullOrWhiteSpace(nome))
                                lookup[nome] = codigo;
                        }
                    }
                    return lookup;
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[CarregarLookupInstituicaoAsync] AVISO: erro ao carregar do Firebird: {ex.GetType().Name}: {ex.Message}. Tentando PostgreSQL.", erro: true);
            }

            // 2. Fallback: carrega do PostgreSQL (tabela pode ter dados de seed/migration).
            try
            {
                using var connPg = new NpgsqlConnection(postgresConnectionString);
                await connPg.OpenAsync(cancellationToken);
                const string sql = @"SELECT ""Id"", ""Sigla"", ""Nome"" FROM ""Instituicao""";
                using var cmd = new NpgsqlCommand(sql, connPg);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    var sigla = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)?.Trim() ?? string.Empty;
                    var nome = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)?.Trim() ?? string.Empty;

                    if (id > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(sigla))
                            lookup[sigla] = id;
                        if (!string.IsNullOrWhiteSpace(nome))
                            lookup[nome] = id;
                    }
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[CarregarLookupInstituicaoAsync] AVISO: nao foi possivel carregar lookup de Instituicao do PostgreSQL: {ex.GetType().Name}: {ex.Message}", erro: true);
            }

            return lookup;
        }

        private static async Task<Dictionary<string, int>> CarregarLookupTabelaExamesAsync(
            string firebirdConnectionString,
            string postgresConnectionString,
            CancellationToken cancellationToken)
        {
            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 1. Tenta carregar do Firebird. Se a tabela nao existir, cai no fallback do PostgreSQL.
            try
            {
                using var connFb = new FbConnection(firebirdConnectionString);
                await connFb.OpenAsync(cancellationToken);

                var sqlExiste = "SELECT COUNT(*) FROM RDB$RELATIONS WHERE UPPER(TRIM(RDB$RELATION_NAME)) = UPPER('TabelaExames')";
                using var cmdExiste = new FbCommand(sqlExiste, connFb);
                var count = Convert.ToInt32(await cmdExiste.ExecuteScalarAsync(cancellationToken));

                if (count > 0)
                {
                    const string sql = @"SELECT ""Codigo"", ""SiglaTabela"", ""NomeTabela"" FROM ""TabelaExames""";
                    using var cmd = new FbCommand(sql, connFb);
                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var codigo = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        var sigla = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)?.Trim() ?? string.Empty;
                        var nome = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)?.Trim() ?? string.Empty;

                        if (codigo > 0)
                        {
                            if (!string.IsNullOrWhiteSpace(sigla))
                                lookup[sigla] = codigo;
                            if (!string.IsNullOrWhiteSpace(nome))
                                lookup[nome] = codigo;
                        }
                    }
                    return lookup;
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[CarregarLookupTabelaExamesAsync] AVISO: erro ao carregar do Firebird: {ex.GetType().Name}: {ex.Message}. Tentando PostgreSQL.", erro: true);
            }

            // 2. Fallback: carrega do PostgreSQL.
            try
            {
                using var connPg = new NpgsqlConnection(postgresConnectionString);
                await connPg.OpenAsync(cancellationToken);
                const string sql = @"SELECT ""Id"", ""SiglaTabela"", ""NomeTabela"" FROM ""TabelaExames""";
                using var cmd = new NpgsqlCommand(sql, connPg);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    var sigla = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)?.Trim() ?? string.Empty;
                    var nome = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)?.Trim() ?? string.Empty;

                    if (id > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(sigla))
                            lookup[sigla] = id;
                        if (!string.IsNullOrWhiteSpace(nome))
                            lookup[nome] = id;
                    }
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[CarregarLookupTabelaExamesAsync] AVISO: nao foi possivel carregar lookup de TabelaExames do PostgreSQL: {ex.GetType().Name}: {ex.Message}", erro: true);
            }

            return lookup;
        }

        private static async Task<HashSet<long>> CarregarIdsClasseExamesAsync(string postgresConnectionString, CancellationToken cancellationToken)
        {
            var ids = new HashSet<long>();
            try
            {
                using var connPg = new NpgsqlConnection(postgresConnectionString);
                await connPg.OpenAsync(cancellationToken);
                const string sql = @"SELECT ""Id"" FROM ""ClasseExames""";
                using var cmd = new NpgsqlCommand(sql, connPg);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
                    if (id > 0)
                        ids.Add(id);
                }
            }
            catch (Exception ex)
            {
                LogEmArquivo($"[CarregarIdsClasseExamesAsync] AVISO: nao foi possivel carregar IDs de ClasseExames: {ex.GetType().Name}: {ex.Message}", erro: true);
            }
            return ids;
        }

        private static void LogEmArquivo(string mensagem, bool erro = false)
        {
            // So registra no log o que for erro. Mensagens informativas sao silenciosas.
            if (!erro)
                return;

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

        //Feito pelo Kiro em 01/08/2026
        /// <summary>
        /// Cria registros de Folha ausentes no PlanoExames.
        /// Para cada combinação ClasseExamesId + TabelaExamesId que tem Principais/Itens
        /// mas não tem o registro de Folha (ContaExame terminado em "0000000"), insere
        /// o registro automaticamente com o nome da ClasseExames.
        /// </summary>
        private static async Task<int> CriarFolhasAusentesPlanoExamesAsync(
            string postgresConnectionString, CancellationToken cancellationToken)
        {
            const string sql = @"
                INSERT INTO ""PlanoExames"" (""ClasseExamesId"", ""TabelaExamesId"", ""ContaExame"",
                    ""Descricao"", ""RefExame"", ""RefItem"",
                    ""CitoInstituicao"", ""CitoTituloExame"", ""QCH"", ""Etiqueta"", ""Etiquetas"",
                    ""Seleciona"", ""NaoMostrar"")
                SELECT
                    f.""ClasseExamesId"",
                    f.""TabelaExamesId"",
                    '11' || LPAD(f.""ClasseExamesId""::text, 2, '0') || '0000000',
                    c.""RefExame"",
                    c.""RefExame"",
                    c.""RefExame"",
                    0, 0, 0, 0, 0, 0, 0
                FROM (
                    SELECT DISTINCT p.""ClasseExamesId"", p.""TabelaExamesId""
                    FROM ""PlanoExames"" p
                    WHERE SUBSTRING(p.""ContaExame"" FROM 5 FOR 7) <> '0000000'
                    EXCEPT
                    SELECT ""ClasseExamesId"", ""TabelaExamesId""
                    FROM ""PlanoExames""
                    WHERE SUBSTRING(""ContaExame"" FROM 5 FOR 7) = '0000000'
                ) f
                JOIN ""ClasseExames"" c ON c.""Id"" = f.""ClasseExamesId""";

            using var conn = new NpgsqlConnection(postgresConnectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new NpgsqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        //..Kiro

        //Feito pelo Kiro em 27/07/2026
        /// <summary>
        /// Deduplicação pós-importação: remove registros duplicados de Pacientes e Médicos,
        /// mantendo o maior Id (registro mais recente) e migrando as FKs das tabelas filhas
        /// para o registro sobrevivente.
        /// Critérios: Pacientes por NomePaciente+Nascimento, Médicos por NomeMedico+CRM.
        /// </summary>
        private async Task<(int PacientesRemovidos, int MedicosRemovidos)> DeduplicarPosImportacaoAsync(
            string postgresConnectionString,
            bool deduplicarPacientes,
            bool deduplicarMedicos,
            string connectionId,
            int totalTabelas,
            int tabelasConcluidas,
            CancellationToken cancellationToken)
        {
            int pacientesRemovidos = 0;
            int medicosRemovidos = 0;

            using var conn = new NpgsqlConnection(postgresConnectionString);
            await conn.OpenAsync(cancellationToken);
            await using (var cmdConfig = new NpgsqlCommand(
                "SET idle_in_transaction_session_timeout = 0; SET statement_timeout = 0;", conn))
            {
                await cmdConfig.ExecuteNonQueryAsync(cancellationToken);
            }

            if (deduplicarPacientes)
            {
                await EnviarProgresso(connectionId, "Deduplicação", 0, 0,
                    "Deduplicando Pacientes (NomePaciente + Nascimento)...",
                    totalTabelas, tabelasConcluidas, "Deduplicação", cancellationToken);

                pacientesRemovidos = await DeduplicarPacientesAsync(conn, cancellationToken);

                LogEmArquivo($"[Deduplicação] Pacientes duplicados removidos: {pacientesRemovidos}", erro: pacientesRemovidos > 0);
                _eventLog.LogEventViewer($"[CargaDados] Deduplicação: {pacientesRemovidos} Pacientes duplicados removidos.", pacientesRemovidos > 0 ? "wInfo" : "wInfo");
            }

            if (deduplicarMedicos)
            {
                await EnviarProgresso(connectionId, "Deduplicação", 0, 0,
                    "Deduplicando Médicos (NomeMedico + CRM)...",
                    totalTabelas, tabelasConcluidas, "Deduplicação", cancellationToken);

                medicosRemovidos = await DeduplicarMedicosAsync(conn, cancellationToken);

                LogEmArquivo($"[Deduplicação] Médicos duplicados removidos: {medicosRemovidos}", erro: medicosRemovidos > 0);
                _eventLog.LogEventViewer($"[CargaDados] Deduplicação: {medicosRemovidos} Médicos duplicados removidos.", medicosRemovidos > 0 ? "wInfo" : "wInfo");
            }

            // Envia progresso final da deduplicação
            var msgFinal = $"Deduplicação concluída. Pacientes removidos: {pacientesRemovidos}, Médicos removidos: {medicosRemovidos}";
            await EnviarProgresso(connectionId, "Deduplicação", 100, 100,
                msgFinal, totalTabelas, tabelasConcluidas, "Deduplicação", cancellationToken);

            return (pacientesRemovidos, medicosRemovidos);
        }

        /// <summary>
        /// Remove Pacientes duplicados: agrupa por UPPER(TRIM(NomePaciente)) + Nascimento::date,
        /// mantém o maior Id, migra FKs em todas as tabelas filhas, e exclui os menores.
        /// </summary>
        private static async Task<int> DeduplicarPacientesAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
        {
            // Identifica os IDs duplicados e seus sobreviventes numa transação
            await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

            // Cria tabela temporária com mapeamento id_duplicado → id_sobrevivente
            const string sqlCriarTemp = @"
                CREATE TEMP TABLE tmp_pac_dedup AS
                WITH duplicatas AS (
                    SELECT
                        ""Id"",
                        UPPER(TRIM(""NomePaciente"")) AS nome_norm,
                        ""Nascimento""::date AS nasc,
                        MAX(""Id"") OVER (
                            PARTITION BY UPPER(TRIM(""NomePaciente"")), ""Nascimento""::date
                        ) AS id_sobrevivente
                    FROM ""Pacientes""
                ),
                grupos AS (
                    SELECT nome_norm, nasc
                    FROM duplicatas
                    GROUP BY nome_norm, nasc
                    HAVING COUNT(*) > 1
                )
                SELECT d.""Id"" AS id_duplicado, d.id_sobrevivente
                FROM duplicatas d
                INNER JOIN grupos g ON d.nome_norm = g.nome_norm AND d.nasc = g.nasc
                WHERE d.""Id"" <> d.id_sobrevivente";

            await using (var cmd = new NpgsqlCommand(sqlCriarTemp, conn, transaction))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Conta duplicatas
            int totalDuplicatas;
            await using (var cmdCount = new NpgsqlCommand("SELECT COUNT(*) FROM tmp_pac_dedup", conn, transaction))
            {
                totalDuplicatas = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(cancellationToken));
            }

            if (totalDuplicatas == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            // Migra FKs em todas as tabelas filhas
            string[] tabelasFilhasPaciente = new[]
            {
                "ExamesRealizados", "ExamesRealizadosAM", "ItensExamesRealizados",
                "ItensExamesRealizadosAM", "Requisitar", "ExamesImpressos",
                "ExamesPendentes", "ExamesExportados", "FichasInternas",
                "FichasLotes", "FichasPlanilhas"
            };

            foreach (var tabela in tabelasFilhasPaciente)
            {
                var sqlUpdate = $@"
                    UPDATE ""{tabela}"" f
                    SET ""PacienteId"" = t.id_sobrevivente
                    FROM tmp_pac_dedup t
                    WHERE f.""PacienteId"" = t.id_duplicado";

                await using (var cmdUpd = new NpgsqlCommand(sqlUpdate, conn, transaction))
                {
                    await cmdUpd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            // Exclui registros duplicados
            const string sqlDelete = @"DELETE FROM ""Pacientes"" WHERE ""Id"" IN (SELECT id_duplicado FROM tmp_pac_dedup)";
            await using (var cmdDel = new NpgsqlCommand(sqlDelete, conn, transaction))
            {
                await cmdDel.ExecuteNonQueryAsync(cancellationToken);
            }

            // Atualiza sequence
            const string sqlSeq = @"SELECT setval(pg_get_serial_sequence('""Pacientes""', 'Id'), COALESCE((SELECT MAX(""Id"") FROM ""Pacientes""), 1))";
            await using (var cmdSeq = new NpgsqlCommand(sqlSeq, conn, transaction))
            {
                await cmdSeq.ExecuteScalarAsync(cancellationToken);
            }

            // Dropa tabela temporária
            await using (var cmdDrop = new NpgsqlCommand("DROP TABLE IF EXISTS tmp_pac_dedup", conn, transaction))
            {
                await cmdDrop.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return totalDuplicatas;
        }

        /// <summary>
        /// Remove Médicos duplicados: agrupa por UPPER(TRIM(NomeMedico)) + UPPER(TRIM(CRM)),
        /// mantém o maior Id, migra FKs em todas as tabelas filhas, e exclui os menores.
        /// </summary>
        private static async Task<int> DeduplicarMedicosAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
        {
            await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

            const string sqlCriarTemp = @"
                CREATE TEMP TABLE tmp_med_dedup AS
                WITH duplicatas AS (
                    SELECT
                        ""Id"",
                        UPPER(TRIM(""NomeMedico"")) AS nome_norm,
                        UPPER(TRIM(""CRM"")) AS crm_norm,
                        MAX(""Id"") OVER (
                            PARTITION BY UPPER(TRIM(""NomeMedico"")), UPPER(TRIM(""CRM""))
                        ) AS id_sobrevivente
                    FROM ""Medicos""
                ),
                grupos AS (
                    SELECT nome_norm, crm_norm
                    FROM duplicatas
                    GROUP BY nome_norm, crm_norm
                    HAVING COUNT(*) > 1
                )
                SELECT d.""Id"" AS id_duplicado, d.id_sobrevivente
                FROM duplicatas d
                INNER JOIN grupos g ON d.nome_norm = g.nome_norm AND d.crm_norm = g.crm_norm
                WHERE d.""Id"" <> d.id_sobrevivente";

            await using (var cmd = new NpgsqlCommand(sqlCriarTemp, conn, transaction))
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            int totalDuplicatas;
            await using (var cmdCount = new NpgsqlCommand("SELECT COUNT(*) FROM tmp_med_dedup", conn, transaction))
            {
                totalDuplicatas = Convert.ToInt32(await cmdCount.ExecuteScalarAsync(cancellationToken));
            }

            if (totalDuplicatas == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            // Migra FKs nas tabelas filhas de Medicos
            string[] tabelasFilhasMedico = new[]
            {
                "ExamesRealizados", "ExamesRealizadosAM", "Requisitar",
                "ExamesPendentes", "ExamesExportados", "FichasInternas",
                "FichasLotes", "FichasPlanilhas"
            };

            foreach (var tabela in tabelasFilhasMedico)
            {
                var sqlUpdate = $@"
                    UPDATE ""{tabela}"" f
                    SET ""MedicoId"" = t.id_sobrevivente
                    FROM tmp_med_dedup t
                    WHERE f.""MedicoId"" = t.id_duplicado";

                await using (var cmdUpd = new NpgsqlCommand(sqlUpdate, conn, transaction))
                {
                    await cmdUpd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            // Exclui registros duplicados
            const string sqlDelete = @"DELETE FROM ""Medicos"" WHERE ""Id"" IN (SELECT id_duplicado FROM tmp_med_dedup)";
            await using (var cmdDel = new NpgsqlCommand(sqlDelete, conn, transaction))
            {
                await cmdDel.ExecuteNonQueryAsync(cancellationToken);
            }

            // Atualiza sequence
            const string sqlSeq = @"SELECT setval(pg_get_serial_sequence('""Medicos""', 'Id'), COALESCE((SELECT MAX(""Id"") FROM ""Medicos""), 1))";
            await using (var cmdSeq = new NpgsqlCommand(sqlSeq, conn, transaction))
            {
                await cmdSeq.ExecuteScalarAsync(cancellationToken);
            }

            // Dropa tabela temporária
            await using (var cmdDrop = new NpgsqlCommand("DROP TABLE IF EXISTS tmp_med_dedup", conn, transaction))
            {
                await cmdDrop.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return totalDuplicatas;
        }
        //..Kiro


    }
}
