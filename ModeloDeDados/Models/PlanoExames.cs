using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class PlanoExames
{
    public int Id { get; set; }

    public int ExameId { get; set; }

    public int CitoInstituicao { get; set; }

    public string? CitoTituloFolha { get; set; }

    public int CitoTituloExame { get; set; }

    public string? CitoParteDescricao { get; set; }

    public byte[]? CitoDescricao { get; set; }

    public string RefExame { get; set; } = null!;

    public string RefItem { get; set; } = null!;

    public int TabelaExamesId { get; set; }

    public string ContaExame { get; set; } = null!;

    public string Descricao { get; set; } = null!;

    public decimal? ValorCusto { get; set; }

    public decimal? ValorItem { get; set; }

    public string? TABELACH { get; set; }

    public int QCH { get; set; }

    public decimal? ICH { get; set; }

    public string? UnidadeMedida { get; set; }

    public string? Referencia { get; set; }

    public int Etiqueta { get; set; }

    public int Etiquetas { get; set; }

    public byte[]? Laudo { get; set; }

    public int AlinhaLaudo { get; set; }

    public int Seleciona { get; set; }

    public int NaoMostrar { get; set; }

    public string? MapaHorizontal { get; set; }

    public decimal? ResultadoMinimo { get; set; }

    public decimal? ResultadoMaximo { get; set; }

    public string? LaboratorioExterno { get; set; }
}
