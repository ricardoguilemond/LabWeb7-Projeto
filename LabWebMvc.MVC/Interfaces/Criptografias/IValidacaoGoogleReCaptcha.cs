using LabWebMvc.MVC.ViewModel;

namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public interface IValidacaoGoogleReCaptcha
    {
        bool IsCaptchaValid(vmLogin vm);

        ICollection<string> ValidaReCaptchaResponse(HttpResponseMessage? conteudo, ref ICollection<string> listaErros);
    }
}