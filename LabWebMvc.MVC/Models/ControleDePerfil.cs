using System.ComponentModel.DataAnnotations.Schema;

namespace LabWebMvc.MVC.Models;

public partial class ControleDePerfil
{
    public int Id { get; set; }

    public int ControleDeAcessoId { get; set; }

    public string MenuNivelMenu { get; set; } = null!;

    public string MenuNivel1 { get; set; } = null!;

    public string MenuNivel2 { get; set; } = null!;

    public string MenuNivel3 { get; set; } = null!;

    public string MenuNivel4 { get; set; } = null!;

    public int Ativo { get; set; }

    public virtual ControleDeAcesso ControleDeAcesso { get; set; } = null!;
}