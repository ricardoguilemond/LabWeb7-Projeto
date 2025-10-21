using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ControleDeAcesso
{
    public int Id { get; set; }

    public int SenhaId { get; set; }

    public virtual ICollection<ControleDePerfil> ControleDePerfil { get; set; } = new List<ControleDePerfil>();
}
