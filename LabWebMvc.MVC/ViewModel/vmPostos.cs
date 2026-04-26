using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmPostos
    {
        public int Id { get; set; }

        [StringLength(60)]
        [Required(ErrorMessage = "<div class='has-error'>Nome do posto de coleta/anexo precisa ser preenchido</div>")]
        public string NomePosto { get; set; } = null!;

        [StringLength(60)]
        [Required(ErrorMessage = "<div class='has-error'>Nome de responsável precisa ser preenchido</div>")]
        public string Responsavel { get; set; } = null!;

        [StringLength(8)]
        public string? Logradouro { get; set; }

        [StringLength(100)]
        public string? Endereco { get; set; }

        [StringLength(15)]
        public string? Numero { get; set; }

        [StringLength(25)]
        public string? Complemento { get; set; }

        [StringLength(45)]
        public string? Bairro { get; set; }

        [StringLength(45)]
        public string? Cidade { get; set; }

        [StringLength(2)]
        public string? UF { get; set; }

        [StringLength(8)]
        public string? CEP { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^\(?([0-9]{2})\)?[-. ]?([0-9]{5})[-. ]?([0-9]{4})", ErrorMessage = "<div class='has-error'>Telefone inválido</div>")]
        public string? Telefone { get; set; }

        /* Campos auxiliares */
        public virtual vmGeral vmGeral { get; set; } = null!;
    }
}