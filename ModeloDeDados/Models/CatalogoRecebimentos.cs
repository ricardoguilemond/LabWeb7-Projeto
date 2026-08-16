using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class CatalogoRecebimentos
{
    public int Id { get; set; }

    public int Origem { get; set; }

    public int InstituicaoId { get; set; }

    public int PacienteId { get; set; }

    public string? PeriodoFaturamento { get; set; }

    public decimal ValorTotal { get; set; }

    //Feito pelo Qoder em 16/08/2026 — desconto concedido no recebimento
    public decimal ValorDesconto { get; set; }
    //..Qoder

    //Feito pelo Qoder em 16/08/2026 — true: valor a cobrar da Instituição (título Pendente)
    public bool CobrancaInstituicao { get; set; }
    //..Qoder

    public DateTime DataRecebimento { get; set; }

    public int Status { get; set; }

    public string? Observacao { get; set; }

    public string? UsuarioRegistro { get; set; }

    public DateTime DataRegistro { get; set; }

    public virtual Instituicao Instituicao { get; set; } = null!;

    public virtual Pacientes Paciente { get; set; } = null!;

    public virtual ICollection<CatalogoRecebimentosExames> CatalogoRecebimentosExames { get; set; } = new List<CatalogoRecebimentosExames>();

    public virtual ICollection<CatalogoRecebimentosFormas> CatalogoRecebimentosFormas { get; set; } = new List<CatalogoRecebimentosFormas>();
}
