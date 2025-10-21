using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Postos
{
    public int Id { get; set; }

    public string NomePosto { get; set; } = null!;

    public string Responsavel { get; set; } = null!;

    public string? Logradouro { get; set; }

    public string? Endereco { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cidade { get; set; }

    public string? UF { get; set; }

    public string? CEP { get; set; }

    public string? Telefone { get; set; }

    public virtual ICollection<ExamesRealizados> ExamesRealizados { get; set; } = new List<ExamesRealizados>();

    public virtual ICollection<ExamesRealizadosAM> ExamesRealizadosAM { get; set; } = new List<ExamesRealizadosAM>();
}
