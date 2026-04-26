using System;
using System.Collections.Generic;

namespace LabWebMvc.MVC.Models;

public partial class IntegracaoDadosExecucao
{
    public int Id { get; set; }

    public int IntegracaoDadosLayoutId { get; set; }

    public DateTime Inicio { get; set; }

    public DateTime? Termino { get; set; }

    public bool Sucesso { get; set; }

    public string? Resumo { get; set; }

    public string? NomeServico { get; set; }

    public string? NomeArquivo { get; set; }

    public string? Header { get; set; }

    public string? Summary { get; set; }

    public virtual ICollection<IntegracaoDadosExecucaoArquivo> IntegracaoDadosExecucaoArquivo { get; set; } = new List<IntegracaoDadosExecucaoArquivo>();

    public virtual IntegracaoDadosLayout IntegracaoDadosLayout { get; set; } = null!;
}
