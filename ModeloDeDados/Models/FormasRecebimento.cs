using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class FormasRecebimento
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public bool PermiteParticular { get; set; }

    public bool PermiteInstituicao { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataRegistro { get; set; }

    public virtual ICollection<CatalogoRecebimentosFormas> CatalogoRecebimentosFormas { get; set; } = new List<CatalogoRecebimentosFormas>();
}
