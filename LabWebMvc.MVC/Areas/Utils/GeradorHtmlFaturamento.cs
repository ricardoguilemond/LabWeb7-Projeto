using LabWebMvc.MVC.Models;
using System.Text;

namespace LabWebMvc.MVC.Areas.Utils
{
    public class GeradorHtmlFaturamento
    {
        public byte[] Gerar(DadosPdfFaturamento dados, Empresa? empresa)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"pt-BR\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            sb.AppendLine("<title>Relatório de Faturamento por Período</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("  body { font-family: Arial, sans-serif; font-size: 11px; color: #222; margin: 20px; }");
            sb.AppendLine("  h1 { font-size: 15px; margin: 0 0 2px 0; }");
            sb.AppendLine("  h2 { font-size: 13px; margin: 0 0 6px 0; }");
            sb.AppendLine("  .cabecalho { margin-bottom: 16px; }");
            sb.AppendLine("  .cabecalho p { margin: 1px 0; font-size: 11px; }");
            sb.AppendLine("  .secao-instituicao { margin-bottom: 24px; }");
            sb.AppendLine("  .titulo-instituicao { font-size: 12px; font-weight: bold; background-color: #d9ead3; padding: 4px 6px; margin-bottom: 4px; border-radius: 3px; }");
            sb.AppendLine("  table { width: 100%; border-collapse: collapse; margin-bottom: 4px; }");
            sb.AppendLine("  th { background-color: #eeeeee; text-align: left; padding: 4px 6px; font-size: 10px; border: 1px solid #bbb; }");
            sb.AppendLine("  td { padding: 3px 6px; font-size: 10px; border: 1px solid #ddd; vertical-align: top; }");
            sb.AppendLine("  tr:nth-child(even) { background-color: #f9f9f9; }");
            sb.AppendLine("  .td-itens { font-size: 9px; color: #444; }");
            sb.AppendLine("  .td-valor { text-align: right; white-space: nowrap; }");
            sb.AppendLine("  .tr-total-inst { font-weight: bold; background-color: #e8f4e8; }");
            sb.AppendLine("  .tr-total-geral { font-weight: bold; font-size: 12px; background-color: #c6efce; }");
            sb.AppendLine("  .resumo-tabelas { font-size: 10px; color: #555; margin-bottom: 14px; }");
            sb.AppendLine("  @media print { body { margin: 10px; } }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Cabeçalho da empresa
            sb.AppendLine("<div class=\"cabecalho\">");
            if (empresa != null)
            {
                if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial))
                    sb.AppendLine($"  <h1>{EscHtml(empresa.RazaoSocial)}</h1>");
                if (!string.IsNullOrWhiteSpace(empresa.Endereco))
                    sb.AppendLine($"  <p>{EscHtml(empresa.Endereco)}</p>");
                if (!string.IsNullOrWhiteSpace(empresa.CNPJ))
                    sb.AppendLine($"  <p>CNPJ: {EscHtml(empresa.CNPJ)}</p>");
            }
            sb.AppendLine($"  <h2 style=\"margin-top:8px;\">Relatório de Faturamento por Período</h2>");
            sb.AppendLine($"  <p>Período: <strong>{dados.DataIni:dd/MM/yyyy}</strong> a <strong>{dados.DataFim:dd/MM/yyyy}</strong></p>");
            sb.AppendLine($"  <p>Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine("</div>");

            // Tabelas utilizadas
            if (dados.TabelasUtilizadas.Count > 0)
            {
                sb.AppendLine("<div class=\"resumo-tabelas\">");
                sb.AppendLine($"  <strong>Tabelas de preços:</strong> {EscHtml(string.Join(" | ", dados.TabelasUtilizadas))}");
                sb.AppendLine("</div>");
            }

            // Agrupa exames por instituição
            var porInstituicao = dados.Exames
                .GroupBy(e => new { e.SiglaInstituicao, e.NomeInstituicao })
                .OrderBy(g => g.Key.SiglaInstituicao)
                .ToList();

            decimal totalGeral = 0m;

            foreach (var grupo in porInstituicao)
            {
                string tituloInst = string.IsNullOrWhiteSpace(grupo.Key.NomeInstituicao)
                    ? grupo.Key.SiglaInstituicao
                    : $"{grupo.Key.SiglaInstituicao} - {grupo.Key.NomeInstituicao}";

                decimal totalInst = grupo.Sum(e => e.ValorTotal);
                totalGeral += totalInst;

                sb.AppendLine("<div class=\"secao-instituicao\">");
                sb.AppendLine($"  <div class=\"titulo-instituicao\">{EscHtml(tituloInst)}</div>");
                sb.AppendLine("  <table>");
                sb.AppendLine("    <thead><tr>");
                sb.AppendLine("      <th style=\"width:60px\">Seq.</th>");
                sb.AppendLine("      <th style=\"width:80px\">Tabela</th>");
                if (dados.ExibirDataConclusao)
                    sb.AppendLine("      <th style=\"width:80px\">Data</th>");
                sb.AppendLine("      <th>Paciente</th>");
                sb.AppendLine("      <th>Itens</th>");
                sb.AppendLine("      <th style=\"width:80px;text-align:right\">Total</th>");
                sb.AppendLine("    </tr></thead>");
                sb.AppendLine("    <tbody>");

                foreach (var exame in grupo)
                {
                    string itensTexto = exame.Itens.Count > 0
                        ? string.Join(", ", exame.Itens.Select(i =>
                            dados.MostragemPrecos != 2
                                ? $"{EscHtml(i.Descricao)} ({i.ValorItem:C2})"
                                : EscHtml(i.Descricao)))
                        : "<em>—</em>";

                    sb.AppendLine("      <tr>");
                    sb.AppendLine($"        <td>{exame.Sequencial}</td>");
                    sb.AppendLine($"        <td>{EscHtml(exame.SiglaTabela)}</td>");
                    if (dados.ExibirDataConclusao)
                        sb.AppendLine($"        <td>{exame.DataExame?.ToString("dd/MM/yyyy") ?? "—"}</td>");
                    sb.AppendLine($"        <td>{EscHtml(exame.NomePaciente)}</td>");
                    sb.AppendLine($"        <td class=\"td-itens\">{itensTexto}</td>");
                    sb.AppendLine($"        <td class=\"td-valor\">{exame.ValorTotal:C2}</td>");
                    sb.AppendLine("      </tr>");
                }

                // Total por instituição
                int colunasTotais = dados.ExibirDataConclusao ? 6 : 5;
                sb.AppendLine("      <tr class=\"tr-total-inst\">");
                sb.AppendLine($"        <td colspan=\"{colunasTotais - 1}\" style=\"text-align:right\">Total {EscHtml(grupo.Key.SiglaInstituicao)}:</td>");
                sb.AppendLine($"        <td class=\"td-valor\">{totalInst:C2}</td>");
                sb.AppendLine("      </tr>");

                sb.AppendLine("    </tbody>");
                sb.AppendLine("  </table>");
                sb.AppendLine("</div>");
            }

            // Total geral
            sb.AppendLine("<table style=\"margin-top:12px;\">");
            sb.AppendLine("  <tbody>");
            sb.AppendLine("    <tr class=\"tr-total-geral\">");
            sb.AppendLine($"      <td style=\"text-align:right;font-size:12px;\"><strong>TOTAL GERAL:</strong></td>");
            sb.AppendLine($"      <td class=\"td-valor\" style=\"width:100px;font-size:12px;\"><strong>{totalGeral:C2}</strong></td>");
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");

            // Quantitativo de itens de exames realizados
            if (dados.QuantitativoItens.Count > 0)
            {
                sb.AppendLine("<div style=\"margin-top:24px;\">");
                sb.AppendLine("  <h2 style=\"font-size:14px;\">QUANTITATIVO DE ITENS DE EXAMES REALIZADOS:</h2>");
                sb.AppendLine("  <pre style=\"font-family:Courier New,monospace;font-size:12px;font-weight:bold;color:#333;line-height:1.3;margin:0;\">");
                sb.AppendLine(EscHtml(FormatarLinhaPontilhada("Folha de Exame, Item", "Quantidade")));

                foreach (var item in dados.QuantitativoItens)
                {
                    string descricao = EscHtml(item.DescricaoCompleta);
                    string quantidade = item.Quantidade.ToString("N0");
                    sb.AppendLine(EscHtml(FormatarLinhaPontilhada(descricao, quantidade)));
                }

                sb.AppendLine("  </pre>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscHtml(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string FormatarLinhaPontilhada(string descricao, string quantidade, int totalCaracteres = 100)
        {
            int pontos = totalCaracteres - (descricao.Length + quantidade.Length);
            if (pontos < 1) pontos = 1;
            return descricao + new string('.', pontos) + quantidade;
        }
    }
}
