using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Logradouro
{
    public int Id { get; set; }

    public string Descricao { get; set; } = null!;
}
