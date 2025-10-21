using System.ComponentModel.DataAnnotations.Schema;

namespace LabWebMvc.MVC.Models;

public partial class ControleDePerfilTipo
{
    public int Id { get; set; }

    public string Tipo { get; set; } = null!;
}