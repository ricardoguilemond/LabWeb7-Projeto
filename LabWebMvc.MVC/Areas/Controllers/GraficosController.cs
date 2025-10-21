using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.Controllers;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.IndicadoresGraficos.Shared;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class GraficosController : BaseController
{
    public GraficosController(
        IDbFactory dbFactory,
        IValidadorDeSessao validador,
        GeralController geralController,
        IEventLogHelper eventLogHelper,
        Imagem imagem)
        : base(dbFactory, validador, geralController, eventLogHelper, imagem)
    {
    }

    public async Task<IActionResult> GraficoReCaptcha()
    {
        var dados = await _db.ReCaptchaMonitoramento
            .OrderBy(x => x.AnoReferencia)
            .ThenBy(x => x.MesReferencia)
            .ToListAsync();

        //var model = new vmGraficoReCaptcha();
        var model = new GraficoModel();

        foreach (var item in dados)
        {
            string label = $"{item.MesReferencia:00}/{item.AnoReferencia}";
            model.Labels.Add(label);
            model.Valores.Add(item.QuantidadeSolicitacoes);
        }
        ViewBag.TextoMenu = new List<string> { "Gráfico ReCaptcha" };

        var agora = DateTime.Now;

        var graficoModel = new GraficoModel
        {
            CanvasId = model.CanvasId,
            Titulo = "Solicitações Google ReCaptcha por Mês",
            Subtitulo = "Dados coletados a partir de 2025",
            Labels = model.Labels,
            Valores = model.Valores,
            TipoGrafico = "line",
            RodapeTextoPrincipal = "Fonte: Dados internos",
            RodapeSubtexto = "Atualizado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            RodapeIconeCss = "fa fa-info-circle",
            RodapeLinkTexto = "Ver detalhes sobre custos para ReCaptcha Enterprise",
            RodapeLinkUrl = @"https://cloud.google.com/recaptcha/docs/compare-tiers?hl=pt-br",
            RodapeMostrarAlerta = false
        };
        ViewBag.GraficoModel = graficoModel;

        return View(model);
    }
}