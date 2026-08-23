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
    //Feito pelo Kiro em 14/06/2026
    [Route("Manutencao")]
    public class ManutencaoController : BaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly IGeralService _geralService;

        public ManutencaoController(
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

        //Feito pelo Qoder em 23/08/2026 — REMOVIDOS os endpoints CompactarRequisicoes/ContarRequisicoes/
        //ExecutarCompactacao e a view CompactarRequisicoes.cshtml: a tabela Requisitar foi eliminada
        //e a tela de compactação perdeu o sentido. O item de menu correspondente (ControleDePerfilMenu)
        //é removido por SQL no banco de cada cliente.

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
                var importador = new ImportadorReferenciaExames(_db, _env, _geralService);
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
