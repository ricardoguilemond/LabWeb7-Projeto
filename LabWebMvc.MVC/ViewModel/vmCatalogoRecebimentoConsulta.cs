using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabWebMvc.MVC.ViewModel;

/// <summary>
/// ViewModel para tela de consulta do Catálogo de Recebimentos.
/// </summary>
public class vmCatalogoRecebimentoConsulta
{
    public DateTime? DataIni { get; set; }

    public DateTime? DataFim { get; set; }

    public int? InstituicaoId { get; set; }

    public List<SelectListItem> Instituicoes { get; set; } = [];

    public int? PacienteId { get; set; }

    public int? FormaRecebimentoId { get; set; }

    public List<SelectListItem> FormasRecebimento { get; set; } = [];

    public int? ContaRecebimentoId { get; set; }

    public List<SelectListItem> ContasRecebimento { get; set; } = [];

    public int? Status { get; set; }
}
