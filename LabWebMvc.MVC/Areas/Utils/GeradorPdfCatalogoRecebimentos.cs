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
        //Feito pelo Qoder em 16/08/2026
        // Altura de uma linha de texto dentro da célula (quebra de linha).
        private const double AlturaLinhaTexto = 12;
        //..Qoder

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
            //Feito pelo Qoder em 16/08/2026
            // Formato limpo: larguras somam exatamente a AreaUtil (antes 555pt > 515pt,
            // estourando a margem direita) e o conteúdo das células quebra linha por
            // medição de texto (antes o texto invadia a coluna vizinha).
            // 16/08/2026: coluna Desconto incluída entre Período e Total.
            double[] colunas = { 25, 57, 50, 130, 120, 48, 45, 0 };
            double somaParciais = 0;
            for (int i = 0; i < colunas.Length - 1; i++) somaParciais += colunas[i];
            colunas[colunas.Length - 1] = AreaUtil - somaParciais;

            string[] headers = { "Id", "Data", "Origem", "Instituição", "Paciente", "Período", "Desconto", "Total" };
            //..Qoder

            y = DesenharCabecalhoTabela(gfx, colunas, headers, y);

            foreach (var rec in dados.Recebimentos)
            {
                string[] valores =
                {
                    rec.CatalogoId.ToString(),
                    rec.DataRecebimento.ToString("dd/MM/yyyy"),
                    rec.Origem,
                    $"{rec.SiglaInstituicao} - {rec.NomeInstituicao}",
                    rec.NomePaciente,
                    rec.PeriodoFaturamento ?? "",
                    //Feito pelo Qoder em 16/08/2026 — desconto do registro
                    rec.ValorDesconto.ToString("N2"),
                    //..Qoder
                    rec.ValorTotal.ToString("N2")
                };

                // Quebra o conteúdo de cada célula em linhas que cabem na coluna
                var linhasPorColuna = new List<string>[valores.Length];
                int maxLinhas = 1;
                for (int i = 0; i < valores.Length; i++)
                {
                    linhasPorColuna[i] = QuebrarLinhas(gfx, valores[i], _fontNormal, colunas[i] - 6);
                    if (linhasPorColuna[i].Count > maxLinhas) maxLinhas = linhasPorColuna[i].Count;
                }
                double alturaLinha = maxLinhas * AlturaLinhaTexto + 6;

                if (y + alturaLinha > LimiteY)
                {
                    var owner = gfx.PdfPage.Owner;
                    gfx.Dispose();
                    var page = owner.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = MargemTopo;
                    y = DesenharCabecalho(gfx, dados, null, y);
                    y = DesenharCabecalhoTabela(gfx, colunas, headers, y);
                }

                double x = MargemEsquerda;
                for (int i = 0; i < valores.Length; i++)
                {
                    //Feito pelo Qoder em 16/08/2026 — Desconto e Total alinhados à direita
                    var format = i >= valores.Length - 2 ? XStringFormats.TopRight : XStringFormats.TopLeft;
                    //..Qoder
                    for (int l = 0; l < linhasPorColuna[i].Count; l++)
                    {
                        gfx.DrawString(linhasPorColuna[i][l], _fontNormal, XBrushes.Black,
                            new XRect(x + 3, y + 3 + l * AlturaLinhaTexto, colunas[i] - 6, AlturaLinhaTexto), format);
                    }
                    x += colunas[i];
                }
                y += alturaLinha;
                gfx.DrawLine(XPens.LightGray, MargemEsquerda, y, MargemEsquerda + AreaUtil, y);
            }

            // Total geral
            y += 4;
            double larguraTotal = colunas[colunas.Length - 1];
            gfx.DrawRectangle(XBrushes.LightGreen, MargemEsquerda, y, AreaUtil, 18);
            gfx.DrawString("TOTAL GERAL:", _fontTotal, XBrushes.Black, new XRect(MargemEsquerda, y + 2, AreaUtil - larguraTotal - 4, 14), XStringFormats.TopRight);
            gfx.DrawString(dados.ValorTotalGeral.ToString("N2"), _fontTotal, XBrushes.Black, new XRect(MargemEsquerda + AreaUtil - larguraTotal + 3, y + 2, larguraTotal - 6, 14), XStringFormats.TopRight);
            y += 22;

            return y;
        }

        private double DesenharCabecalhoTabela(XGraphics gfx, double[] colunas, string[] headers, double y)
        {
            gfx.DrawRectangle(XBrushes.LightGray, MargemEsquerda, y, AreaUtil, 18);
            double x = MargemEsquerda;
            for (int i = 0; i < headers.Length; i++)
            {
                //Feito pelo Qoder em 16/08/2026 — Desconto e Total alinhados à direita
                var format = i >= headers.Length - 2 ? XStringFormats.TopRight : XStringFormats.TopLeft;
                //..Qoder
                gfx.DrawString(headers[i], _fontNormalBold, XBrushes.Black, new XRect(x + 3, y + 2, colunas[i] - 6, 14), format);
                x += colunas[i];
            }
            return y + 18;
        }

        /// <summary>
        /// Quebra o texto em linhas que cabem em larguraMax; palavras maiores que a
        /// coluna são cortadas com reticências.
        /// </summary>
        private static List<string> QuebrarLinhas(XGraphics gfx, string texto, XFont fonte, double larguraMax)
        {
            var linhas = new List<string>();
            if (string.IsNullOrWhiteSpace(texto))
            {
                linhas.Add("");
                return linhas;
            }

            var fila = new Queue<string>(texto.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var linhaAtual = "";

            while (fila.Count > 0)
            {
                var palavra = fila.Dequeue();

                // Palavra maior que a coluna: corta com reticências
                while (gfx.MeasureString(palavra, fonte).Width > larguraMax && palavra.Length > 2)
                {
                    int corte = palavra.Length - 1;
                    while (corte > 2 && gfx.MeasureString(palavra.Substring(0, corte) + "...", fonte).Width > larguraMax)
                        corte--;
                    linhas.Add(palavra.Substring(0, corte) + "...");
                    palavra = palavra.Substring(corte);
                }

                var teste = linhaAtual.Length == 0 ? palavra : linhaAtual + " " + palavra;
                if (gfx.MeasureString(teste, fonte).Width <= larguraMax)
                {
                    linhaAtual = teste;
                }
                else
                {
                    linhas.Add(linhaAtual);
                    linhaAtual = palavra;
                }
            }

            linhas.Add(linhaAtual);
            return linhas;
        }

        private double DesenharTotais(XGraphics gfx, DadosPdfCatalogoRecebimentos dados, double y)
        {
            y += 10;
            gfx.DrawString("Totais por Forma de Recebimento", _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;

            foreach (var total in dados.TotaisPorForma)
            {
                //Feito pelo Qoder em 16/08/2026 — quantidade de itens entre parênteses
                gfx.DrawString($"({total.Quantidade}) {total.Descricao}: {total.Valor:N2}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 10, y, AreaUtil - 10, 14), XStringFormats.TopLeft);
                //..Qoder
                y += 12;
            }

            y += 8;
            gfx.DrawString("Totais por Conta de Recebimento", _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;

            foreach (var total in dados.TotaisPorConta)
            {
                //Feito pelo Qoder em 16/08/2026 — quantidade de itens entre parênteses
                gfx.DrawString($"({total.Quantidade}) {total.Descricao}: {total.Valor:N2}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 10, y, AreaUtil - 10, 14), XStringFormats.TopLeft);
                //..Qoder
                y += 12;
            }

            //Feito pelo Qoder em 16/08/2026 — seção de totais dos descontos
            y += 8;
            gfx.DrawString("Totais dos Descontos", _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;
            gfx.DrawString($"({dados.QuantidadeDescontos}) Total de Descontos: {dados.ValorDescontoGeral:N2}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 10, y, AreaUtil - 10, 14), XStringFormats.TopLeft);
            y += 12;
            //..Qoder

            //Feito pelo Qoder em 16/08/2026 — seção de totais por origem ao final
            y += 8;
            gfx.DrawString("Totais por Origem", _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;
            foreach (var total in dados.TotaisPorOrigem)
            {
                gfx.DrawString($"({total.Quantidade}) {total.Descricao}: {total.Valor:N2}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 10, y, AreaUtil - 10, 14), XStringFormats.TopLeft);
                y += 12;
            }
            //..Qoder

            return y;
        }

    }
}
