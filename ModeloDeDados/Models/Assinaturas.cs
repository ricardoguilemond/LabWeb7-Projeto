using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Assinaturas
{
    public int Id { get; set; }

    public byte[]? Assinatura1 { get; set; }

    public int Usar1 { get; set; }

    public string Crbio1 { get; set; } = null!;

    public byte[]? Assinatura2 { get; set; }

    public int Usar2 { get; set; }

    public string? Crbio2 { get; set; }

    public byte[]? Assinatura3 { get; set; }

    public int Usar3 { get; set; }

    public string? Crbio3 { get; set; }

    public byte[]? Assinatura4 { get; set; }

    public int Usar4 { get; set; }

    public string? Crbio4 { get; set; }
}
