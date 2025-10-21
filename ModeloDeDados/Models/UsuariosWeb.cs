using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabWebMvc.MVC.Models;

public partial class UsuariosWeb
{
    public int Id { get; set; }

    [ForeignKey("Senhas")]
    public int SenhaId { get; set; }

    public string CPFUsuario { get; set; } = null!;

    public DateTime DataNascimentoUsuario { get; set; }

    public string CNPJEmpresa { get; set; } = null!;

    public DateTime DataCadastro { get; set; }

    public virtual Senhas Senhas { get; set; } = null!;
}
