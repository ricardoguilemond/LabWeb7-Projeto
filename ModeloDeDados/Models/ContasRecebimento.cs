using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ContasRecebimento
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public int Tipo { get; set; }

    public string? Identificacao { get; set; }

    public bool PadraoPortaria { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataRegistro { get; set; }

    public virtual ICollection<CatalogoRecebimentosFormas> CatalogoRecebimentosFormas { get; set; } = new List<CatalogoRecebimentosFormas>();
}
