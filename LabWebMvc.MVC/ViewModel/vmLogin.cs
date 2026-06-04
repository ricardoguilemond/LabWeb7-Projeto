using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmLogin
    {
        [Display(Name = "Email")]
        [Required(ErrorMessage = "Informe seu email")]
        public string? LoginUsuario { get; set; }

        [Required(ErrorMessage = "Informe sua senha")]
        [DataType(DataType.Password)]
        public string? SenhaUsuario { get; set; }

        [Display(Name = "Lembrar esse login da próxima vez")]
        public bool LembrarMe { get; set; } = false;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Informe seu Email de cadastro")]
        public string? Email { get; set; }

        //"Nomes" apenas informativo
        public string? NomeLogin { get; set; }

        public string? NomeCompleto { get; set; }

        [Display(Name = "CPF")]
        [Required(ErrorMessage = "Informe seu CPF")]
        public string? CPF { get; set; }

        [Display(Name = "Data de nascimento")]
        [Required(ErrorMessage = "Informe sua Data de Nascimento")]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        /* Chave Pública do Enterprise para os Front-End e Back-end */
        //public string KeySecretPublic { get; set; } =  configuration.GetSection("ExecutaAvaliacaoReCaptcha").GetSection("ReCaptchaSiteKey").Value ?? ""; //CriptoDecripto.mySecretKeyGoogle;

        /* Token retornado pelo ReCaptcha do Google Response */
        public string GoogleCaptchaToken { get; set; } = null!;

        /* Propriedades para a View de Login (tipadas - migradas de ViewBag) */
        public string? KeySecretPublic { get; set; }
        public string? UrlGoogleAPI { get; set; }
        public string TitleLogin { get; set; } = "Login";
        public string Entrar { get; set; } = "Entrar";
        public string EsqueceuSuaSenha { get; set; } = "Esqueceu sua senha?";
        public string? OlhoEmail { get; set; }
        public string? MensagemErroLogin { get; set; }
    }
}