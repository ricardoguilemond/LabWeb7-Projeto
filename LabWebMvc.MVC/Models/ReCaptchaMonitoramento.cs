using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.Models
{
    public class ReCaptchaMonitoramento
    {
        public int Id { get; set; }

        [Required]
        public string NomeProjeto { get; set; } = "labwebmvc";

        [Required]
        public int QuantidadeSolicitacoes { get; set; }

        [Required]
        public int MesReferencia { get; set; }

        [Required]
        public int AnoReferencia { get; set; }
    }
}