using static LabWebMvc.MVC.Areas.Controllers.ReCaptchaTrackerController;

namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public class ReCaptchaService
    {
        private readonly CreateAssessmentSample _captchaService;

        public ReCaptchaService(CreateAssessmentSample captchaService)
        {
            _captchaService = captchaService;
        }

        public async Task<ICollection<string>> RegistrarSolicitacaoAsync(string token, string projectId, string recaptchaAction)
        {
            return await _captchaService.CreateAssessment(token, projectId, recaptchaAction);
        }
    }
}
