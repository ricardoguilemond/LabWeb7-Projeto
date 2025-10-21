using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmLogArquivos
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string StrRef { get; set; } = null!;

        [StringLength(200)]
        public string NomeArquivo { get; set; } = null!;

        [Required]
        public DateTime Data { get; set; }

        public DateTime? DataPeriodoInicial { get; set; }
        public DateTime? DataPeriodoFinal { get; set; }
        public int? IdIntegracaoDadosLayout { get; set; }
    }
}