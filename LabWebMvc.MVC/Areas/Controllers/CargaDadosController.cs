using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Integracoes.Importacao;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel.CargaDados;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class CargaDadosController : BaseController
    {
        private readonly IFirebirdImporter _firebirdImporter;
        private readonly ISchemaComparer _schemaComparer;
        private readonly ICargaDadosExecutor _executor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CargaDadosController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IFirebirdImporter firebirdImporter,
            ISchemaComparer schemaComparer,
            ICargaDadosExecutor executor,
            IHttpContextAccessor httpContextAccessor)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _firebirdImporter = firebirdImporter;
            _schemaComparer = schemaComparer;
            _executor = executor;
            _httpContextAccessor = httpContextAccessor;
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CargaDados")]
        public IActionResult Index()
        {
            return RedirectToAction("Conexao");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CargaDados/Conexao")]
        public IActionResult Conexao()
        {
            ViewBag.TextoMenu = "Carga de Dados : Conexão Firebird";
            //Feito pelo Kiro em 26/07/2026 — Fix bug encoding acentuação importação Firebird
            return View(new FirebirdConnectionViewModel
            {
                Usuario = "SYSDBA",
                Senha = "Sucesso105",
                Servidor = "localhost",
                Porta = 3051,
                Charset = "NONE",
                TamanhoLote = 2500,
                ModoSimulacao = true,
                CaminhoBanco = @"F:\x-Web7\DadosLabWeb7\DB_CONDELAB.FDB"
            });
            //..Kiro
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CargaDados/Conexao")]
        public async Task<IActionResult> Conexao(FirebirdConnectionViewModel viewModel)
        {
            ViewBag.TextoMenu = "Carga de Dados : Conexão Firebird";

            if (!ModelState.IsValid)
                return View(viewModel);

            bool conectou = false;
            string mensagemErro = string.Empty;
            string connStr;

            if (viewModel.UsarODBC)
            {
                connStr = _firebirdImporter.MontarStringConexaoODBC(viewModel);
                (conectou, mensagemErro) = await _firebirdImporter.TestarConexaoODBCAsync(connStr);

                if (!conectou)
                {
                    viewModel.Senha = "masterkey";
                    connStr = _firebirdImporter.MontarStringConexaoODBC(viewModel);
                    (conectou, mensagemErro) = await _firebirdImporter.TestarConexaoODBCAsync(connStr);
                }
            }
            else
            {
                connStr = _firebirdImporter.MontarStringConexao(viewModel);
                (conectou, mensagemErro) = await _firebirdImporter.TestarConexaoAsync(connStr);

                if (!conectou)
                {
                    viewModel.Senha = "masterkey";
                    connStr = _firebirdImporter.MontarStringConexao(viewModel);
                    (conectou, mensagemErro) = await _firebirdImporter.TestarConexaoAsync(connStr);
                }
            }

            if (!conectou)
            {
                ModelState.AddModelError("", $"Não foi possível conectar ao banco Firebird. Verifique servidor, porta, caminho e senha.\\n\\nErro: {mensagemErro}");
                return View(viewModel);
            }

            HttpContext.Session.SetString("CargaDados_FirebirdConnectionString", connStr);
            HttpContext.Session.SetInt32("CargaDados_TamanhoLote", viewModel.TamanhoLote);
            HttpContext.Session.SetString("CargaDados_ModoSimulacao", viewModel.ModoSimulacao ? "1" : "0");
            HttpContext.Session.SetString("CargaDados_UsarODBC", viewModel.UsarODBC ? "1" : "0");

            return RedirectToAction("Tabelas");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CargaDados/Tabelas")]
        public IActionResult Tabelas()
        {
            ViewBag.TextoMenu = "Carga de Dados : Seleção de Tabelas";

            string? connStr = HttpContext.Session.GetString("CargaDados_FirebirdConnectionString");
            if (string.IsNullOrEmpty(connStr))
                return RedirectToAction("Conexao");

            var tabelas = FirebirdImporter.ObterTabelasSuportadas()
                .Select((t, idx) => new TabelaSelecaoViewModel
                {
                    NomeFirebird = t.Firebird,
                    NomePostgreSQL = t.Postgres,
                    Descricao = t.Descricao,
                    Ordem = idx,
                    Selecionada = true
                })
                .ToList();

            var model = new SelecaoTabelasViewModel
            {
                Tabelas = tabelas,
                StringConexaoFirebird = connStr,
                TamanhoLote = HttpContext.Session.GetInt32("CargaDados_TamanhoLote") ?? 1000,
                ModoSimulacao = HttpContext.Session.GetString("CargaDados_ModoSimulacao") == "1"
            };

            return View(model);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CargaDados/Tabelas")]
        public IActionResult Tabelas(SelecaoTabelasViewModel viewModel)
        {
            if (viewModel.Tabelas == null || !viewModel.Tabelas.Any(t => t.Selecionada))
            {
                ModelState.AddModelError("", "Selecione pelo menos uma tabela para importar.");
                return View(viewModel);
            }

            var selecionadas = viewModel.Tabelas
                .Where(t => t.Selecionada)
                .OrderBy(t => t.Ordem)
                .Select(t => t.NomeFirebird)
                .ToList();

            HttpContext.Session.SetString("CargaDados_TabelasSelecionadas", string.Join(",", selecionadas));

            return RedirectToAction("Estimativa");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CargaDados/Estimativa")]
        public async Task<IActionResult> Estimativa()
        {
            ViewBag.TextoMenu = "Carga de Dados : Estimativa";

            string? connStr = HttpContext.Session.GetString("CargaDados_FirebirdConnectionString");
            string? tabelasStr = HttpContext.Session.GetString("CargaDados_TabelasSelecionadas");
            int tamanhoLote = HttpContext.Session.GetInt32("CargaDados_TamanhoLote") ?? 1000;
            bool modoSimulacao = HttpContext.Session.GetString("CargaDados_ModoSimulacao") == "1";

            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(tabelasStr))
                return RedirectToAction("Conexao");

            var tabelas = tabelasStr.Split(',').ToList();
            string postgresConnStr = _connectionServiceBase.GetConnectionString();

            var estimativa = await _firebirdImporter.GerarEstimativaAsync(connStr, postgresConnStr, tabelas, tamanhoLote, modoSimulacao);

            return View(estimativa);
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CargaDados/Importar")]
        public IActionResult Importar(string connectionId, bool executarBackup = false)
        {
            string? connStr = HttpContext.Session.GetString("CargaDados_FirebirdConnectionString");
            string? tabelasStr = HttpContext.Session.GetString("CargaDados_TabelasSelecionadas");
            int tamanhoLote = HttpContext.Session.GetInt32("CargaDados_TamanhoLote") ?? 1000;
            bool modoSimulacao = HttpContext.Session.GetString("CargaDados_ModoSimulacao") == "1";

            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(tabelasStr))
                return Json(new { sucesso = false, mensagem = "Configuração de importação não encontrada." });

            var configuracao = new ImportacaoConfiguracao
            {
                StringConexaoFirebird = connStr,
                TabelasSelecionadas = tabelasStr.Split(',').ToList(),
                TamanhoLote = tamanhoLote,
                ModoSimulacao = modoSimulacao,
                ConnectionId = connectionId ?? ""
            };

            string postgresConnStr = _connectionServiceBase.GetConnectionString();
            string chave = _executor.IniciarImportacao(configuracao, postgresConnStr);

            return Json(new { sucesso = true, chave });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CargaDados/Status")]
        public IActionResult Status(string chave)
        {
            var status = _executor.ObterStatus(chave);
            if (status == null)
                return Json(new { sucesso = false, mensagem = "Importação não encontrada." });

            return Json(new
            {
                sucesso = true,
                status.EmExecucao,
                status.Concluido,
                status.Erro,
                status.MensagemErro,
                status.AguardandoDecisao,
                status.TabelaComErro,
                status.DetalheErro,
                progresso = status.Progresso,
                resultado = status.Resultado
            });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("CargaDados/Decisao")]
        public IActionResult Decisao(string chave, bool ignorar)
        {
            _executor.DefinirDecisao(chave, ignorar);
            return Json(new { sucesso = true });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CargaDados/Importar")]
        public IActionResult Importar()
        {
            ViewBag.TextoMenu = "Carga de Dados : Importação";
            return View();
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CargaDados/ListarArquivos")]
        public IActionResult ListarArquivos(string caminho)
        {
            try
            {
                // Lista de drives quando solicitado explicitamente ou quando não há caminho
                if (string.IsNullOrWhiteSpace(caminho) || caminho.Equals("__drives__", StringComparison.OrdinalIgnoreCase))
                {
                    var drives = DriveInfo.GetDrives()
                        .Where(d => d.IsReady)
                        .Select(d => new { nome = d.Name, caminho = d.RootDirectory.FullName })
                        .OrderBy(d => d.nome)
                        .ToList();

                    return Json(new { sucesso = true, diretorios = drives, arquivos = new List<object>(), caminho = "Drives", ehRaiz = true });
                }

                caminho = Path.GetFullPath(caminho);

                if (!Directory.Exists(caminho))
                    return Json(new { sucesso = false, mensagem = "Caminho não encontrado." });

                var diretorios = Directory.GetDirectories(caminho)
                    .Select(d => new { nome = Path.GetFileName(d), caminho = d })
                    .OrderBy(d => d.nome)
                    .ToList();

                var arquivos = Directory.GetFiles(caminho, "*.fdb")
                    .Select(f => new { nome = Path.GetFileName(f), caminho = f })
                    .OrderBy(f => f.nome)
                    .ToList();

                bool ehRaiz = Path.GetPathRoot(caminho)?.Equals(caminho, StringComparison.OrdinalIgnoreCase) ?? false;

                return Json(new { sucesso = true, diretorios, arquivos, caminho, ehRaiz });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = ex.Message });
            }
        }
    }
}
