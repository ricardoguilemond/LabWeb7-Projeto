namespace LabWebMvc.MVC.Models;

public partial class IntegracaoDadosLayout
{
    public int Id { get; set; }

    public int IntegracaoDadosConfiguracaoId { get; set; }

    public string Descricao { get; set; } = null!;

    public int TipoServico { get; set; }

    public bool Exportacao { get; set; }

    public bool Habilitado { get; set; }

    public DateTime? DataInicial { get; set; }

    public DateTime? DataFinal { get; set; }

    public virtual IntegracaoDadosConfiguracao IntegracaoDadosConfiguracao { get; set; } = null!;

    public virtual ICollection<IntegracaoDadosExecucao> IntegracaoDadosExecucao { get; set; } = [];

    public virtual ICollection<LogArquivos> LogArquivos { get; set; } = [];
}