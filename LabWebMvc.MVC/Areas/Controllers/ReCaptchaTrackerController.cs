using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Interfaces.Criptografias;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public class ReCaptchaTrackerController : BaseController
    {
        private readonly CreateAssessmentSample _captchaService;

        public ReCaptchaTrackerController(
            IDbFactory dbFactory,
            IValidadorDeSessao validador,
            GeralController geralController,
            IEventLogHelper eventLogHelper,
            Imagem imagem,
            CreateAssessmentSample captchaService)
            : base(dbFactory, validador, geralController, eventLogHelper, imagem)
        {
            _captchaService = captchaService;
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
            DateTime agora = DateTime.UtcNow;

            // Sempre registra a tentativa, independentemente do sucesso da verificação de limite, pois o Google registra todas as tentativas!
            try
            {
                ReCaptchaMonitoramento? monitor = _db.ReCaptchaMonitoramento
                    .FirstOrDefault(x => x.NomeProjeto == nomeProjeto &&
                                         x.MesReferencia == agora.Month &&
                                         x.AnoReferencia == agora.Year);

                if (monitor == null)
                {
                    monitor = new ReCaptchaMonitoramento
                    {
                        NomeProjeto = nomeProjeto,
                        QuantidadeSolicitacoes = 1,
                        MesReferencia = agora.Month,
                        AnoReferencia = agora.Year
                    };
                    _db.ReCaptchaMonitoramento.Add(monitor);
                }
                else
                {
                    monitor.QuantidadeSolicitacoes++;
                    _db.ReCaptchaMonitoramento.Update(monitor);
                }

                _db.SaveChanges();
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
        public ReCaptchaLimiteResult VerificarReCaptchaLimite(string nomeProjeto)
        {
            try
            {
                DateTime agora = DateTime.UtcNow;
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