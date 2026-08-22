using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    //Feito pelo Kiro em 14/07/2025
    [Route("ExameReferencia")]
    public class ExameReferenciaController : BaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly IGeralService _geralService;

        public ExameReferenciaController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IWebHostEnvironment env,
            IGeralService geralService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _env = env;
            _geralService = geralService;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            ViewBag.TextoMenu = new object[] { "Plano de Exames", false };

            var dados = _db.ExameReferencia
                .Include(r => r.TabelaExames)
                .AsNoTracking()
                .OrderByDescending(r => r.DataAlteracao)
                .Select(r => new
                {
                    r.Id,
                    r.ContaExame,
                    Descricao = _db.PlanoExames
                        .Where(p => p.ContaExame == r.ContaExame && p.TabelaExamesId == r.TabelaExamesId)
                        .Select(p => p.Descricao)
                        .FirstOrDefault() ?? "-",
                    Tabela = r.TabelaExames.NomeTabela,
                    r.FormatoOrigem,
                    r.DataAlteracao,
                    r.UsuarioAlteracao
                })
                .ToList();

            var listaDados = dados.Select(r => new
            {
                r.Id,
                r.ContaExame,
                r.Descricao,
                r.Tabela,
                r.FormatoOrigem,
                DataAlteracao = r.DataAlteracao.Kind == DateTimeKind.Utc
                    ? TimeZoneInfo.ConvertTimeFromUtc(r.DataAlteracao, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).ToString("dd/MM/yyyy HH:mm")
                    : r.DataAlteracao.ToString("dd/MM/yyyy HH:mm"),
                r.UsuarioAlteracao
            }).ToList();

            ViewBag.ListaDados = listaDados;

            return View();
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Editar")]
        public IActionResult Editar(int? id, string? contaExame = null, int? tabelaExamesId = null)
        {
            ViewBag.TextoMenu = new object[] { "Plano de Exames", false };

            var tabelas = _db.TabelaExames
                .OrderBy(t => t.NomeTabela)
                .Select(t => new { t.Id, t.NomeTabela })
                .ToList();
            ViewBag.Tabelas = tabelas;

            // Se não há id, mas há contaExame + tabelaExamesId, buscar registro existente
            if ((!id.HasValue || id.Value <= 0) && !string.IsNullOrWhiteSpace(contaExame) && tabelaExamesId.HasValue)
            {
                var existente = _db.ExameReferencia
                    .AsNoTracking()
                    .FirstOrDefault(r => r.ContaExame == contaExame && r.TabelaExamesId == tabelaExamesId.Value);
                if (existente != null)
                {
                    id = existente.Id;
                }
            }

            if (id.HasValue && id.Value > 0)
            {
                var registro = _db.ExameReferencia
                    .AsNoTracking()
                    .FirstOrDefault(r => r.Id == id.Value);

                if (registro == null)
                    return RedirectToAction("Index");

                string descricaoExame = _db.PlanoExames
                    .AsNoTracking()
                    .Where(p => p.ContaExame == registro.ContaExame && p.TabelaExamesId == registro.TabelaExamesId)
                    .Select(p => p.Descricao)
                    .FirstOrDefault() ?? "";

                string conteudoHtml = "";
                if (registro.ConteudoBinario != null && registro.ConteudoBinario.Length > 0)
                {
                    if (registro.FormatoOrigem == "HTML")
                    {
                        conteudoHtml = System.Text.Encoding.UTF8.GetString(registro.ConteudoBinario);
                    }
                    else if (registro.FormatoOrigem == "RTF")
                    {
                        // Converter RTF para texto legível e exibir no Quill como HTML simples
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        string textoRtf = System.Text.Encoding.GetEncoding(1252).GetString(registro.ConteudoBinario);

                        // Decodificar escapes RTF acentuados (\'XX → caractere Windows-1252)
                        textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\\'([0-9a-fA-F]{2})", match =>
                        {
                            byte b = Convert.ToByte(match.Groups[1].Value, 16);
                            return System.Text.Encoding.GetEncoding(1252).GetString(new[] { b });
                        });

                        // Strip RTF tags preservando texto
                        textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\{\\[^}]*\}", "");
                        textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\\par\b\s?", "\n");
                        textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\\[a-z]+\d*\s?", "");
                        textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"[{}]", "");
                        textoRtf = textoRtf.Trim();

                        // Converter quebras de linha em <br> para o Quill exibir corretamente
                        var linhas = textoRtf.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        conteudoHtml = string.Join("<br>", linhas.Where(l => !string.IsNullOrWhiteSpace(l)));
                    }
                    else
                    {
                        // DOCX ou outro formato binário — indicar que não é editável diretamente
                        conteudoHtml = "<p><em>[Conteúdo importado no formato " + registro.FormatoOrigem + ". Para editar, faça upload de uma nova versão.]</em></p>";
                    }
                }

                ViewBag.Registro = registro;
                ViewBag.ConteudoHtml = conteudoHtml;
                ViewBag.DescricaoExame = descricaoExame;
                ViewBag.Editando = true;
            }
            else
            {
                ViewBag.Registro = null;
                ViewBag.ConteudoHtml = "";
                ViewBag.DescricaoExame = "";
                ViewBag.Editando = false;
                ViewBag.ContaExamePre = contaExame;
                ViewBag.TabelaExamesIdPre = tabelaExamesId;

                // Pré-carregar a descrição do exame se temos contaExame + tabelaExamesId
                if (!string.IsNullOrWhiteSpace(contaExame) && tabelaExamesId.HasValue)
                {
                    ViewBag.DescricaoExame = _db.PlanoExames
                        .AsNoTracking()
                        .Where(p => p.ContaExame == contaExame && p.TabelaExamesId == tabelaExamesId.Value)
                        .Select(p => p.Descricao)
                        .FirstOrDefault() ?? "";
                }
            }

            return View("Editar");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Incluir")]
        public IActionResult Incluir()
        {
            return Editar(null);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Salvar")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Salvar([FromBody] SalvarReferenciaRequest request)
        {
            try
            {
                int id = request.Id;
                string contaExame = request.ContaExame ?? "";
                int tabelaExamesId = request.TabelaExamesId;
                string conteudoHtml = request.ConteudoHtml ?? "";
                int alinhaLaudo = request.AlinhaLaudo;

                string usuario = HttpContext.Session.GetString("SessionNome") ?? "sistema";
                DateTime agora = _geralService.ObterDataHoraUtc();
                byte[] conteudoBytes = System.Text.Encoding.UTF8.GetBytes(conteudoHtml);

                if (id > 0)
                {
                    var registro = await _db.ExameReferencia.FindAsync(id);
                    if (registro == null)
                        return Json(new { sucesso = false, mensagem = "Registro não encontrado." });

                    registro.ConteudoBinario = conteudoBytes;
                    registro.FormatoOrigem = "HTML";
                    registro.AlinhaLaudo = alinhaLaudo;
                    registro.DataAlteracao = agora;
                    registro.UsuarioAlteracao = usuario;
                    registro.Versao += 1;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(contaExame))
                        return Json(new { sucesso = false, mensagem = "Conta Exame é obrigatória." });

                    var novoRegistro = new ExameReferencia
                    {
                        ContaExame = contaExame.Trim(),
                        TabelaExamesId = tabelaExamesId,
                        ConteudoBinario = conteudoBytes,
                        FormatoOrigem = "HTML",
                        AlinhaLaudo = alinhaLaudo,
                        DataCriacao = agora,
                        DataAlteracao = agora,
                        UsuarioAlteracao = usuario,
                        Versao = 1
                    };
                    _db.ExameReferencia.Add(novoRegistro);
                }

                await _db.SaveChangesAsync();

                return Json(new { sucesso = true, mensagem = "Documento atualizado. Para que a alteração reflita na impressão de laudos, efetue login novamente." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ExameReferencia] Erro ao salvar: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao salvar: " + ex.Message });
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Upload")]
        public IActionResult Upload(int id, string contaExame, int tabelaExamesId, IFormFile arquivo)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                    return Json(new { sucesso = false, mensagem = "Nenhum arquivo selecionado." });

                string extensao = Path.GetExtension(arquivo.FileName).TrimStart('.').ToUpper();
                if (extensao != "DOC" && extensao != "DOCX" && extensao != "RTF")
                    return Json(new { sucesso = false, mensagem = "Formato inválido. Permitidos: .DOC, .DOCX, .RTF" });

                string usuario = HttpContext.Session.GetString("SessionNome") ?? "sistema";
                DateTime agora = _geralService.ObterDataHoraUtc();

                // Ler bytes do arquivo
                byte[] arquivoBytes;
                using (var ms = new MemoryStream())
                {
                    arquivo.CopyTo(ms);
                    arquivoBytes = ms.ToArray();
                }

                // Converter sempre para HTML (UTF-8) — DOC/DOCX/RTF são apenas para importação
                string htmlConvertido = ConversorDocParaHtml.Converter(extensao, arquivoBytes);
                byte[] conteudoBytes = System.Text.Encoding.UTF8.GetBytes(htmlConvertido);

                if (id > 0)
                {
                    var registro = _db.ExameReferencia.FirstOrDefault(r => r.Id == id);
                    if (registro == null)
                        return Json(new { sucesso = false, mensagem = "Registro não encontrado." });

                    registro.ConteudoBinario = conteudoBytes;
                    registro.FormatoOrigem = "HTML";
                    registro.DataAlteracao = agora;
                    registro.UsuarioAlteracao = usuario;
                    registro.Versao += 1;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(contaExame))
                        return Json(new { sucesso = false, mensagem = "Conta Exame é obrigatória." });

                    var novoRegistro = new ExameReferencia
                    {
                        ContaExame = contaExame.Trim(),
                        TabelaExamesId = tabelaExamesId,
                        ConteudoBinario = conteudoBytes,
                        FormatoOrigem = "HTML",
                        AlinhaLaudo = 0,
                        DataCriacao = agora,
                        DataAlteracao = agora,
                        UsuarioAlteracao = usuario,
                        Versao = 1
                    };
                    _db.ExameReferencia.Add(novoRegistro);
                }

                _db.SaveChanges();

                return Json(new { sucesso = true, mensagem = "Documento atualizado. Para que a alteração reflita na impressão de laudos, efetue login novamente." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ExameReferencia] Erro no upload: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro no upload: " + ex.Message });
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("Excluir")]
        public IActionResult Excluir(int id)
        {
            try
            {
                var registro = _db.ExameReferencia.FirstOrDefault(r => r.Id == id);
                if (registro == null)
                    return Json(new { sucesso = false, mensagem = "Registro não encontrado." });

                _db.ExameReferencia.Remove(registro);
                _db.SaveChanges();

                return Json(new { sucesso = true, mensagem = "Referência excluída com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[ExameReferencia] Erro ao excluir: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao excluir: " + ex.Message });
            }
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Visualizar")]
        public IActionResult Visualizar(int id)
        {
            var registro = _db.ExameReferencia
                .AsNoTracking()
                .FirstOrDefault(r => r.Id == id);

            if (registro == null)
                return Json(new { sucesso = false, mensagem = "Registro não encontrado." });

            string conteudo = "";
            if (registro.ConteudoBinario != null && registro.ConteudoBinario.Length > 0)
            {
                if (registro.FormatoOrigem == "HTML")
                {
                    conteudo = System.Text.Encoding.UTF8.GetString(registro.ConteudoBinario);
                }
                else
                {
                    conteudo = "<p><em>[Conteúdo no formato " + registro.FormatoOrigem + " — visualização não disponível no browser.]</em></p>";
                }
            }

            return Json(new { sucesso = true, conteudo, formato = registro.FormatoOrigem });
        }
    }

    /// <summary>
    /// Converte conteúdo binário de DOC/DOCX/RTF para HTML.
    /// O objetivo é evitar conteúdo ilegível (acentuação, caracteres ruins) e padronizar
    /// todos os laudos como HTML. DOC/DOCX são apenas para importação.
    /// </summary>
    public static class ConversorDocParaHtml
    {
        /// <summary>
        /// Converte o conteúdo binário para HTML UTF-8.
        /// </summary>
        public static string Converter(string extensao, byte[] conteudoBytes)
        {
            extensao = extensao.ToUpperInvariant();

            return extensao switch
            {
                "HTML" => System.Text.Encoding.UTF8.GetString(conteudoBytes),
                "RTF" => ConverterRtf(conteudoBytes),
                "DOCX" => ConverterDocx(conteudoBytes),
                "DOC" => ConverterDoc(conteudoBytes),
                _ => ""
            };
        }

        private static string ConverterRtf(byte[] conteudoBytes)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            string textoRtf = System.Text.Encoding.GetEncoding(1252).GetString(conteudoBytes);

            // Decodificar escapes RTF acentuados (\'XX -> caractere Windows-1252)
            textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\\'([0-9a-fA-F]{2})", match =>
            {
                byte b = Convert.ToByte(match.Groups[1].Value, 16);
                return System.Text.Encoding.GetEncoding(1252).GetString(new[] { b });
            });

            // Remover tags RTF preservando texto
            textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\{\\[^}]*\}", "");
            textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\\par\b\s?", "\n");
            textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"\\[a-z]+\d*\s?", "");
            textoRtf = System.Text.RegularExpressions.Regex.Replace(textoRtf, @"[{}]", "");
            textoRtf = textoRtf.Trim();

            // Converter quebras de linha em <br> para o Quill exibir corretamente
            var linhas = textoRtf.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            return string.Join("<br>", linhas.Where(l => !string.IsNullOrWhiteSpace(l)));
        }

        private static string ConverterDocx(byte[] conteudoBytes)
        {
            // DOCX é um arquivo ZIP contendo word/document.xml
            try
            {
                using (var ms = new MemoryStream(conteudoBytes))
                using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read))
                {
                    var entry = archive.GetEntry("word/document.xml");
                    if (entry == null)
                        return "";

                    string xml;
                    using (var stream = entry.Open())
                    using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                    {
                        xml = reader.ReadToEnd();
                    }

                    // Dividir por parágrafos (</w:p>) e extrair texto de <w:t> em cada parágrafo
                    var paragrafos = System.Text.RegularExpressions.Regex.Split(xml, @"</w:p>");
                    var linhasHtml = new List<string>();

                    foreach (var paragrafo in paragrafos)
                    {
                        var textos = System.Text.RegularExpressions.Regex.Matches(paragrafo, @"<w:t[^>]*>([^<]*)</w:t>");
                        var sb = new System.Text.StringBuilder();
                        foreach (System.Text.RegularExpressions.Match m in textos)
                        {
                            sb.Append(m.Groups[1].Value);
                        }

                        string linha = sb.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(linha))
                        {
                            linhasHtml.Add("<p>" + System.Net.WebUtility.HtmlEncode(linha) + "</p>");
                        }
                    }

                    return string.Join("", linhasHtml);
                }
            }
            catch
            {
                // Se a extração falhar, retornar HTML vazio
                return "";
            }
        }

        private static string ConverterDoc(byte[] conteudoBytes)
        {
            // DOC binário (formato legado) — não há parser nativo sem biblioteca de terceiros.
            // Tentar extrair sequências legíveis de texto, descartando dados binários.
            // Se o resultado for confiável, converter para HTML; caso contrário, retornar vazio.
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                string raw = System.Text.Encoding.GetEncoding(1252).GetString(conteudoBytes);

                // Extrair sequências de caracteres legíveis (ASCII imprimível + Latin-1)
                // Mínimo de 4 caracteres consecutivos para considerar texto válido
                var matches = System.Text.RegularExpressions.Regex.Matches(raw, @"[\x20-\x7E\xA0-\xFF]{4,}");
                var linhasHtml = new List<string>();

                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    string texto = m.Value.Trim();
                    // Filtrar sequências que parecem binário (muitos caracteres não alfabéticos)
                    int alfaCount = texto.Count(c => char.IsLetterOrDigit(c) || c == ' ' || c == '.' || c == ',' || c == '-');
                    if (!string.IsNullOrWhiteSpace(texto) && alfaCount >= texto.Length / 2)
                    {
                        linhasHtml.Add("<p>" + System.Net.WebUtility.HtmlEncode(texto) + "</p>");
                    }
                }

                // Se conseguimos extrair pelo menos 3 linhas, é provável que seja texto válido
                if (linhasHtml.Count >= 3)
                    return string.Join("", linhasHtml);

                // Caso contrário, retornar vazio (evita conteúdo ilegível)
                return "";
            }
            catch
            {
                return "";
            }
        }
    }

    public class SalvarReferenciaRequest
    {
        public int Id { get; set; }
        public string? ContaExame { get; set; }
        public int TabelaExamesId { get; set; }
        public string? ConteudoHtml { get; set; }
        public int AlinhaLaudo { get; set; }
    }
    //..Kiro
}
