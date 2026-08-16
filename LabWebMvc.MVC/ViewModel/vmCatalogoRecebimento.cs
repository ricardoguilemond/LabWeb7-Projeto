using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel;

/// <summary>
/// ViewModel para tela de lançamento do Catálogo de Recebimentos por instituição/período.
/// </summary>
public class vmCatalogoRecebimento
{
    [Required(ErrorMessage = "Informe a data inicial.")]
    public DateTime DataIni { get; set; } = DateTime.Now.AddMonths(-1).Date;

    [Required(ErrorMessage = "Informe a data final.")]
    public DateTime DataFim { get; set; } = DateTime.Now.Date;

    public int? InstituicaoId { get; set; }

    public List<SelectListItem> Instituicoes { get; set; } = [];
}

/// <summary>
/// Item de exame disponível para seleção no catálogo.
/// </summary>
public class vmCatalogoRecebimentoItem
{
    public int ExameRealizadoId { get; set; }
    public int PacienteId { get; set; }
    public string NomePaciente { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public int InstituicaoId { get; set; }
    public string SiglaInstituicao { get; set; } = string.Empty;
    public int Sequencial { get; set; }
    public DateTime? DataExame { get; set; }
    public decimal ValorTotal { get; set; }
}

/// <summary>
/// Forma de pagamento vinculada a um catálogo.
/// </summary>
public class vmCatalogoRecebimentoForma
{
    public int FormaRecebimentoId { get; set; }

    public int ContaRecebimentoId { get; set; }

    public decimal Valor { get; set; }

    public DateTime DataRecebimento { get; set; } = DateTime.Now.Date;

    public string? Observacao { get; set; }
}

/// <summary>
/// DTO para persistência de um novo catálogo de recebimentos.
/// </summary>
public class vmCatalogoRecebimentoSalvar
{
    public int Origem { get; set; } = 2; // 1=Portaria, 2=Faturamento

    public int InstituicaoId { get; set; }

    public int PacienteId { get; set; }

    public string? PeriodoFaturamento { get; set; }

    public decimal ValorTotal { get; set; }

    //Feito pelo Qoder em 16/08/2026 — desconto concedido no recebimento (0 = sem desconto)
    public decimal ValorDesconto { get; set; }
    //..Qoder

    //Feito pelo Qoder em 16/08/2026 — true: não receber do paciente; cobrar da Instituição (Status Pendente)
    public bool CobrancaInstituicao { get; set; }
    //..Qoder

    public DateTime DataRecebimento { get; set; } = DateTime.Now.Date;

    public string? Observacao { get; set; }

    public List<int> ExamesRealizadosIds { get; set; } = [];

    public List<vmCatalogoRecebimentoForma> Formas { get; set; } = [];
}

//Feito pelo Qoder em 16/08/2026
/// <summary>
/// DTO para baixa de título pendente de cobrança à instituição.
/// </summary>
public class vmReceberPendente
{
    public int CatalogoId { get; set; }

    public string? PeriodoFaturamento { get; set; }

    public List<vmCatalogoRecebimentoForma> Formas { get; set; } = [];
}
//..Qoder
