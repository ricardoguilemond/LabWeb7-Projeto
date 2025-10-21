using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class TabelaExames
{
    public int Id { get; set; }

    public string SiglaTabela { get; set; } = null!;

    public string NomeTabela { get; set; } = null!;

    public int Orcamento { get; set; }

    public int Bloqueado { get; set; }

    public virtual ICollection<ExamesExportados> ExamesExportados { get; set; } = new List<ExamesExportados>();

    public virtual ICollection<ExamesImpressos> ExamesImpressos { get; set; } = new List<ExamesImpressos>();

    public virtual ICollection<ExamesPendentes> ExamesPendentes { get; set; } = new List<ExamesPendentes>();

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = new List<ExamesRealizados>();

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; set; } = new List<ExamesRealizadosAM>();

    public virtual ICollection<FichasLotes> FichasLotes { get; set; } = new List<FichasLotes>();

    public virtual ICollection<FichasPlanilhas> FichasPlanilhas { get; set; } = new List<FichasPlanilhas>();

    public virtual ICollection<ItensExamesRealizados> ItensExamesRealizados { get; set; } = new List<ItensExamesRealizados>();

    public virtual ICollection<ItensExamesRealizadosAM> ItensExamesRealizadosAM { get; set; } = new List<ItensExamesRealizadosAM>();

    public virtual ICollection<Requisitar> Requisitar { get; set; } = new List<Requisitar>();
}
