using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmPostos
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Instituição é obrigatória")]
        [Range(1, int.MaxValue, ErrorMessage = "Instituição é obrigatória")]
        public int InstituicaoId { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^[A-Z0-9 ._\-]+$",
            ErrorMessage = "Sigla aceita apenas letras maiúsculas (A-Z), dígitos e os símbolos espaço . _ -")]
        [Required(ErrorMessage = "Sigla do posto precisa ser preenchida")]
        public string SiglaPosto { get; set; } = null!;

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

        /* Auxiliares somente leitura para listagens */
        public string? SiglaInstituicao { get; set; }
        public string? NomeInstituicao { get; set; }

        /* Listas para dropdown de Instituicao (nao participam do binding POST) */
        public List<SelectListItem> InstituicoesSigla { get; set; } = new();
        public List<SelectListItem> InstituicoesNome { get; set; } = new();

        /* Auxiliar para pre-selecao de UF */
        public string? SessionUF { get; set; }

        /* Propriedade para grid Index (migrada de ViewBag.ListaDados) */
        public ICollection<dynamic>? ListaDados { get; set; }

        /* Campos auxiliares */
        public virtual vmGeral vmGeral { get; set; } = null!;
    }
}