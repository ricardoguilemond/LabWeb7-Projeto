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
        public ManutencaoController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
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
        public async Task<IActionResult> ContarRequisicoes(string? dataLimite)
        {
            DateTime limite;
            if (!string.IsNullOrWhiteSpace(dataLimite))
                limite = dataLimite.Trim().FormataData("dd/MM/yyyy", true);
            else
                limite = DateTime.Today.AddMonths(-12);

            var (_, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(limite);

            var idsExamesAtivos = _db.ExamesRealizados.Select(e => e.Id);

            int total = await _db.Requisitar
                .Where(r => r.DataIni <= fimUtc
                    && (r.ExameRealizadoId == null || !idsExamesAtivos.Contains(r.ExameRealizadoId.Value)))
                .CountAsync();

            return Json(new { sucesso = true, total });
        }

        [TypeFilter(typeof(SessionFilter))]
        [HttpPost]
        [Route("ExecutarCompactacao")]
        public async Task<IActionResult> ExecutarCompactacao(string? dataLimite)
        {
            try
            {
                DateTime limite;
                if (!string.IsNullOrWhiteSpace(dataLimite))
                    limite = dataLimite.Trim().FormataData("dd/MM/yyyy", true);
                else
                    limite = DateTime.Today.AddMonths(-12);

                var (_, fimUtc) = _geralController.ConverterDataLocalParaRangeUtc(limite);

                var idsExamesAtivos = _db.ExamesRealizados.Select(e => e.Id);

                int totalRemovido = 0;
                int lote;

                do
                {
                    var registros = await _db.Requisitar
                        .Where(r => r.DataIni <= fimUtc
                            && (r.ExameRealizadoId == null || !idsExamesAtivos.Contains(r.ExameRealizadoId.Value)))
                        .Take(1000)
                        .ToListAsync();

                    lote = registros.Count;
                    if (lote > 0)
                    {
                        _db.Requisitar.RemoveRange(registros);
                        await _db.SaveChangesAsync();
                        totalRemovido += lote;
                    }
                } while (lote == 1000);

                _eventLogHelper.LogEventViewer($"[Manutenção] Compactação de Requisições: {totalRemovido} registros removidos até {limite:dd/MM/yyyy}", "wInformation");

                return Json(new { sucesso = true, totalRemovido, mensagem = $"{totalRemovido} registros compactados com sucesso." });
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer("[Manutenção] Erro na compactação: " + ex.Message, "wError");
                return Json(new { sucesso = false, mensagem = "Erro ao compactar requisições. Detalhe: " + ex.Message });
            }
        }
    }
    //..Kiro
}
