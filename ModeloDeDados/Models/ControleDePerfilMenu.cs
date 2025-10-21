using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class ControleDePerfilMenu
{
    public int Id { get; set; }

    public int Coluna { get; set; }

    public string Menu { get; set; } = null!;

    public string? Area { get; set; }

    public string? Controller { get; set; }

    public string? Action { get; set; }

    public string Nivel { get; set; } = null!;

    public int Ativo { get; set; }
}
