using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.Models;
public partial class Requisitar
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public int ClasseExamesId { get; set; }
    public string ClasseExamesNome { get; set; } = null!;
    public int ExameId { get; set; }
    public int OrdemItem { get; set; }
    public string? RefExame { get; set; }
    public string? RefItem { get; set; }
    public string ContaExame { get; set; } = null!;
    public int InstituicaoId { get; set; }
    public int? PostoId { get; set; }
    public int TabelaExamesId { get; set; }
    public int MedicoId { get; set; }
    public string? LaboratorioApoio { get; set; }
    public string? ControleApoio { get; set; }
    public string? LaboratorioExterno { get; set; }
    public string? MaterialSaida { get; set; }
    public string? MaterialRetorno { get; set; }
    public string? Descricao { get; set; }
    
    //Feito pelo Qoder em 04/06/2026
    // Campo Resultado removido: não era utilizado pelo usuário.
    // O resultado relevante está na tabela ItensExamesRealizados (para laudos e relatórios).
    // public string? Resultado { get; set; }  <-- REMOVIDO
    
    public string? UnidadeMedida { get; set; }
    public string? Referencia { get; set; }
    public decimal? ValorItem { get; set; }
    public byte[]? Laudo { get; set; }
    public int Etiquetas { get; set; }
    public DateTime DataIni { get; set; }
    public DateTime? DataEntregaParcial { get; set; }
    public int Liberado { get; set; }
    public int Baixado { get; set; }

    //Feito pelo Kiro em 03/05/2026
    // Vínculo lógico com ExamesRealizados.Id — SEM FK física no banco.
    // Identifica o código do exame ao qual estes itens pertencem.
    public int? ExameRealizadoId { get; set; }
    //..Kiro

    public virtual ClasseExames ClasseExames { get; set; } = null!;
    public virtual Instituicao Instituicao { get; set; } = null!;
    public virtual Postos? Posto { get; set; }
    public virtual Medicos Medicos { get; set; } = null!;
    public virtual Pacientes Pacientes { get; set; } = null!;
    public virtual TabelaExames TabelaExames { get; set; } = null!;
}