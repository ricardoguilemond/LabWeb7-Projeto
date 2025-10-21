using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmMedicos
    {
        [Display(Name = "Id")]
        public int Id { get; set; }

        [StringLength(100)]
        [Required(ErrorMessage = "<div class='has-error'>Nome do médico precisa ser preenchido</div>")]
        [Display(Name = "Nome Médico")]
        public string NomeMedico { get; set; } = null!;

        [Display(Name = "Especialidade")]
        public string? Especialidade { get; set; }

        [Required(ErrorMessage = "<div class='has-error'>CRM do médico precisa ser preenchido</div>")]
        [Display(Name = "CRM")]
        public string CRM { get; set; } = null!;

        [StringLength(15)]
        [RegularExpression(@"^\(?([0-9]{2})\)?[-. ]?([0-9]{5})[-. ]?([0-9]{4})", ErrorMessage = "<div class='has-error'>Telefone inválido</div>")]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z]+(([\'\,\.\- ][a-zA-Z ])?[a-zA-Z]*)*\s+<(\w[-._\w]*\w@\w[-._\w]*\w\.\w{2,3})>$|^(\w[-._\w]*\w@\w[-._\w]*\w\.\w{2,3})$", ErrorMessage = "<div class='has-error'>Formato do e-mail inválido</div>")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        //public virtual ICollection<vmMedicos> ListaMedicos { get; set; } = [];

        public List<vmMedicos> Dados { get; set; } = [];

        // Recupera os nomes das propriedades automaticamente
        public List<string> NomesCampos => typeof(vmMedicos).GetProperties().Select(p => p.Name).ToList();

        // Recupera os nomes dos títulos pelo atributo [Display(Name = "...")]
        public List<string> TitulosColunas => typeof(vmMedicos).GetProperties().Select(p => p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name).ToList();
    }//fim
}