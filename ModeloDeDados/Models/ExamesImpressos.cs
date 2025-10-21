using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ExamesImpressos
{
    public int Id { get; set; }

    public int PacienteId { get; set; }

    public int InstituicaoId { get; set; }

    public int TabelaExamesId { get; set; }

    public int Sequencial { get; set; }

    public DateTime? DataExame { get; set; }

    public DateTime? DataImpresso { get; set; }

    public int TotalImpresso { get; set; }

    public virtual Instituicao Instituicao { get; set; } = null!;

    public virtual Pacientes Pacientes { get; set; } = null!;

    public virtual TabelaExames TabelaExames { get; set; } = null!;
}
