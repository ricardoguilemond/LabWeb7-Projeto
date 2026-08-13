using System;

namespace LabWebMvc.MVC.Models;

public partial class CatalogoRecebimentosExames
{
    public int Id { get; set; }

    public int CatalogoRecebimentoId { get; set; }

    public int ExameRealizadoId { get; set; }

    public decimal Valor { get; set; }

    public virtual CatalogoRecebimentos CatalogoRecebimento { get; set; } = null!;

    public virtual ExamesRealizados ExameRealizado { get; set; } = null!;
}
