using BLL;
using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
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

        // Acessa o HttpContext atual dinamicamente
        private HttpContext? _httpContext => _httpContextAccessor.HttpContext;
        public GeralController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            IHttpContextAccessor httpContextAccessor,
            ITempoServidorService tempoService)
        {
            _db = dbFactory.Create();
            _validador = validador;
            _httpContextAccessor = httpContextAccessor;
            _tempoService = tempoService;
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

        //Sobrescrito
        //VALIDAÇÃO EXCLUSIVA PARA REQUISIÇÃO ORIGINAL - LANÇAR EXAMES DOS PACIENTES:
        [TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public IActionResult Validacao(string retornoDeRota, string titulo, int totalRegistros = 0, int totalTabela = 0, ICollection<Requisitar>? dados = null, string? partialView = null)
        {
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

        [HttpGet]
        [TypeFilter(typeof(SessionFilter))]
        public IActionResult ValidacaoGenerica<T>(vmListaValidacao<T> vm)
        {
            ViewBag.TextoMenu = new object[] { vm.Titulo, false };
            ViewBag.TotalRegistros = vm.TotalRegistros.ToString();
            ViewBag.TotalTabela = vm.TotalTabela.ToString();
            ViewBag.ListaDados = vm.ListaDados;
            ViewBag.SessionUF = Convert.ToString(_httpContext!.Session.GetString("SessionUF"));

            return string.IsNullOrEmpty(vm.PartialView) 
                ? View(vm.RetornoDeRota, vm) 
                : PartialView(vm.PartialView, vm);
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

        [TypeFilter(typeof(SessionFilter))]  //observar a classe ValidacoesDeSessao que iniciou essa tratativa aqui.
        public string ObterDataHoraServidor(bool iso = false)
        {
            if (iso)
                return _tempoService.ObterDataHoraServidor("iso");  //yyyy/mm/ddTHH:mm:ss.fffZ
            else
                return _tempoService.ObterDataHoraServidor();       //dd/mm/yyyy HH:mm:ss
        }
    }
}