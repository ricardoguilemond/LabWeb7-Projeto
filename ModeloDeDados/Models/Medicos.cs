using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Medicos
{
    public int Id { get; set; }

    public string NomeMedico { get; set; } = null!;

    public string? Especialidade { get; set; }

    public string CRM { get; set; } = null!;

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<ExamesExportados> ExamesExportados { get; set; } = new List<ExamesExportados>();

    public virtual ICollection<ExamesPendentes> ExamesPendentes { get; set; } = new List<ExamesPendentes>();

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = new List<ExamesRealizados>();

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; set; } = new List<ExamesRealizadosAM>();

    public virtual ICollection<FichasInternas> FichasInternas { get; set; } = new List<FichasInternas>();

    public virtual ICollection<FichasLotes> FichasLotes { get; set; } = new List<FichasLotes>();

    public virtual ICollection<FichasPlanilhas> FichasPlanilhas { get; set; } = new List<FichasPlanilhas>();

    public virtual ICollection<Requisitar> Requisitar { get; set; } = new List<Requisitar>();
}
