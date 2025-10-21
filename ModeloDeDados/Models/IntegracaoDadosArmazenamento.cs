using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class IntegracaoDadosArmazenamento
{
    public int Id { get; set; }

    public string? Senha { get; set; }

    public int TipoArmazenamento { get; set; }

    public string? Host { get; set; }

    public int? Usuario { get; set; }

    public string? UsuarioLogin { get; set; }

    public virtual ICollection<IntegracaoDadosConfiguracao> IntegracaoDadosConfiguracao { get; set; } = new List<IntegracaoDadosConfiguracao>();
}
