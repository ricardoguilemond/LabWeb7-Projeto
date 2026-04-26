using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class LogArquivos
{
    public int Id { get; set; }

    public int? IntegracaoDadosLayoutId { get; set; }

    public string StrRef { get; set; } = null!;

    public string? NomeArquivo { get; set; }

    public DateTime Data { get; set; }

    public DateTime? DataPeriodoInicial { get; set; }

    public DateTime? DataPeriodoFinal { get; set; }

    public int TipoServico { get; set; }

    public virtual IntegracaoDadosLayout? IntegracaoDadosLayout { get; set; }
}
