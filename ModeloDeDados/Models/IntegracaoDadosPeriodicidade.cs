using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class IntegracaoDadosPeriodicidade
{
    public int Id { get; set; }

    public string TipoPeriodoExtracao { get; set; } = null!;
}
