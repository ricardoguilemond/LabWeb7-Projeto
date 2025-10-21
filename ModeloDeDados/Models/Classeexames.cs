using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ClasseExames
{
    public int Id { get; set; }

    public string? RefExame { get; set; }

    public int Etiquetas { get; set; }

    public string? TipoMapa { get; set; }

    public int Assinatura1 { get; set; }

    public int Assinatura2 { get; set; }

    public int Assinatura3 { get; set; }

    public int Assinatura4 { get; set; }

    public byte[]? ImgAss1 { get; set; }

    public byte[]? ImgAss2 { get; set; }

    public byte[]? ImgAss3 { get; set; }

    public byte[]? ImgAss4 { get; set; }

    public string? NomeAss1 { get; set; }

    public string? NomeAss2 { get; set; }

    public string? NomeAss3 { get; set; }

    public string? NomeAss4 { get; set; }

    public int Marcado { get; set; }

    public int Planilha { get; set; }

    public int MHI { get; set; }

    public string? LaboratorioExterno { get; set; }

    public virtual ICollection<ExamesPendentes> ExamesPendentes { get; set; } = new List<ExamesPendentes>();

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = new List<ExamesRealizados>();

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; set; } = new List<ExamesRealizadosAM>();

    public virtual ICollection<ItensExamesRealizados> ItensExamesRealizados { get; set; } = new List<ItensExamesRealizados>();

    public virtual ICollection<ItensExamesRealizadosAM> ItensExamesRealizadosAM { get; set; } = new List<ItensExamesRealizadosAM>();

    public virtual ICollection<Requisitar> Requisitar { get; set; } = new List<Requisitar>();
}
