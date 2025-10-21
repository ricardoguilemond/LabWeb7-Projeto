namespace LabWebMvc.MVC.Models;

public partial class FichasInternas
{
    public int Id { get; set; }

    public string? NomeFicha { get; set; }

    public string? ContaExame { get; set; }

    public string? Descricao { get; set; }

    public string? Resultado { get; set; }

    public string? MapaHorizontal { get; set; }

    public int ExamesRealizadosId { get; set; }

    public int PacienteId { get; set; }

    public int MedicoId { get; set; }

    public int InstituicaoId { get; set; }

    public DateTime DataExame { get; set; }

    public string? ControleApoio { get; set; }

    public int Sequencial { get; set; }

    public string? HistoricoClinico { get; set; }

    public DateTime DataIni { get; set; }

    public DateTime? DataFim { get; set; }

    public int Pagina { get; set; }

    public string? Coluna1 { get; set; }

    public string? Coluna2 { get; set; }

    public string? Coluna3 { get; set; }

    public string? Coluna4 { get; set; }

    public string? Coluna5 { get; set; }

    public string? Coluna6 { get; set; }

    public string? Coluna7 { get; set; }

    public string? Coluna8 { get; set; }

    public string? Coluna9 { get; set; }

    public string? Coluna10 { get; set; }

    public string? Coluna11 { get; set; }

    public string? Coluna12 { get; set; }

    public string? Coluna13 { get; set; }

    public string? Coluna14 { get; set; }

    public string? Coluna15 { get; set; }

    public string? Coluna16 { get; set; }

    public string? Coluna17 { get; set; }

    public string? Coluna18 { get; set; }

    public virtual ExamesRealizados ExamesRealizados { get; set; } = null!;

    public virtual Instituicao Instituicao { get; set; } = null!;

    public virtual Medicos Medicos { get; set; } = null!;

    public virtual Pacientes Pacientes { get; set; } = null!;
}