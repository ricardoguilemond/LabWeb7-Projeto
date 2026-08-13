using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.Utils
{
    public class GeradorWordCatalogoRecebimentos
    {
        public byte[] Gerar(DadosPdfCatalogoRecebimentos dados, Empresa? empresa)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                var body = mainPart.Document.Body!;

                AdicionarEstilos(mainPart);

                var sectPr = new SectionProperties(
                    new PageSize { Width = 16838, Height = 11906, Orient = PageOrientationValues.Landscape },
                    new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720 });
                body.AppendChild(sectPr);

                if (empresa != null)
                {
                    if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial))
                        body.InsertBefore(CriarParagrafo(empresa.RazaoSocial, "Titulo"), sectPr);
                    if (!string.IsNullOrWhiteSpace(empresa.Endereco))
                        body.InsertBefore(CriarParagrafo(empresa.Endereco, "SubTitulo"), sectPr);
                    if (!string.IsNullOrWhiteSpace(empresa.CNPJ))
                        body.InsertBefore(CriarParagrafo($"CNPJ: {empresa.CNPJ}", "SubTitulo"), sectPr);
                }

                body.InsertBefore(CriarParagrafo("Relatório do Catálogo de Recebimentos", "TituloRelatorio"), sectPr);
                body.InsertBefore(CriarParagrafo($"Período: {dados.DataIni:dd/MM/yyyy} a {dados.DataFim:dd/MM/yyyy}  |  Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}", "SubTitulo"), sectPr);
                body.InsertBefore(CriarParagrafoVazio(), sectPr);

                var tabela = CriarTabela();
                tabela.AppendChild(CriarLinhaHeader());

                foreach (var rec in dados.Recebimentos)
                {
                    tabela.AppendChild(CriarLinhaRecebimento(rec));
                }

                tabela.AppendChild(CriarLinhaTotal(dados.ValorTotalGeral));
                body.InsertBefore(tabela, sectPr);
                body.InsertBefore(CriarParagrafoVazio(), sectPr);

                body.InsertBefore(CriarParagrafo("Totais por Forma de Recebimento", "SubTituloNegrito"), sectPr);
                foreach (var total in dados.TotaisPorForma)
                {
                    body.InsertBefore(CriarParagrafo($"{total.Descricao}: {total.Valor:N2}"), sectPr);
                }

                body.InsertBefore(CriarParagrafoVazio(), sectPr);
                body.InsertBefore(CriarParagrafo("Totais por Conta de Recebimento", "SubTituloNegrito"), sectPr);
                foreach (var total in dados.TotaisPorConta)
                {
                    body.InsertBefore(CriarParagrafo($"{total.Descricao}: {total.Valor:N2}"), sectPr);
                }
            }

            return stream.ToArray();
        }

        private static Table CriarTabela()
        {
            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));
            return table;
        }

        private static TableRow CriarLinhaHeader()
        {
            var row = new TableRow();
            string[] headers = { "Id", "Data", "Origem", "Instituição", "Paciente", "Período", "Total" };
            foreach (var h in headers)
            {
                row.AppendChild(CriarCelula(h, true));
            }
            return row;
        }

        private static TableRow CriarLinhaRecebimento(RecebimentoCatalogoDto rec)
        {
            var row = new TableRow();
            row.AppendChild(CriarCelula(rec.CatalogoId.ToString()));
            row.AppendChild(CriarCelula(rec.DataRecebimento.ToString("dd/MM/yyyy")));
            row.AppendChild(CriarCelula(rec.Origem));
            row.AppendChild(CriarCelula($"{rec.SiglaInstituicao} - {rec.NomeInstituicao}"));
            row.AppendChild(CriarCelula(rec.NomePaciente));
            row.AppendChild(CriarCelula(rec.PeriodoFaturamento ?? ""));
            row.AppendChild(CriarCelula(rec.ValorTotal.ToString("N2"), false, true));
            return row;
        }

        private static TableRow CriarLinhaTotal(decimal total)
        {
            var row = new TableRow();
            row.AppendChild(new TableCell(new TableCellProperties(new GridSpan { Val = 6 }), new Paragraph(new Run(new RunProperties(new Bold()), new Text("TOTAL GERAL")))));
            row.AppendChild(CriarCelula(total.ToString("N2"), false, true));
            return row;
        }

        private static TableCell CriarCelula(string texto, bool negrito = false, bool direita = false)
        {
            var runProps = new RunProperties();
            if (negrito) runProps.AppendChild(new Bold());
            var parProps = new ParagraphProperties();
            if (direita) parProps.AppendChild(new Justification { Val = JustificationValues.Right });
            return new TableCell(
                new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                new Paragraph(parProps, new Run(runProps, new Text(texto ?? ""))));
        }

        private static Paragraph CriarParagrafo(string texto, string estilo = "Normal")
        {
            return new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = estilo }),
                new Run(new Text(texto)));
        }

        private static Paragraph CriarParagrafoVazio()
        {
            return new Paragraph();
        }

        private static void AdicionarEstilos(MainDocumentPart mainPart)
        {
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            var styles = new Styles();

            styles.AppendChild(new Style(
                new StyleName { Val = "Titulo" },
                new BasedOn { Val = "Normal" },
                new StyleParagraphProperties(new Justification { Val = JustificationValues.Left }),
                new StyleRunProperties(new Bold(), new FontSize { Val = "28" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" })) { Type = StyleValues.Paragraph, StyleId = "Titulo" });

            styles.AppendChild(new Style(
                new StyleName { Val = "TituloRelatorio" },
                new BasedOn { Val = "Normal" },
                new StyleParagraphProperties(new Justification { Val = JustificationValues.Left }, new SpacingBetweenLines { After = "120" }),
                new StyleRunProperties(new Bold(), new FontSize { Val = "24" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" })) { Type = StyleValues.Paragraph, StyleId = "TituloRelatorio" });

            styles.AppendChild(new Style(
                new StyleName { Val = "SubTitulo" },
                new BasedOn { Val = "Normal" },
                new StyleRunProperties(new FontSize { Val = "20" }, new RunFonts { Ascii = "Arial", HighAnsi = "Arial" })) { Type = StyleValues.Paragraph, StyleId = "SubTitulo" });

            styles.AppendChild(new Style(
                new StyleName { Val = "SubTituloNegrito" },
                new BasedOn { Val = "SubTitulo" },
                new StyleRunProperties(new Bold())) { Type = StyleValues.Paragraph, StyleId = "SubTituloNegrito" });

            stylesPart.Styles = styles;
        }
    }
}
