using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Servicos;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using LabWebMvc.MVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class GeralController : Controller
    {
        private readonly Db _db;
        private readonly IValidadorDeSessao _validador;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITempoServidorService _tempoService;
        private readonly IGeralService _geralService;

        // Acessa o HttpContext atual dinamicamente
        private HttpContext? _httpContext => _httpContextAccessor.HttpContext;
        public GeralController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            IHttpContextAccessor httpContextAccessor,
            ITempoServidorService tempoService,
            IGeralService geralService)
        {
            _db = dbFactory.Create();
            _validador = validador;
            _httpContextAccessor = httpContextAccessor;
            _tempoService = tempoService;
            _geralService = geralService;
        }


        public IActionResult GeralAcoes()
        {
            return View();
        }

        public IActionResult ConstroiBotoes()
        {
            string[] buttons =
            {
            "<input type='submit' name='b1' id='b1' class='subbotao' value='Adicionar' onclick='alert(this.value)'/>",
            "<input type='button' name='b2' id='b2' class='subbotao' value='Excluir' onclick='alert(this.value)' />",
            "<input type='button' name='b3' id='b3' class='subbotao' value='Enviar senha por email' onclick='alert(this.value)'/>",
            "<input type='button' name='b4' id='b4' class='subbotao' value='Ver exames' onclick='alert(this.value)' />",
            "<input type='button' name='b5' id='b5' class='subbotao' value='Arquivar' onclick='alert(this.value)' />"
            };
            ViewBag.ViewBotoes = buttons;

            return View();
        }

        /*
         * MÉTODOS DE VALIDAÇÃO GENÉRICOS, QUE VALIDAM SE UMA VIEW FOI CHAMADO COM O USUÁRIO LOGADO!
         * PARA ATENDER A DIVERSAS POSSIBILIDADES AO CHAMAR UMA VIEW E PASSAR OS PARÂMETROS NECESSÁRIOS PARA CARREGAR O HTML COM OS DADOS
         * VALIDAÇÃO DA SESSION PARA SABER SE O USUÁRIO ESTÁ REALMENTE LOGADO PARA PODER ACESSAR AS TELAS DO SISTEMA
         *
         */

        //[TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public IActionResult Validacao(string retornoDeRota, string titulo)  //Esta validação é boa e é a mais simples
        {
            //exemplos de retornoDeRota: "Index,Pacientes" ou "Index,Medicos" etc.
            ViewBag.TextoMenu = new object[] { titulo, false };
            ViewBag.SessionUF = Convert.ToString(_httpContext!.Session.GetString("SessionUF"));

            if (_validador.SessaoValida())
            {
                return View();
            }
            else
                return Json(new { titulo = MensagensError_pt_BR.ErroPagina, mensagem = "A sessão não foi validada", action = "", sucesso = false });
        }

        //Sobrescrito
        [TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public IActionResult Validacao(string retornoDeRota, string titulo, dynamic itensView, string? partialView = null)  //TANTO FAZ: ViewBag ou ViewModel como parâmetro, ambos são aceitos!
        {
            //exemplos de retornoDeRota: "Index,Pacientes" ou "Index,Medicos" etc.
            ViewBag.TextoMenu = new object[] { titulo, false };

            /* Cada HTML saberá o que está vindo nesta ViewBag genérica com os dados de "itensView"!
             */
            ViewBag.Itens = itensView;

            if (_validador.SessaoValida())
            {
                if (!string.IsNullOrEmpty(partialView))
                    return PartialView(partialView);  //nos casos em que temos uma partial view num grid/table
                else
                    return View();
            }
            return RedirectToAction("AcessoValidado", "Mensagem", new { retornoDeRota = retornoDeRota });
        }

        //Sobrescrito
        [TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public IActionResult Validacao(string retornoDeRota, string titulo, int totalRegistros = 0, int totalTabela = 0, ICollection<dynamic>? listaGrid = null, string? partialView = null)
        {
            //exemplos de retornoDeRota: "Index,Pacientes" ou "Index,Medicos" etc.
            ViewBag.TextoMenu = new object[] { titulo, false };
            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = listaGrid;
            ViewBag.SessionUF = Convert.ToString(_httpContext!.Session.GetString("SessionUF"));

            if (_validador.SessaoValida())
            {
                if (!string.IsNullOrEmpty(partialView))
                    return View(partialView);  //nos casos em que temos uma partial view num grid/table
                else
                    return View();
            }
            return RedirectToAction("AcessoValidado", "Mensagem", new { retornoDeRota = retornoDeRota });
        }

        //Sobrescrito
        //VALIDAÇÃO EXCLUSIVA PARA O PLANO DE ITENS DE EXAMES:
        [TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public IActionResult Validacao(string retornoDeRota, string titulo, int totalRegistros = 0, int totalTabela = 0, ICollection<PlanoExames>? dados = null, string? partialView = null)
        {
            //exemplos de retornoDeRota: "Index,Pacientes" ou "Index,Medicos" etc.
            ViewBag.TextoMenu = new object[] { titulo, false };
            ViewBag.TotalRegistros = totalRegistros.ToString();
            ViewBag.TotalTabela = totalTabela.ToString();
            ViewBag.ListaDados = dados;
            ViewBag.SessionUF = Convert.ToString(_httpContext!.Session.GetString("SessionUF"));

            if (_validador.SessaoValida())
            {
                if (!string.IsNullOrEmpty(partialView))
                    return PartialView(partialView);  //nos casos em que temos uma partial view num grid/table
                else
                    return View();
            }
            return RedirectToAction("AcessoValidado", "Mensagem", new { retornoDeRota = retornoDeRota });
        }

        //Feito pelo Qoder em 12/08/2026 — removida sobrecarga com ICollection<Requisitar> (tabela eliminada).
        // A chamada no RequisitarController.Index agora utiliza a sobrecarga com ICollection<PlanoExames>.

        [HttpGet]
        [TypeFilter(typeof(SessionFilter))]
        public IActionResult ValidacaoGenerica<T>(vmListaValidacao<T> vm)
        {
            ViewBag.TextoMenu = new object[] { vm.Titulo, false };
            ViewBag.TotalRegistros = vm.TotalRegistros.ToString();
            ViewBag.TotalTabela = vm.TotalTabela.ToString();
            ViewBag.ListaDados = vm.ListaDados;
            ViewBag.SessionUF = Convert.ToString(_httpContext!.Session.GetString("SessionUF"));

            if (_validador.SessaoValida())
            {
                if (!string.IsNullOrEmpty(vm.PartialView))
                    return View(vm.PartialView);  //nos casos em que temos uma partial view num grid/table
                else
                    return View();
            }
            return RedirectToAction("AcessoValidado", "Mensagem", new { retornoDeRota = vm.RetornoDeRota });
        }

        /* FIM DOS MÉTODOS DE VALIDAÇÃO E CHAMAMENTO DAS VIEWS  */


        /* Métodos Genéricos de Pesquisas dinâmicas em consultas  */
        /*
               Com usar:
                         dados = await dbE.ClasseExames
                                 .AsNoTracking()
                                 .FiltrarPorConteudo(Conteudo, x => x.RefExame, x => x.Id.ToString())
                                 .OrderByDescending(x => x.Id)
                                 .ToListAsync();

         */

        [TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public async Task<IActionResult> ObterDataHoraServidorView()
        {
            string data = await _tempoService.ObterDataHoraServidorFormatadoAsync(); // ou ("iso")

            // usar a variável normalmente
            ViewBag.DataHora = data;

            return View();
        }

        // Feito pelo Qoder em 22/08/2026 — Dívida Técnica §1 (opção A): os utilitários de data/hora/fuso
        // foram extraídos para IGeralService/GeralService (Areas\Servicos), que é registrado no DI e pode
        // ser injetado em qualquer classe sem infraestrutura MVC. Este controller agora apenas delega,
        // mantendo as assinaturas públicas para compatibilidade retroativa.
        [TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public string ObterDataHoraServidor(bool iso = false) => _geralService.ObterDataHoraServidor(iso);

        /// <summary>
        /// Retorna DateTime UTC para uso em persistência.
        /// Fonte: PostgreSQL (NOW()). Fallback: DateTime.UtcNow.
        /// NUNCA use DateTime.Now ou dados do cliente para timestamps de criacao.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        public DateTime ObterDataHoraUtc() => _geralService.ObterDataHoraUtc();

        /// <summary>
        /// Retorna DateTime no timezone local (America/Sao_Paulo) com Kind=Unspecified.
        /// Uso: exibição e lógica de negócio local.
        /// NÃO use para persistência — use ObterDataHoraUtc() para isso.
        /// NÃO use como parâmetro de query EF Core com colunas timestamptz — use ObterRangeDiaUtc().
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        public DateTime ObterDataHoraLocal() => _geralService.ObterDataHoraLocal();

        /// <summary>
        /// Retorna o range do dia atual em UTC (Kind=Utc), pronto para uso em
        /// queries EF Core que comparam com colunas timestamptz.
        /// 
        /// IMPORTANTE: No Npgsql 8.x (sem legacy behavior), DateTimeKind.Unspecified
        /// causa InvalidOperationException ao comparar com timestamptz.
        /// Este método converte meia-noite local (America/Sao_Paulo) para UTC corretamente.
        /// </summary>
        [TypeFilter(typeof(SessionFilter))]
        public (DateTime inicioUtc, DateTime fimUtc) ObterRangeDiaUtc() => _geralService.ObterRangeDiaUtc();

        /// <summary>
        /// Converte uma data local (meia-noite America/Sao_Paulo) para range UTC (Kind=Utc),
        /// pronto para uso em queries EF Core com colunas timestamptz.
        /// 
        /// Use este método quando o filtro vem de input do usuário (string dd/MM/yyyy)
        /// ou de DateTime.Parse. NUNCA passe DateTime.Kind=Unspecified diretamente ao PostgreSQL.
        /// 
        /// Exemplo: dataLocal = 2026-05-03 00:00:00 (Brasília)
        ///          inicioUtc = 2026-05-03 03:00:00 UTC
        ///          fimUtc    = 2026-05-04 02:59:59 UTC
        /// </summary>
        public (DateTime inicioUtc, DateTime fimUtc) ConverterDataLocalParaRangeUtc(DateTime dataLocal) => _geralService.ConverterDataLocalParaRangeUtc(dataLocal);

        /// <summary>
        /// Converte um DateTime local (America/Sao_Paulo, Kind=Unspecified) para UTC (Kind=Utc),
        /// pronto para gravação em colunas timestamptz.
        /// 
        /// Use quando um valor de data/hora vem do cliente (ex: DataEntregaParcial)
        /// e precisa ser persistido como UTC.
        /// </summary>
        public DateTime ConverterLocalParaUtc(DateTime dataLocal) => _geralService.ConverterLocalParaUtc(dataLocal);
    }
}