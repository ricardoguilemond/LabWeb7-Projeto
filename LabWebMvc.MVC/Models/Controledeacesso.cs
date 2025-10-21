using System.ComponentModel.DataAnnotations.Schema;

namespace LabWebMvc.MVC.Models;

public partial class ControleDeAcesso
{
    public int Id { get; set; }

    public int SenhaId { get; set; }

    public virtual ICollection<ControleDePerfil> ControleDePerfil { get; set; } = [];
}