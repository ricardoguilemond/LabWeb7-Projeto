using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabWebMvc.MVC.ViewModel;

/// <summary>
/// ViewModel para a tela de Manutenção de Faturamento.
/// Contém os filtros de busca e a lista de instituições para o dropdown.
/// Os dados do exame e itens são carregados via AJAX (JSON).
/// </summary>
public class vmManutencaoFaturamento
{
    /// <summary>
    /// Lista de instituições disponíveis para o filtro de busca.
    /// </summary>
    public List<SelectListItem> Instituicoes { get; set; } = [];

    /// <summary>
    /// Sigla da instituição selecionada no filtro.
    /// </summary>
    public string? SiglaInstituicao { get; set; }

    /// <summary>
    /// Número sequencial do exame (mutuamente exclusivo com CodigoExame).
    /// </summary>
    public string? Sequencial { get; set; }

    /// <summary>
    /// Código do exame realizado (mutuamente exclusivo com Sequencial).
    /// </summary>
    public int? CodigoExame { get; set; }
}
