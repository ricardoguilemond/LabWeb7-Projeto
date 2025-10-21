using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class MemoAuxiliar
{
    public int Id { get; set; }

    public string? NomeFolha { get; set; }

    public string? Linha1 { get; set; }

    public string? Linha2 { get; set; }

    public string? Linha3 { get; set; }

    public string? Linha4 { get; set; }

    public string? Linha5 { get; set; }

    public string? Linha6 { get; set; }

    public byte[]? CampoMemo { get; set; }
}
