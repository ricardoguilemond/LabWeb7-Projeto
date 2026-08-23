using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ExamesExportados
{
    public int Id { get; set; }

    public int ExameId { get; set; }

    public int PacienteId { get; set; }

    public int InstituicaoId { get; set; }

    public int TabelaExamesId { get; set; }

    public int MedicoId { get; set; }

    public int Sequencial { get; set; }

    public string? LaboratorioApoio { get; set; }

    public string ControleApoio { get; set; } = null!;

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataColeta { get; set; }

    public DateTime? DataExportado { get; set; }

    public DateTime? DataImportado { get; set; }

    public virtual ExamesRealizados ExamesRealizados { get; set; } = null!;

    public virtual Instituicao Instituicao { get; set; } = null!;

    public virtual Medicos Medicos { get; set; } = null!;

    public virtual Pacientes Pacientes { get; set; } = null!;

    public virtual TabelaExames TabelaExames { get; set; } = null!;
}
