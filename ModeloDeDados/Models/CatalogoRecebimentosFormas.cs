using System;

namespace LabWebMvc.MVC.Models;

public partial class CatalogoRecebimentosFormas
{
    public int Id { get; set; }

    public int CatalogoRecebimentoId { get; set; }

    public int FormaRecebimentoId { get; set; }

    public int ContaRecebimentoId { get; set; }

    public decimal Valor { get; set; }

    public DateTime DataRecebimento { get; set; }

    public string? Observacao { get; set; }

    public virtual CatalogoRecebimentos CatalogoRecebimento { get; set; } = null!;

    public virtual ContasRecebimento ContaRecebimento { get; set; } = null!;

    public virtual FormasRecebimento FormaRecebimento { get; set; } = null!;
}
