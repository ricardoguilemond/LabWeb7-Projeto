namespace LabWebMvc.MVC.Models;

public partial class TipoSanguineo
{
    public int Id { get; set; }

    public string Tipo { get; set; } = null!;

    public string Rh { get; set; } = null!;

    public string? DoaPara { get; set; }

    public string? RecebeDe { get; set; }
}