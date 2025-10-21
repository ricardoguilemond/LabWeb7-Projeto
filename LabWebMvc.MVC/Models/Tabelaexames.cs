namespace LabWebMvc.MVC.Models;

public partial class TabelaExames
{
    public int Id { get; set; }

    public string SiglaTabela { get; set; } = null!;

    public string NomeTabela { get; set; } = null!;

    public int Orcamento { get; set; }

    public int Bloqueado { get; set; }

    public virtual ICollection<ExamesExportados> ExamesExportados { get; set; } = [];

    public virtual ICollection<ExamesImpressos> ExamesImpressos { get; set; } = [];

    public virtual ICollection<ExamesPendentes> ExamesPendentes { get; set; } = [];

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = [];

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; set; } = [];

    public virtual ICollection<FichasLotes> FichasLotes { get; set; } = [];

    public virtual ICollection<FichasPlanilhas> FichasPlanilhas { get; set; } = [];

    public virtual ICollection<ItensExamesRealizados> ItensExamesRealizados { get; set; } = [];

    public virtual ICollection<ItensExamesRealizadosAM> ItensExamesRealizadosAM { get; set; } = [];

    public virtual ICollection<Requisitar> Requisitar { get; set; } = [];
}