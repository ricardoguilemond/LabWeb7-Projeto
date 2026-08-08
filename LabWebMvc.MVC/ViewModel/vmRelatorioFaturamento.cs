using Microsoft.AspNetCore.Mvc.Rendering;

namespace LabWebMvc.MVC.ViewModel;

public class vmRelatorioFaturamento
{
    public DateTime DataIni { get; set; } = DateTime.Now.AddMonths(-1).Date;
    public DateTime DataFim { get; set; } = DateTime.Now.Date;

    public List<SelectListItem> Instituicoes { get; set; } = [];
    public List<int> InstituicoesSelecionadas { get; set; } = [];

    public List<SelectListItem> Tabelas { get; set; } = [];
    public List<int> TabelasSelecionadas { get; set; } = [];

    /// <summary>
    /// 0 = Alfabética (Nome Paciente)
    /// 1 = Sigla Instituição + Sequencial
    /// 2 = Data, Sigla Instituição + Sequencial
    /// </summary>
    public int Ordenacao { get; set; } = 2;

    /// <summary>
    /// 0 = Aceitar zerados nos Exames dos Pacientes
    /// 1 = Aceitar todos os zerados baseado no Plano de Exames
    /// 2 = Não imprimir itens com valores zerados
    /// </summary>
    public int MostragemPrecos { get; set; } = 2;

    public bool DuasColunas { get; set; } = true;
    public bool IncluirBaixados { get; set; } = false;

    /// <summary>
    /// Quando true, exibe a Data de Conclusao do Exame (DataFim) no relatório.
    /// Padrao: false (igual ao Delphi).
    /// </summary>
    public bool ExibirDataConclusao { get; set; } = false;

    /// <summary>
    /// 0 = PDF (padrão)
    /// 1 = HTML
    /// 2 = Word (.docx)
    /// </summary>
    public int FormatoSaida { get; set; } = 0;
}
