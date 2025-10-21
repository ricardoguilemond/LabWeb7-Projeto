using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmPacientes
    {
        public int Id { get; set; }
        public string? IdPacienteExterno { get; set; }

        [Required(ErrorMessage = "<div class='has-error'>Nome do paciente precisa ser preenchido</div>"), MaxLength(100)]
        [StringLength(100, ErrorMessage = "<div class='has-error'>Precisa ter o mínimo de 5 e máximo de {1} caracteres</div>", MinimumLength = 5)]
        public string NomePaciente { get; set; } = null!;

        [StringLength(100, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? NomeSocial { get; set; }

        [StringLength(100, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? NomePai { get; set; }

        [StringLength(100, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? NomeMae { get; set; }

        [Required(ErrorMessage = "<div class='has-error'>Data de Nascimento deve ser preenchida</div>")]
        [BindProperty, DataType(DataType.Date)]
        public DateTime Nascimento { get; set; }

        /*  Não limitar o tamanho do CPF aqui, porque na View este campo poderá receber números de outros documentos  */
        public string? CPF { get; set; }

        public int TipoDocumento { get; set; }

        [StringLength(20, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Identidade { get; set; }

        [Required]
        public int Emissor { get; set; }

        [StringLength(15, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? CarteiraSUS { get; set; }

        [Required]
        public int EstadoCivil { get; set; }

        public string? Sexo { get; set; }

        public string? Cor { get; set; }

        public string? EtniaIndigena { get; set; }

        public string? TipoSanguineo { get; set; }

        public DateTime? DUM { get; set; }

        [Required]
        public int TempoGestacao { get; set; }

        public string? Profissao { get; set; }

        [StringLength(30, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Naturalidade { get; set; }

        [StringLength(30, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Nacionalidade { get; set; }

        public DateTime? DataEntradaBrasil { get; set; }

        [StringLength(8, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Logradouro { get; set; }

        [StringLength(100, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Endereco { get; set; }

        [StringLength(15, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Numero { get; set; }

        [StringLength(25, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Complemento { get; set; }

        [StringLength(45, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Bairro { get; set; }

        [StringLength(45, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? UF { get; set; }

        [StringLength(8, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        public string? CEP { get; set; }

        [StringLength(15, ErrorMessage = "<div class='has-error'>Telefone inválido (precisa ter o DDD com dois dígitos)</div>", MinimumLength = 8)]
        public string? Telefone { get; set; }

        [StringLength(100, ErrorMessage = "<div class='has-error'>Pode ter no máximo {1} caracteres</div>", MinimumLength = 0)]
        [RegularExpression(@"^[a-zA-Z]+(([\'\,\.\- ][a-zA-Z ])?[a-zA-Z]*)*\s+<(\w[-._\w]*\w@\w[-._\w]*\w\.\w{2,3})>$|^(\w[-._\w]*\w@\w[-._\w]*\w\.\w{2,3})$", ErrorMessage = "<div class='has-error'>Formato do e-mail inválido</div>")]
        public string? Email { get; set; }

        public string? Observacao { get; set; }

        public DateTime DataEntrada { get; set; }

        public DateTime? DataBaixa { get; set; }

        public int StatusBaixa { get; set; }

        public DateTime DataRegistro { get; set; }

        //Others
        public virtual List<vmPacientes>? ListaPacientes { get; set; }

        public virtual vmGeral vmGeral { get; set; } = null!;

        /*
    public virtual ICollection<ExamesExportados> ExamesExportados { get; } = new List<ExamesExportados>();

    public virtual ICollection<ExamesImpressos> ExamesImpressos { get; } = new List<ExamesImpressos>();

    public virtual ICollection<ExamesPendentes> ExamesPendentes { get; } = new List<ExamesPendentes>();

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; } = new List<ExamesRealizados>();

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; } = new List<ExamesRealizadosAM>();

    public virtual ICollection<FichasInternas> FichasInternas { get; } = new List<FichasInternas>();

    public virtual ICollection<FichasLotes> FichasLotes { get; } = new List<FichasLotes>();

    public virtual ICollection<FichasPlanilhas> FichasPlanilhas { get; } = new List<FichasPlanilhas>();

    public virtual ICollection<ItensExamesRealizados> ItensExamesRealizados { get; } = new List<ItensExamesRealizados>();

    public virtual ICollection<Requisitar> Requisitar { get; } = new List<Requisitar>();

         */
    }
}