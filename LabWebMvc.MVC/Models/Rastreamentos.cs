namespace LabWebMvc.MVC.Models;

public partial class Rastreamentos
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public DateTime DataOcorrencia { get; set; }

    public string? SistemaUtilizado { get; set; }

    public string? VersaoSistema { get; set; }

    public string? OpcaoMenu { get; set; }

    public string? OperacaoRealizada { get; set; }

    public string? OperacaoComplementar { get; set; }

    public string? Falha { get; set; }

    public string? Exception { get; set; }

    public string? Iplocal { get; set; }

    public string? Ipexterno { get; set; }

    public string? NomeComputador { get; set; }
}