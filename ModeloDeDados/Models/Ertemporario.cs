using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ERTemporario
{
    public int Id { get; set; }

    public int ExameId { get; set; }

    public int PacienteId { get; set; }

    public int InstituicaoId { get; set; }

    public int TabelaExamesId { get; set; }

    public int MedicoId { get; set; }

    public int Sequencial { get; set; }

    public int ClasseExamesId { get; set; }

    public string? HistoricoClinico { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataIni { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataFim { get; set; }

    public int Liberacao { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataExame { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime? DataEntrega { get; set; }

    public int Baixado { get; set; }
}
