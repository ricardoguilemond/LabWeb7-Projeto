namespace LabWebMvc.MVC.Models;

public partial class IntegracaoDadosConfiguracao
{
    public int Id { get; set; }

    public int IntegracaoDadosArmazenamentoId { get; set; }

    public string? PastaSaida { get; set; }

    public string? PastaEntrada { get; set; }

    public string NomeArquivo { get; set; } = null!;

    public string HoraExecucao { get; set; } = null!;

    public string? HoraEncerramento { get; set; }

    public int? DiaExecucao { get; set; }

    public int Periodicidade { get; set; }

    public bool IntegraUmaUnicaVezNoDia { get; set; }

    public int PausaDoEventoEmMinutos { get; set; }

    public string? PastaEntradaProcessado { get; set; }

    public string? PastaEntradaProcessadoErro { get; set; }

    public string? PastaEntradaProcessadoParcial { get; set; }

    public int? UsuarioPadrao { get; set; }

    public virtual IntegracaoDadosArmazenamento IntegracaoDadosArmazenamento { get; set; } = null!;

    public virtual ICollection<IntegracaoDadosLayout> IntegracaoDadosLayout { get; set; } = [];
}