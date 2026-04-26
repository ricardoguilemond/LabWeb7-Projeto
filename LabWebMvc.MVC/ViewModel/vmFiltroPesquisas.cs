using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmFiltroPesquisas
    {
        [Display(Name = "Entre com algum dado")]
        public string? Conteudo { get; set; } = null!;
    }
}