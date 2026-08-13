using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel;

public class vmFormasRecebimento
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    public bool PermiteParticular { get; set; } = true;

    public bool PermiteInstituicao { get; set; } = true;

    public bool Ativo { get; set; } = true;
}
