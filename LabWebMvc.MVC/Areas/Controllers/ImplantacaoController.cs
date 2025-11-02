using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.Genericos;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Integracoes.Importacao;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using static BLL.UtilBLL;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class ImplantacaoController : BaseController
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MovimentacaoImportacao _movimentacaoImportacao;

        public ImplantacaoController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IHttpContextAccessor httpContextAccessor,
            MovimentacaoImportacao movimentacaoImportacao)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService)
        {
            _httpContextAccessor = httpContextAccessor;
            _movimentacaoImportacao = movimentacaoImportacao;
        }

        private void MontaControllers(string action, string controller, string parametros = "")
        {
            PartialFiltro.Action = action;
            PartialFiltro.Controller = controller;
            PartialFiltro.ActionButton = action + parametros;
            PartialFiltro.ControllerButton = controller;
            PartialFiltro.Esconde = false;
            ViewBag.TextoMenu = action.MensagemStartUp();
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("Implantacao")]
        public IActionResult Index(string? Conteudo, int registros = 50)
        {
            // Finalização da View
            return _geralController.Validacao(
                "Index",
                @"Carga de Dados : Implantação : Lista das Tabelas que podem ser importadas (Local: \Temp\Importacao\)");
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("CarregarImplantacao")]
        public IActionResult CarregarImplantacao(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new
                {
                    titulo = MensagensError_pt_BR.ErroFalhou,
                    mensagem = "ID da tabela não informado.",
                    action = "",
                    sucesso = false
                });
            }

            var param = new MovimentacaoImportacaoParameter
            {
                NomeTabela = id
            };

            _movimentacaoImportacao.ProcessaMovimentacao(param);

            var mensagem = $"Processo de carga de dados na implantação da tabela [{id}] concluído";

            return Json(new
            {
                titulo = Mensagens_pt_BR.Sucesso,
                mensagem,
                action = "",
                sucesso = true
            });
        }
    }
}
