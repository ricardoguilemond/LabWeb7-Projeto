using LabWebMvc.MVC.Areas.Controllers;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Areas.Utils
{
    //Feito pelo Kiro em 11/07/2025
    /// <summary>
    /// Rotina de importação dos arquivos .DOC (RTF Delphi) existentes na pasta Laudos/
    /// para a tabela ExameReferencia. Pode ser acionada via endpoint administrativo.
    /// </summary>
    public class ImportadorReferenciaExames
    {
        private readonly Db _db;
        private readonly IWebHostEnvironment _env;
        private readonly IGeralService _geralService;

        public ImportadorReferenciaExames(Db db, IWebHostEnvironment env, IGeralService geralService)
        {
            _db = db;
            _env = env;
            _geralService = geralService;
        }

        public async Task<ResultadoImportacao> ExecutarAsync(string? pastaCustomizada = null)
        {
            var resultado = new ResultadoImportacao();

            // Usar pasta customizada se informada, senão default Laudos/
            string pastaLaudos;
            if (!string.IsNullOrWhiteSpace(pastaCustomizada))
            {
                pastaLaudos = Path.IsPathRooted(pastaCustomizada)
                    ? pastaCustomizada
                    : Path.Combine(_env.ContentRootPath, pastaCustomizada);
            }
            else
            {
                pastaLaudos = Path.Combine(_env.ContentRootPath, "Laudos");
            }

            if (!Directory.Exists(pastaLaudos))
            {
                resultado.Erros.Add("Pasta não encontrada: " + pastaLaudos);
                return resultado;
            }

            var arquivos = Directory.GetFiles(pastaLaudos, "*.DOC");
            resultado.TotalLidos = arquivos.Length;

            if (arquivos.Length == 0)
                return resultado;

            // Carregar registros existentes para verificação de duplicidade (ContaExame + TabelaExamesId + DataAlteracao)
            var existentes = await _db.ExameReferencia
                .AsNoTracking()
                .Select(r => new { r.ContaExame, r.TabelaExamesId, r.DataAlteracao })
                .ToListAsync();

            var existentesSet = new HashSet<string>(
                existentes.Select(e => $"{e.ContaExame}|{e.TabelaExamesId}|{e.DataAlteracao:yyyyMMddHHmmss}"));

            // Carregar PlanoExames para lookup de TabelaExamesId por ContaExame
            var planoLookup = await _db.PlanoExames
                .AsNoTracking()
                .GroupBy(p => p.ContaExame)
                .Select(g => new { ContaExame = g.Key, TabelaExamesId = g.Min(p => p.TabelaExamesId) })
                .ToDictionaryAsync(x => x.ContaExame, x => x.TabelaExamesId);

            DateTime agora = _geralService.ObterDataHoraUtc();

            foreach (var arquivo in arquivos)
            {
                try
                {
                    string nomeArquivo = Path.GetFileNameWithoutExtension(arquivo);
                    string contaExame = nomeArquivo.Trim();

                    // Validar nomenclatura: apenas dígitos (código de item de exame)
                    // ContaExame deve ter 11 dígitos numéricos. Se não for, ignorar silenciosamente.
                    if (contaExame.Length != 11 || !contaExame.All(char.IsDigit))
                    {
                        // Arquivo não corresponde a um código de exame — ignorar silenciosamente
                        resultado.TotalLidos--; // não contar como "lido" válido
                        continue;
                    }

                    // Obter data/hora de última modificação do arquivo (UTC)
                    var infoArquivo = new FileInfo(arquivo);
                    DateTime dataModificacaoArquivo = infoArquivo.LastWriteTimeUtc;

                    // Buscar TabelaExamesId via PlanoExames
                    int tabelaExamesId = planoLookup.TryGetValue(contaExame, out int tabId) ? tabId : 1;

                    // Verificar duplicidade: mesmo ContaExame + TabelaExamesId + mesma data/hora de modificação
                    string chave = $"{contaExame}|{tabelaExamesId}|{dataModificacaoArquivo:yyyyMMddHHmmss}";
                    if (existentesSet.Contains(chave))
                    {
                        resultado.Ignorados++;
                        resultado.Avisos.Add($"{nomeArquivo}.DOC: já importado (mesma data de alteração).");
                        continue;
                    }

                    // Verificar se já existe registro com mesmo ContaExame + TabelaExamesId (data diferente)
                    // Neste caso, permite reimportação (arquivo foi atualizado) — insere novo registro
                    // O sistema imprimirá ambos na ordem de DataCriacao (regra de duplicados do Spec)

                    // Ler conteúdo binário completo
                    byte[] conteudoBinario = await File.ReadAllBytesAsync(arquivo);

                    // Inserir na tabela ExameReferencia
                    var novoRegistro = new ExameReferencia
                    {
                        ContaExame = contaExame,
                        TabelaExamesId = tabelaExamesId,
                        ConteudoBinario = conteudoBinario,
                        FormatoOrigem = "RTF",
                        AlinhaLaudo = 0,
                        DataCriacao = dataModificacaoArquivo,
                        DataAlteracao = dataModificacaoArquivo,
                        UsuarioAlteracao = "IMPORTACAO",
                        Versao = 1
                    };

                    _db.ExameReferencia.Add(novoRegistro);
                    existentesSet.Add(chave);
                    resultado.Importados++;
                }
                catch (Exception ex)
                {
                    resultado.Erros.Add($"{Path.GetFileName(arquivo)}: {ex.Message}");
                }
            }

            if (resultado.Importados > 0)
                await _db.SaveChangesAsync();

            return resultado;
        }
    }

    public class ResultadoImportacao
    {
        public int TotalLidos { get; set; }
        public int Importados { get; set; }
        public int Ignorados { get; set; }
        public List<string> Avisos { get; set; } = [];
        public List<string> Erros { get; set; } = [];
    }
    //..Kiro
}
