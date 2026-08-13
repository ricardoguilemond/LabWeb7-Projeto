using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel;

public class vmContasRecebimento
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo é obrigatório.")]
    [Range(1, 4, ErrorMessage = "O tipo deve ser Caixa, Banco, Cofre ou Outro.")]
    public int Tipo { get; set; } = 1;

    [MaxLength(100, ErrorMessage = "A identificação deve ter no máximo 100 caracteres.")]
    public string? Identificacao { get; set; }

    public bool PadraoPortaria { get; set; }

    public bool Ativo { get; set; } = true;
}
