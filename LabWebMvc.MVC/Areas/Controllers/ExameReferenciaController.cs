using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
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

        public ExameReferenciaController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IWebHostEnvironment env)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _env = env;
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
        public IActionResult Editar(int? id)
        {
            ViewBag.TextoMenu = new object[] { "Plano de Exames", false };

            var tabelas = _db.TabelaExames
                .OrderBy(t => t.NomeTabela)
                .Select(t => new { t.Id, t.NomeTabela })
                .ToList();
            ViewBag.Tabelas = tabelas;

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
            }

            return View();
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
                DateTime agora = _geralController.ObterDataHoraUtc();
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
                DateTime agora = _geralController.ObterDataHoraUtc();

                byte[] conteudoBytes;
                using (var ms = new MemoryStream())
                {
                    arquivo.CopyTo(ms);
                    conteudoBytes = ms.ToArray();
                }

                if (id > 0)
                {
                    var registro = _db.ExameReferencia.FirstOrDefault(r => r.Id == id);
                    if (registro == null)
                        return Json(new { sucesso = false, mensagem = "Registro não encontrado." });

                    registro.ConteudoBinario = conteudoBytes;
                    registro.FormatoOrigem = extensao;
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
                        FormatoOrigem = extensao,
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
