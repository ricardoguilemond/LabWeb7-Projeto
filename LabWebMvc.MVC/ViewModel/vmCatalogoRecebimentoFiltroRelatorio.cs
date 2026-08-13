using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel;

/// <summary>
/// ViewModel para filtro do relatório do Catálogo de Recebimentos.
/// </summary>
public class vmCatalogoRecebimentoFiltroRelatorio
{
    [Required(ErrorMessage = "Informe a data inicial.")]
    public DateTime DataIni { get; set; } = DateTime.Now.AddMonths(-1).Date;

    [Required(ErrorMessage = "Informe a data final.")]
    public DateTime DataFim { get; set; } = DateTime.Now.Date;

    public int? InstituicaoId { get; set; }

    public List<SelectListItem> Instituicoes { get; set; } = [];

    public int? FormaRecebimentoId { get; set; }

    public List<SelectListItem> FormasRecebimento { get; set; } = [];

    public int? ContaRecebimentoId { get; set; }

    public List<SelectListItem> ContasRecebimento { get; set; } = [];

    /// <summary>
    /// 0 = PDF (padrão)
    /// 1 = HTML
    /// 2 = Word (.docx)
    /// </summary>
    public int FormatoSaida { get; set; } = 0;

    /// <summary>
    /// 0 = Data de recebimento
    /// 1 = Instituição
    /// 2 = Forma de pagamento
    /// </summary>
    public int Ordenacao { get; set; } = 0;
}
