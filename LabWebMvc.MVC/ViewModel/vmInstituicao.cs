using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmInstituicao
    {
        public int Id { get; set; }

        [StringLength(20, ErrorMessage = "<div class='has-error'>Precisa ter o mínimo de 3 e máximo de {1} caracteres</div>", MinimumLength = 3)]
        [Required(ErrorMessage = "<div class='has-error'>Entre com a sigla da instituição</div>"), MaxLength(20)]
        public string Sigla { get; set; } = null!;

        [StringLength(100, ErrorMessage = "<div class='has-error'>Precisa ter o mínimo de 3 e máximo de {1} caracteres</div>", MinimumLength = 3)]
        [Required(ErrorMessage = "<div class='has-error'>Entre com o nome da instituição</div>"), MaxLength(100)]
        public string Nome { get; set; } = null!;

        [StringLength(14, ErrorMessage = "<div class='has-error'>Precisa ter {1} caracteres. (aceita zeros à esquerda)</div>", MinimumLength = 14)]
        [Required(ErrorMessage = "<div class='has-error'>Entre com o CNPJ da instituição</div>"), MaxLength(14)]
        public string CNPJ { get; set; } = null!;

        public int Sequencial { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z]+(([\'\,\.\- ][a-zA-Z ])?[a-zA-Z]*)*\s+<(\w[-._\w]*\w@\w[-._\w]*\w\.\w{2,3})>$|^(\w[-._\w]*\w@\w[-._\w]*\w\.\w{2,3})$", ErrorMessage = "<div class='has-error'>Formato do e-mail inválido</div>")]
        public string Email { get; set; } = null!;

        public string? TituloTimbre { get; set; }

        public string? SubTituloTimbre { get; set; }

        public byte[]? Timbre { get; set; }

        public byte[]? Logomarca { get; set; }

        public string? NomeTimbre { get; set; }

        public string? NomeLogomarca { get; set; }

        public int CarimboSN { get; set; }

        public int TimbreSN { get; set; }

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

        public string Contato { get; set; } = null!;

        [StringLength(15)]
        [RegularExpression(@"^\(?([0-9]{2})\)?[-. ]?([0-9]{5})[-. ]?([0-9]{4})", ErrorMessage = "<div class='has-error'>Telefone inválido</div>")]
        [Required(ErrorMessage = "<div class='has-error'>Número de telefone fixo ou celular com DDD</div>")]
        public string Telefone { get; set; } = null!;

        public string? Celular { get; set; }

        public string? UsuarioCaminhoFTP { get; set; }

        public string? UsuarioEmailFTP { get; set; }

        public int? UsuarioPortaFTP { get; set; }

        public string? UsuarioSenhaFTP { get; set; }

        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        public decimal? ValorExameCitologia { get; set; }

        public int? Propaganda { get; set; }

        public string? AvisoRodape1 { get; set; }

        public string? AvisoRodape2 { get; set; }

        /* Campos auxiliares */
        public string? CaminhoImagemTimbre { get; set; }
        public string? CaminhoImagemLogomarca { get; set; }

        public virtual List<vmInstituicao>? ListaInstituicoes { get; set; }

        public virtual vmGeral vmGeral { get; set; } = null!;
    }
}