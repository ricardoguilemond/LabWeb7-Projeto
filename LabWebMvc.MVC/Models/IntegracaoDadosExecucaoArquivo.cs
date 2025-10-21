namespace LabWebMvc.MVC.Models;

public partial class IntegracaoDadosExecucaoArquivo
{
    public int Id { get; set; }

    public int IntegracaoDadosExecucaoId { get; set; }

    public string? NomeArquivo { get; set; }

    public int Status { get; set; }

    public string? Resumo { get; set; }

    public string? NomeArquivoProcessado { get; set; }

    public string? NomeArquivoGerado { get; set; }

    public virtual IntegracaoDadosExecucao IntegracaoDadosExecucao { get; set; } = null!;
}