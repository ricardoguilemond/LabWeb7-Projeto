using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabWebMvc.MVC.Models;

public partial class CatalogoRecebimentosFormas
{
    public int Id { get; set; }

    public int CatalogoRecebimentoId { get; set; }

    public int FormaRecebimentoId { get; set; }

    public int ContaRecebimentoId { get; set; }

    public decimal Valor { get; set; }

    [Column(TypeName = "date")] //Feito pelo Qoder em 22/08/2026 — data de negócio (somente dia/mês/ano)
    public DateTime DataRecebimento { get; set; }

    public string? Observacao { get; set; }

    public virtual CatalogoRecebimentos CatalogoRecebimento { get; set; } = null!;

    public virtual ContasRecebimento ContaRecebimento { get; set; } = null!;

    public virtual FormasRecebimento FormaRecebimento { get; set; } = null!;
}
