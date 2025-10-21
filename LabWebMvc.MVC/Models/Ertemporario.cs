namespace LabWebMvc.MVC.Models;

public partial class ERTemporario
{
    public int Id { get; set; }

    public int ExameId { get; set; }

    public int PacienteId { get; set; }

    public int InstituicaoId { get; set; }

    public int TabelaExamesId { get; set; }

    public int MedicoId { get; set; }

    public int Sequencial { get; set; }

    public int ClasseExamesId { get; set; }

    public string? HistoricoClinico { get; set; }

    public DateTime? DataIni { get; set; }

    public DateTime? DataFim { get; set; }

    public int Liberacao { get; set; }

    public DateTime? DataExame { get; set; }

    public DateTime? DataEntrega { get; set; }

    public int Baixado { get; set; }
}