using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class FichasLotes
{
    public int Id { get; set; }

    public string? NomeFicha { get; set; }

    public string? ContaExame { get; set; }

    public string? Descricao { get; set; }

    public string? Resultado { get; set; }

    public string? MapaHorizontal { get; set; }

    public int ExamesRealizadosId { get; set; }

    public int PacienteId { get; set; }

    public int MedicoId { get; set; }

    public int InstituicaoId { get; set; }

    public int TabelaExamesId { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataExame { get; set; }

    public string? ControleApoio { get; set; }

    public int Sequencial { get; set; }

    public string? HistoricoClinico { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataIni { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataFim { get; set; }

    public int Lote { get; set; }

    public string? LiberadoExclusao { get; set; }

    public virtual ExamesRealizados ExamesRealizados { get; set; } = null!;

    public virtual Instituicao Instituicao { get; set; } = null!;

    public virtual Medicos Medicos { get; set; } = null!;

    public virtual Pacientes Pacientes { get; set; } = null!;

    public virtual TabelaExames TabelaExames { get; set; } = null!;
}
