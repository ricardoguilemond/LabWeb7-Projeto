namespace LabWebMvc.MVC.Models;

public partial class Medicos
{
    public int Id { get; set; }

    public string NomeMedico { get; set; } = null!;

    public string? Especialidade { get; set; }

    public string CRM { get; set; } = null!;

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<ExamesExportados> ExamesExportados { get; set; } = [];

    public virtual ICollection<ExamesPendentes> ExamesPendentes { get; set; } = [];

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = [];

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; set; } = [];

    public virtual ICollection<FichasInternas> FichasInternas { get; set; } = [];

    public virtual ICollection<FichasLotes> FichasLotes { get; set; } = [];

    public virtual ICollection<FichasPlanilhas> FichasPlanilhas { get; set; } = [];

    public virtual ICollection<Requisitar> Requisitar { get; set; } = [];
}