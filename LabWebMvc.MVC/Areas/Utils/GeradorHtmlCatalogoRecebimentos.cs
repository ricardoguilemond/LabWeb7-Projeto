using LabWebMvc.MVC.Models;
using System.Text;

namespace LabWebMvc.MVC.Areas.Utils
{
    public class GeradorHtmlCatalogoRecebimentos
    {
        public byte[] Gerar(DadosPdfCatalogoRecebimentos dados, Empresa? empresa)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"pt-BR\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            sb.AppendLine("<title>Relatório do Catálogo de Recebimentos</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("  body { font-family: Arial, sans-serif; font-size: 11px; color: #222; margin: 20px; }");
            sb.AppendLine("  h1 { font-size: 15px; margin: 0 0 2px 0; }");
            sb.AppendLine("  h2 { font-size: 13px; margin: 0 0 6px 0; }");
            sb.AppendLine("  .cabecalho { margin-bottom: 16px; }");
            sb.AppendLine("  .cabecalho p { margin: 1px 0; font-size: 11px; }");
            sb.AppendLine("  table { width: 100%; border-collapse: collapse; margin-bottom: 12px; }");
            sb.AppendLine("  th { background-color: #eeeeee; text-align: left; padding: 4px 6px; font-size: 10px; border: 1px solid #bbb; }");
            sb.AppendLine("  td { padding: 3px 6px; font-size: 10px; border: 1px solid #ddd; vertical-align: top; }");
            sb.AppendLine("  tr:nth-child(even) { background-color: #f9f9f9; }");
            sb.AppendLine("  .td-valor { text-align: right; white-space: nowrap; }");
            sb.AppendLine("  .tr-total { font-weight: bold; background-color: #e8f4e8; }");
            sb.AppendLine("  .secao-totais { margin-top: 16px; }");
            sb.AppendLine("  @media print { body { margin: 10px; } }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

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
            sb.AppendLine($"  <h2 style=\"margin-top:8px;\">Relatório do Catálogo de Recebimentos</h2>");
            sb.AppendLine($"  <p>Período: <strong>{dados.DataIni:dd/MM/yyyy}</strong> a <strong>{dados.DataFim:dd/MM/yyyy}</strong></p>");
            sb.AppendLine($"  <p>Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<table>");
            sb.AppendLine("  <thead><tr>");
            sb.AppendLine("    <th>Id</th>");
            sb.AppendLine("    <th>Data</th>");
            sb.AppendLine("    <th>Origem</th>");
            sb.AppendLine("    <th>Instituição</th>");
            sb.AppendLine("    <th>Paciente</th>");
            sb.AppendLine("    <th>Período</th>");
            sb.AppendLine("    <th style=\"text-align:right\">Total</th>");
            sb.AppendLine("  </tr></thead>");
            sb.AppendLine("  <tbody>");

            foreach (var rec in dados.Recebimentos)
            {
                sb.AppendLine("    <tr>");
                sb.AppendLine($"      <td>{rec.CatalogoId}</td>");
                sb.AppendLine($"      <td>{rec.DataRecebimento:dd/MM/yyyy}</td>");
                sb.AppendLine($"      <td>{EscHtml(rec.Origem)}</td>");
                sb.AppendLine($"      <td>{EscHtml(rec.SiglaInstituicao)} - {EscHtml(rec.NomeInstituicao)}</td>");
                sb.AppendLine($"      <td>{EscHtml(rec.NomePaciente)}</td>");
                sb.AppendLine($"      <td>{EscHtml(rec.PeriodoFaturamento ?? "—")}</td>");
                sb.AppendLine($"      <td class=\"td-valor\">{rec.ValorTotal:C2}</td>");
                sb.AppendLine("    </tr>");

                if (rec.Formas.Count > 0)
                {
                    sb.AppendLine("    <tr>");
                    sb.AppendLine("      <td colspan=\"7\" style=\"padding-left:24px;\">");
                    sb.AppendLine("        <strong>Formas:</strong> " + string.Join(" | ", rec.Formas.Select(f =>
                        $"{EscHtml(f.FormaNome)} / {EscHtml(f.ContaNome)}: {f.Valor:C2}")));
                    sb.AppendLine("      </td>");
                    sb.AppendLine("    </tr>");
                }
            }

            sb.AppendLine("    <tr class=\"tr-total\">");
            sb.AppendLine("      <td colspan=\"6\" style=\"text-align:right\">TOTAL GERAL:</td>");
            sb.AppendLine($"      <td class=\"td-valor\">{dados.ValorTotalGeral:C2}</td>");
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");

            sb.AppendLine("<div class=\"secao-totais\">");
            sb.AppendLine("  <h2>Totais por Forma de Recebimento</h2>");
            sb.AppendLine("  <table style=\"width:50%\">");
            sb.AppendLine("    <thead><tr><th>Forma</th><th style=\"text-align:right\">Valor</th></tr></thead>");
            sb.AppendLine("    <tbody>");
            foreach (var total in dados.TotaisPorForma)
            {
                sb.AppendLine($"      <tr><td>{EscHtml(total.Descricao)}</td><td class=\"td-valor\">{total.Valor:C2}</td></tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");

            sb.AppendLine("  <h2>Totais por Conta de Recebimento</h2>");
            sb.AppendLine("  <table style=\"width:50%\">");
            sb.AppendLine("    <thead><tr><th>Conta</th><th style=\"text-align:right\">Valor</th></tr></thead>");
            sb.AppendLine("    <tbody>");
            foreach (var total in dados.TotaisPorConta)
            {
                sb.AppendLine($"      <tr><td>{EscHtml(total.Descricao)}</td><td class=\"td-valor\">{total.Valor:C2}</td></tr>");
            }
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
            sb.AppendLine("</div>");

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
    }
}
