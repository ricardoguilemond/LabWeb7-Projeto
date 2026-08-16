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
        //Feito pelo Qoder em 16/08/2026 — totais por origem (Portaria/Instituição/Faturamento)
        public List<TotalCatalogoDto> TotaisPorOrigem { get; set; } = [];
        //..Qoder
        public decimal ValorTotalGeral => Recebimentos.Sum(r => r.ValorTotal);
        //Feito pelo Qoder em 16/08/2026 — soma dos descontos concedidos no período
        public decimal ValorDescontoGeral => Recebimentos.Sum(r => r.ValorDesconto);
        // Quantidade de recebimentos com desconto concedido
        public int QuantidadeDescontos => Recebimentos.Count(r => r.ValorDesconto > 0);
        //..Qoder
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
        //Feito pelo Qoder em 16/08/2026 — desconto concedido no recebimento (0 = sem desconto)
        public decimal ValorDesconto { get; set; }
        //..Qoder
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
        //Feito pelo Qoder em 16/08/2026 — quantidade de itens (linhas) do grupo
        public int Quantidade { get; set; }
        //..Qoder
    }
}
