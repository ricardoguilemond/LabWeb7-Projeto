using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ExamesPendentes
{
    public int Id { get; set; }

    public int PacienteId { get; set; }

    public int InstituicaoId { get; set; }

    public int TabelaExamesId { get; set; }

    public int MedicoId { get; set; }

    public int ClasseExamesId { get; set; }

    public int Sequencial { get; set; }

    public string? LaboratorioApoio { get; set; }

    public string? ControleApoio { get; set; }

    public string? ContaExame { get; set; }

    public string? NomeFolha { get; set; }

    public string? NomeGrupo { get; set; }

    public string? NomeItem { get; set; }

    public DateTime? DataIni { get; set; }

    public virtual ClasseExames ClasseExames { get; set; } = null!;

    public virtual Instituicao Instituicao { get; set; } = null!;

    public virtual Medicos Medicos { get; set; } = null!;

    public virtual Pacientes Pacientes { get; set; } = null!;

    public virtual TabelaExames TabelaExames { get; set; } = null!;
}
