using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.ViewModel.CargaDados;
using Microsoft.Extensions.Caching.Memory;

namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public interface ICargaDadosExecutor
    {
        string IniciarImportacao(ImportacaoConfiguracao configuracao, string postgresConnectionString);
        ImportacaoStatus? ObterStatus(string chave);
        void DefinirDecisao(string chave, bool ignorar);
    }

    public class ImportacaoStatus
    {
        public string Chave { get; set; } = string.Empty;
        public bool EmExecucao { get; set; }
        public bool Concluido { get; set; }
        public bool Erro { get; set; }
        public string? MensagemErro { get; set; }
        public LoteProgressoViewModel Progresso { get; set; } = new();
        public ImportacaoFinalViewModel? Resultado { get; set; }
        public bool AguardandoDecisao { get; set; }
        public string? TabelaComErro { get; set; }
        public string? DetalheErro { get; set; }
    }

    public class CargaDadosExecutor : ICargaDadosExecutor
    {
        private readonly IFirebirdImporter _importer;
        private readonly IEventLogHelper _eventLog;
        private readonly IMemoryCache _cache;

        public CargaDadosExecutor(IFirebirdImporter importer, IEventLogHelper eventLog, IMemoryCache cache)
        {
            _importer = importer;
            _eventLog = eventLog;
            _cache = cache;
        }

        public string IniciarImportacao(ImportacaoConfiguracao configuracao, string postgresConnectionString)
        {
            string chave = Guid.NewGuid().ToString("N");

            var status = new ImportacaoStatus
            {
                Chave = chave,
                EmExecucao = true,
                Progresso = new LoteProgressoViewModel
                {
                    Status = "Inicializando...",
                    EmExecucao = true
                }
            };

            _cache.Set(chave, status, TimeSpan.FromHours(2));

            _ = Task.Run(async () => await ExecutarImportacaoAsync(chave, configuracao, postgresConnectionString), CancellationToken.None);

            return chave;
        }

        public ImportacaoStatus? ObterStatus(string chave)
        {
            _cache.TryGetValue(chave, out ImportacaoStatus? status);
            return status;
        }

        public void DefinirDecisao(string chave, bool ignorar)
        {
            if (_cache.TryGetValue(chave, out ImportacaoStatus? status) && status != null)
            {
                status.AguardandoDecisao = false;
                _cache.Set(chave, status, TimeSpan.FromHours(2));

                if (ignorar)
                {
                    // A decisão de ignorar será lida durante a execução
                    _cache.Set($"{chave}_ignorar", true, TimeSpan.FromHours(2));
                }
                else
                {
                    _cache.Set($"{chave}_cancelar", true, TimeSpan.FromHours(2));
                }
            }
        }

        private async Task ExecutarImportacaoAsync(string chave, ImportacaoConfiguracao configuracao, string postgresConnectionString)
        {
            if (!_cache.TryGetValue(chave, out ImportacaoStatus? status) || status == null)
                return;

            try
            {
                var resultado = await _importer.ImportarAsync(configuracao, postgresConnectionString, CancellationToken.None);

                status.Resultado = resultado;
                status.Concluido = true;
                status.EmExecucao = false;
                status.Progresso.EmExecucao = false;
                status.Progresso.PorcentagemTotal = 100;
                status.Progresso.Status = resultado.MensagemFinal ?? "Concluído";
            }
            catch (Exception ex)
            {
                status.Erro = true;
                status.EmExecucao = false;
                status.MensagemErro = ex.Message;
                status.Progresso.EmExecucao = false;
                status.Progresso.Erro = true;
                status.Progresso.Mensagem = ex.Message;
                _eventLog.LogEventViewer($"[CargaDados] Erro na importação: {ex.Message}", "wError");
            }

            _cache.Set(chave, status, TimeSpan.FromHours(2));
        }
    }
}
