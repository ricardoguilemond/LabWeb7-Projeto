﻿using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Interfaces.Criptografias;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class ReCaptchaTrackerController : BaseController
    {
        private readonly GoogleReCaptchaSettings _captchaSettings;
        private readonly IReCaptchaMetricasService _metricasService;

        public ReCaptchaTrackerController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            ExclusaoService exclusaoService,
            IConnectionService connectionService,
            IOptions<GoogleReCaptchaSettings> captchaSettings,
            IReCaptchaMetricasService metricasService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem, exclusaoService, connectionService)
        {
            _captchaSettings = captchaSettings.Value;
            _metricasService = metricasService;
        }

        public class ReCaptchaLimiteResult
        {
            public bool Sucesso { get; set; }
            public string? Titulo { get; set; }
            public string? Mensagem { get; set; }
            public bool PrecisaConfirmacao { get; set; }
        }

        public ReCaptchaLimiteResult RegistrarSolicitacaoReCaptcha(string nomeProjeto)
        {
            // Sempre registra a tentativa, independentemente do sucesso da verificação de limite, pois o Google registra todas as tentativas!
            try
            {
                LabWebMvc.MVC.Areas.Utils.Utils.RegistrarSolicitacaoReCaptcha(_db, nomeProjeto);
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer($"Erro ao registrar solicitação ReCaptcha: {ex.Message}", "wError");
                // Ainda retorna resultado, mas registra erro de log
            }

            // Valida limites após o registro
            ReCaptchaLimiteResult res = VerificarReCaptchaLimite(nomeProjeto);

            if (res.Sucesso == false && res.PrecisaConfirmacao == true)
            {
                return new ReCaptchaLimiteResult
                {
                    Sucesso = false,
                    Titulo = res.Titulo,
                    Mensagem = res.Mensagem,
                    PrecisaConfirmacao = true
                };
            }

            return new ReCaptchaLimiteResult { Sucesso = true };
        }

        [HttpGet]
        [Route("ReCaptchaTracker/Total")]
        public async Task<IActionResult> ObterTotalReCaptcha()
        {
            await SincronizarReCaptchaComGoogleAsync();
            string total = LabWebMvc.MVC.Areas.Utils.Utils.TotalReCaptcha(_db) ?? "0/0";
            return Json(new { sucesso = true, total });
        }

        [HttpGet]
        [Route("ReCaptchaTracker/Sincronizar")]
        public async Task<IActionResult> SincronizarReCaptcha()
        {
            await SincronizarReCaptchaComGoogleAsync();
            string total = LabWebMvc.MVC.Areas.Utils.Utils.TotalReCaptcha(_db) ?? "0/0";
            return Json(new { sucesso = true, total });
        }

        private async Task SincronizarReCaptchaComGoogleAsync()
        {
            if (!_captchaSettings.SincronizarComGoogle)
            {
                // Sincronização desabilitada intencionalmente; não loga repetidamente para não poluir o Event Viewer.
                return;
            }

            try
            {
                string projectId = _captchaSettings.ProjectID;
                if (string.IsNullOrWhiteSpace(projectId))
                {
                    _eventLogHelper.LogEventViewer("[ReCaptchaTracker] ProjectID não configurado. Não é possível sincronizar com o Google.", "wWarning");
                    return;
                }

                _eventLogHelper.LogEventViewer($"[ReCaptchaTracker] Iniciando sincronização para o projeto {projectId}...", "wInfo");
                long? totalGoogle = await _metricasService.ObterTotalAvaliacoesMesAtualAsync(projectId);
                if (totalGoogle.HasValue)
                {
                    LabWebMvc.MVC.Areas.Utils.Utils.AtualizarContagemReCaptcha(_db, projectId, totalGoogle.Value);
                    _eventLogHelper.LogEventViewer($"[ReCaptchaTracker] Contagem atualizada no banco local: {totalGoogle.Value}", "wInfo");
                }
                else
                {
                    _eventLogHelper.LogEventViewer("[ReCaptchaTracker] Consulta ao Google não retornou valor. Contagem local não alterada.", "wWarning");
                }
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer($"[ReCaptchaTracker] Erro ao sincronizar ReCaptcha com Google: {ex.Message}", "wError");
            }
        }

        [HttpGet]
        [Route("ReCaptchaTracker/VerificarLimite")]
        public ReCaptchaLimiteResult VerificarReCaptchaLimite(string nomeProjeto)
        {
            try
            {
                DateTime agora = DateTime.Now;
                ReCaptchaMonitoramento? monitor = _db.ReCaptchaMonitoramento
                    .FirstOrDefault(x => x.NomeProjeto == nomeProjeto &&
                                         x.MesReferencia == agora.Month &&
                                         x.AnoReferencia == agora.Year);

                if (monitor != null && monitor.QuantidadeSolicitacoes >= 9000)
                {
                    _eventLogHelper.LogEventViewer($"Você está chegando no limite gratuito de 10.000 (dez mil) no mês em solicitações ReCaptcha. Depois disso, a cada 1.000 (mil) solicitações haverá $1 dollar de custo ou mais cobrados pelo Google!", "wWarning");
                    _eventLogHelper.LogEventViewer($"Contagem acima do limite gratuito para ReCaptcha está em: " + (monitor.QuantidadeSolicitacoes - 10000).ToString(), "wWarning");
                    return new ReCaptchaLimiteResult()
                    {
                        Sucesso = false,
                        Titulo = "Limite Atingido",
                        Mensagem = "O limite máximo gratuito no mês é de 10.000 requisições ReCaptcha. Deseja continuar? Isso pode gerar custos em dollar pelo Google.",
                        PrecisaConfirmacao = true
                    };
                }
                return new ReCaptchaLimiteResult { Sucesso = true };
            }
            catch (Exception ex)
            {
                _eventLogHelper.LogEventViewer($"Erro ao verificar limite ReCaptcha: {ex.Message}", "wError");
                return new ReCaptchaLimiteResult { Sucesso = false, Titulo = "Erro ReCaptcha", Mensagem = "Erro ao verificar limite ReCaptcha: " + ex.Message };
            }
        }
    }
}