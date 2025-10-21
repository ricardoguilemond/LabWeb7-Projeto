using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ExamesRealizadosAM
{
    public int Id { get; set; }

    public int OrigemId { get; set; }

    public int PacienteId { get; set; }

    public int TabelaExamesId { get; set; }

    public int InstituicaoId { get; set; }

    public int PostoId { get; set; }

    public int MedicoId { get; set; }

    public int ClasseExamesId { get; set; }

    public int Sequencial { get; set; }

    public string? LaboratorioApoio { get; set; }

    public string ControleApoio { get; set; } = null!;

    public string? HistoricoClinico { get; set; }

    public string? ExameColado { get; set; }

    public string? ExameColadoImagens { get; set; }

    public int TravaColado { get; set; }

    public DateTime DataIni { get; set; }

    public DateTime? DataFim { get; set; }

    public int Liberacao { get; set; }

    public DateTime? DataExame { get; set; }

    public string? DataColeta { get; set; }

    public DateTime? DataEntrega { get; set; }

    public int Baixado { get; set; }

    public int EnviarEmail { get; set; }

    public int Situacao { get; set; }

    public int TotalImpresso { get; set; }

    public virtual ClasseExames ClasseExames { get; set; } = null!;

    public virtual Instituicao Instituicao { get; set; } = null!;

    public virtual ICollection<ItensExamesRealizadosAM> ItensExamesRealizadosAM { get; set; } = new List<ItensExamesRealizadosAM>();

    public virtual Medicos Medicos { get; set; } = null!;

    public virtual Pacientes Pacientes { get; set; } = null!;

    public virtual Postos Postos { get; set; } = null!;

    public virtual TabelaExames TabelaExames { get; set; } = null!;
}
