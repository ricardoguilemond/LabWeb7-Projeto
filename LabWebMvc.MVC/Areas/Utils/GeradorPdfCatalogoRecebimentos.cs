using LabWebMvc.MVC.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LabWebMvc.MVC.Areas.Utils
{
    public class GeradorPdfCatalogoRecebimentos
    {
        private const double MargemEsquerda = 40;
        private const double MargemDireita = 40;
        private const double MargemTopo = 30;
        private const double ReservaRodape = 40;
        private const double LarguraPagina = 595.28;
        private const double AlturaPagina = 841.89;
        private const double AreaUtil = LarguraPagina - MargemEsquerda - MargemDireita;
        private const double LimiteY = AlturaPagina - ReservaRodape;

        private readonly XFont _fontTitulo = new("Arial", 14, XFontStyle.Bold);
        private readonly XFont _fontSubtitulo = new("Arial", 10, XFontStyle.Regular);
        private readonly XFont _fontNormal = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontNormalBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontTotal = new("Arial", 10, XFontStyle.Bold);

        public byte[] Gerar(DadosPdfCatalogoRecebimentos dados, Empresa? empresa)
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = "Relatório do Catálogo de Recebimentos";
            pdfDocument.Info.Author = "LabWeb7";

            PdfPage page = pdfDocument.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            XGraphics gfx = XGraphics.FromPdfPage(page);
            double y = MargemTopo;

            y = DesenharCabecalho(gfx, dados, empresa, y);
            y = DesenharTabela(gfx, dados, y);
            y = DesenharTotais(gfx, dados, y);

            using var stream = new MemoryStream();
            pdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private double DesenharCabecalho(XGraphics gfx, DadosPdfCatalogoRecebimentos dados, Empresa? empresa, double y)
        {
            if (empresa != null)
            {
                if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial))
                {
                    gfx.DrawString(empresa.RazaoSocial, _fontTitulo, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 20), XStringFormats.TopLeft);
                    y += 18;
                }
                if (!string.IsNullOrWhiteSpace(empresa.Endereco))
                {
                    gfx.DrawString(empresa.Endereco, _fontSubtitulo, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
                    y += 12;
                }
                if (!string.IsNullOrWhiteSpace(empresa.CNPJ))
                {
                    gfx.DrawString($"CNPJ: {empresa.CNPJ}", _fontSubtitulo, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
                    y += 12;
                }
            }

            gfx.DrawString("Relatório do Catálogo de Recebimentos", _fontTitulo, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 20), XStringFormats.TopLeft);
            y += 18;
            gfx.DrawString($"Período: {dados.DataIni:dd/MM/yyyy} a {dados.DataFim:dd/MM/yyyy}  |  Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}", _fontSubtitulo, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 22;

            return y;
        }

        private double DesenharTabela(XGraphics gfx, DadosPdfCatalogoRecebimentos dados, double y)
        {
            double[] colunas = { 40, 60, 60, 140, 140, 55, 60 };
            double x = MargemEsquerda;
            string[] headers = { "Id", "Data", "Origem", "Instituição", "Paciente", "Período", "Total" };

            // Cabeçalho
            gfx.DrawRectangle(XBrushes.LightGray, MargemEsquerda, y, AreaUtil, 18);
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawString(headers[i], _fontNormalBold, XBrushes.Black, new XRect(x, y + 2, colunas[i], 14), XStringFormats.TopLeft);
                x += colunas[i];
            }
            y += 18;

            foreach (var rec in dados.Recebimentos)
            {
                if (y > LimiteY)
                {
                    gfx.Dispose();
                    var page = gfx.PdfPage.Owner.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = MargemTopo;
                    y = DesenharCabecalho(gfx, dados, null, y);
                }

                x = MargemEsquerda;
                string[] valores =
                {
                    rec.CatalogoId.ToString(),
                    rec.DataRecebimento.ToString("dd/MM/yyyy"),
                    rec.Origem,
                    $"{rec.SiglaInstituicao} - {rec.NomeInstituicao}",
                    rec.NomePaciente,
                    rec.PeriodoFaturamento ?? "",
                    rec.ValorTotal.ToString("N2")
                };

                for (int i = 0; i < valores.Length; i++)
                {
                    var format = i == valores.Length - 1 ? XStringFormats.TopRight : XStringFormats.TopLeft;
                    var rect = new XRect(x, y + 2, colunas[i], 14);
                    gfx.DrawString(Truncar(valores[i], 35), _fontNormal, XBrushes.Black, rect, format);
                    x += colunas[i];
                }
                y += 14;
            }

            // Total geral
            y += 4;
            gfx.DrawRectangle(XBrushes.LightGreen, MargemEsquerda, y, AreaUtil, 18);
            gfx.DrawString("TOTAL GERAL:", _fontTotal, XBrushes.Black, new XRect(MargemEsquerda, y + 2, AreaUtil - 70, 14), XStringFormats.TopRight);
            gfx.DrawString(dados.ValorTotalGeral.ToString("N2"), _fontTotal, XBrushes.Black, new XRect(MargemEsquerda + AreaUtil - 65, y + 2, 60, 14), XStringFormats.TopRight);
            y += 22;

            return y;
        }

        private double DesenharTotais(XGraphics gfx, DadosPdfCatalogoRecebimentos dados, double y)
        {
            y += 10;
            gfx.DrawString("Totais por Forma de Recebimento", _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;

            foreach (var total in dados.TotaisPorForma)
            {
                gfx.DrawString($"{total.Descricao}: {total.Valor:N2}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 10, y, AreaUtil - 10, 14), XStringFormats.TopLeft);
                y += 12;
            }

            y += 8;
            gfx.DrawString("Totais por Conta de Recebimento", _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;

            foreach (var total in dados.TotaisPorConta)
            {
                gfx.DrawString($"{total.Descricao}: {total.Valor:N2}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 10, y, AreaUtil - 10, 14), XStringFormats.TopLeft);
                y += 12;
            }

            return y;
        }

        private static string Truncar(string texto, int maximo)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto.Length <= maximo ? texto : texto[..maximo] + "...";
        }
    }
}
