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
    //Feito pelo Kiro em 14/06/2026
    [Route("Manutencao")]
    public class ManutencaoController : BaseController
    {
        private readonly IWebHostEnvironment _env;

        public ManutencaoController(
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
        [Route("CompactarRequisicoes")]
        public IActionResult CompactarRequisicoes()
        {
            ViewBag.TextoMenu = new object[] { "Manutenção", false };
            return View();
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ContarRequisicoes")]
        //Feito pelo Qoder em 12/08/2026 — desativado: tabela Requisitar eliminada.
        // Mantido o endpoint para compatibilidade com a interface de manutenção.
        public IActionResult ContarRequisicoes(string? dataLimite)
        {
            return Json(new { sucesso = true, total = 0, mensagem = "Compactação de requisições desativada (tabela Requisitar eliminada)." });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ExecutarCompactacao")]
        //Feito pelo Qoder em 12/08/2026 — desativado: tabela Requisitar eliminada.
        public IActionResult ExecutarCompactacao(string? dataLimite)
        {
            return Json(new { sucesso = true, totalRemovido = 0, mensagem = "Compactação de requisições desativada (tabela Requisitar eliminada)." });
        }

        //Feito pelo Kiro em 03/07/2026
        [TypeFilter(typeof(SessionFilter))]
        [HttpGet]
        [Route("ImportarReferencias")]
        public IActionResult ImportarReferenciasView()
        {
            ViewBag.TextoMenu = new object[] { "Carga de Dados", false };
            return View("ImportarReferencias");
        }
        //..Kiro

        //Feito pelo Kiro em 11/07/2025
        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ImportarReferencias")]
        public async Task<IActionResult> ImportarReferencias(string? pastaOrigem)
        {
            try
            {
                var importador = new ImportadorReferenciaExames(_db, _env, _geralController);
                var resultado = await importador.ExecutarAsync(pastaOrigem);
                return Json(new { sucesso = true, resultado });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Manutenção] Erro na importação de referências: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao importar referências. Detalhe: " + ex.Message });
            }
        }
        //..Kiro
    }
    //..Kiro
}
