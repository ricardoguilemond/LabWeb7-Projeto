using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.Utils
{
    /// <summary>
    /// Dados brutos para geração do relatório do Catálogo de Recebimentos.
    /// </summary>
    public class DadosPdfCatalogoRecebimentos
    {
        public DateTime DataIni { get; set; }
        public DateTime DataFim { get; set; }
        public int Ordenacao { get; set; }
        public List<RecebimentoCatalogoDto> Recebimentos { get; set; } = [];
        public List<TotalCatalogoDto> TotaisPorForma { get; set; } = [];
        public List<TotalCatalogoDto> TotaisPorConta { get; set; } = [];
        public decimal ValorTotalGeral => Recebimentos.Sum(r => r.ValorTotal);
    }

    public class RecebimentoCatalogoDto
    {
        public int CatalogoId { get; set; }
        public DateTime DataRecebimento { get; set; }
        public string Origem { get; set; } = "";
        public string SiglaInstituicao { get; set; } = "";
        public string NomeInstituicao { get; set; } = "";
        public string NomePaciente { get; set; } = "";
        public string? PeriodoFaturamento { get; set; }
        public decimal ValorTotal { get; set; }
        public string? Observacao { get; set; }
        public List<FormaRecebimentoCatalogoDto> Formas { get; set; } = [];
        public List<ExameCatalogoDto> Exames { get; set; } = [];
    }

    public class FormaRecebimentoCatalogoDto
    {
        public string FormaNome { get; set; } = "";
        public string ContaNome { get; set; } = "";
        public decimal Valor { get; set; }
        public DateTime DataRecebimento { get; set; }
        public string? Observacao { get; set; }
    }

    public class ExameCatalogoDto
    {
        public int ExameRealizadoId { get; set; }
        public int Sequencial { get; set; }
        public decimal Valor { get; set; }
    }

    public class TotalCatalogoDto
    {
        public string Descricao { get; set; } = "";
        public decimal Valor { get; set; }
    }
}
