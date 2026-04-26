using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ControleDePerfilModelo
{
    public int Id { get; set; }

    public string MenuNivel1 { get; set; } = null!;

    public string MenuNivel2 { get; set; } = null!;

    public string MenuNivel3 { get; set; } = null!;

    public string MenuNivel4 { get; set; } = null!;

    public string MenuNivel5 { get; set; } = null!;
}
