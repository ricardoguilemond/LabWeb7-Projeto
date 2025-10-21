using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class Senhas
{
    public int Id { get; set; }

    public string LoginUsuario { get; set; } = null!;

    public string NomeUsuario { get; set; } = null!;

    public string NomeCompleto { get; set; } = null!;

    public string SenhaUsuario { get; set; } = null!;

    public DateTime DataCadastro { get; set; }

    public DateTime? DataExpira { get; set; }

    public byte[]? Assinatura { get; set; }

    public int UsarAssinatura { get; set; }

    public string? NomeAssinatura { get; set; }

    public int Bloqueado { get; set; }

    public int Administrador { get; set; }

    public string Email { get; set; } = null!;

    public int EmailConfirmado { get; set; }

    public string CNPJEmpresa { get; set; } = null!;

    public virtual ICollection<UsuariosWeb> UsuariosWeb { get; set; } = new List<UsuariosWeb>();
}
