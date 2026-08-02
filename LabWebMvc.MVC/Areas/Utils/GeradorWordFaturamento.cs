using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.Utils
{
    public class GeradorWordFaturamento
    {
        // Larguras das colunas da tabela em twips (1440 twips = 1 polegada)
        private const int ColSeq    = 800;
        private const int ColTabela = 1200;
        private const int ColData   = 1200;
        private const int ColPaciente = 3000;
        private const int ColItens  = 4000;
        private const int ColTotal  = 1200;

        public byte[] Gerar(DadosPdfFaturamento dados, Empresa? empresa)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                var body = mainPart.Document.Body!;

                // Estilos
                AdicionarEstilos(mainPart);

                // Configuração de página (A4 paisagem para caber as colunas)
                var sectPr = new SectionProperties(
                    new PageSize
                    {
                        Width  = 16838,  // A4 largura em twips (paisagem)
                        Height = 11906,
                        Orient = PageOrientationValues.Landscape
                    },
                    new PageMargin
                    {
                        Top    = 720,
                        Bottom = 720,
                        Left   = 720,
                        Right  = 720
                    });
                body.AppendChild(sectPr);

                // Cabeçalho da empresa
                if (empresa != null)
                {
                    if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial))
                        body.InsertBefore(CriarParagrafo(empresa.RazaoSocial, "Titulo"), sectPr);

                    if (!string.IsNullOrWhiteSpace(empresa.Endereco))
                        body.InsertBefore(CriarParagrafo(empresa.Endereco, "SubTitulo"), sectPr);

                    if (!string.IsNullOrWhiteSpace(empresa.CNPJ))
                        body.InsertBefore(CriarParagrafo($"CNPJ: {empresa.CNPJ}", "SubTitulo"), sectPr);
                }

                body.InsertBefore(CriarParagrafo("Relatório de Faturamento por Período", "TituloRelatorio"), sectPr);
                body.InsertBefore(CriarParagrafo($"Período: {dados.DataIni:dd/MM/yyyy} a {dados.DataFim:dd/MM/yyyy}  |  Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}", "SubTitulo"), sectPr);

                if (dados.TabelasUtilizadas.Count > 0)
                    body.InsertBefore(CriarParagrafo($"Tabelas de preços: {string.Join(" | ", dados.TabelasUtilizadas)}", "SubTitulo"), sectPr);

                body.InsertBefore(CriarParagrafoVazio(), sectPr);

                // Agrupa por instituição
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

                    body.InsertBefore(CriarParagrafo(tituloInst, "CabecalhoInstituicao"), sectPr);

                    var tabela = CriarTabela();

                    // Cabeçalho da tabela
                    tabela.AppendChild(CriarLinhaHeader());

                    // Linhas de exames
                    foreach (var exame in grupo)
                    {
                        string itensTexto = exame.Itens.Count > 0
                            ? string.Join(", ", exame.Itens.Select(i =>
                                dados.MostragemPrecos != 2
                                    ? $"{i.Descricao} ({i.ValorItem:C2})"
                                    : i.Descricao))
                            : "—";

                        tabela.AppendChild(CriarLinhaExame(
                            exame.Sequencial.ToString(),
                            exame.SiglaTabela,
                            exame.DataExame?.ToString("dd/MM/yyyy") ?? "—",
                            exame.NomePaciente,
                            itensTexto,
                            exame.ValorTotal.ToString("C2"),
                            false));
                    }

                    // Total por instituição
                    tabela.AppendChild(CriarLinhaTotalInst(
                        $"Total {grupo.Key.SiglaInstituicao}:",
                        totalInst.ToString("C2")));

                    body.InsertBefore(tabela, sectPr);
                    body.InsertBefore(CriarParagrafoVazio(), sectPr);
                }

                // Total geral
                body.InsertBefore(CriarParagrafo($"TOTAL GERAL: {totalGeral:C2}", "TotalGeral"), sectPr);

                // Quantitativo de itens de exames realizados
                if (dados.QuantitativoItens.Count > 0)
                {
                    body.InsertBefore(CriarParagrafo("QUANTITATIVO DE ITENS DE EXAMES REALIZADOS:", "TituloRelatorio"), sectPr);

                    body.InsertBefore(CriarParagrafoQuantitativo(
                        FormatarLinhaPontilhada("Folha de Exame, Item", "Quantidade")), sectPr);

                    foreach (var item in dados.QuantitativoItens)
                    {
                        body.InsertBefore(CriarParagrafoQuantitativo(
                            FormatarLinhaPontilhada(item.DescricaoCompleta, item.Quantidade.ToString("N0"))), sectPr);
                    }
                }

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        // -----------------------------------------------------------------------
        // Helpers de parágrafos
        // -----------------------------------------------------------------------

        private static Paragraph CriarParagrafo(string texto, string estilo)
        {
            return new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = estilo }),
                new Run(new Text(texto) { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static Paragraph CriarParagrafoVazio() =>
            new Paragraph(new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "80" }));

        // -----------------------------------------------------------------------
        // Helpers de tabela
        // -----------------------------------------------------------------------

        private static Table CriarTabela()
        {
            return new Table(
                new TableProperties(
                    new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
                    new TableBorders(
                        new TopBorder    { Val = BorderValues.Single, Size = 4 },
                        new BottomBorder { Val = BorderValues.Single, Size = 4 },
                        new LeftBorder   { Val = BorderValues.Single, Size = 4 },
                        new RightBorder  { Val = BorderValues.Single, Size = 4 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4 })));
        }

        private static TableRow CriarLinhaHeader()
        {
            return new TableRow(
                CriarCelula("Seq.",     ColSeq,      negrito: true, sombreado: true),
                CriarCelula("Tabela",   ColTabela,   negrito: true, sombreado: true),
                CriarCelula("Data",     ColData,     negrito: true, sombreado: true),
                CriarCelula("Paciente", ColPaciente, negrito: true, sombreado: true),
                CriarCelula("Itens",    ColItens,    negrito: true, sombreado: true),
                CriarCelula("Total",    ColTotal,    negrito: true, sombreado: true, alinharDireita: true));
        }

        private static TableRow CriarLinhaExame(
            string seq, string tabela, string data, string paciente, string itens, string total, bool sombreado)
        {
            return new TableRow(
                CriarCelula(seq,      ColSeq,      sombreado: sombreado),
                CriarCelula(tabela,   ColTabela,   sombreado: sombreado),
                CriarCelula(data,     ColData,     sombreado: sombreado),
                CriarCelula(paciente, ColPaciente, sombreado: sombreado),
                CriarCelula(itens,    ColItens,    sombreado: sombreado, tamanhoFonte: "16"),
                CriarCelula(total,    ColTotal,    sombreado: sombreado, alinharDireita: true));
        }

        private static TableRow CriarLinhaTotalInst(string label, string valor)
        {
            return new TableRow(
                CriarCelulaSpan(label, ColSeq + ColTabela + ColData + ColPaciente + ColItens, negrito: true, sombreado: true, alinharDireita: true),
                CriarCelula(valor, ColTotal, negrito: true, sombreado: true, alinharDireita: true));
        }

        private static Paragraph CriarParagrafoQuantitativo(string texto)
        {
            var runProps = new RunProperties(
                new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" },
                new Bold(),
                new Color { Val = "333333" },
                new FontSize { Val = "20" });

            var paraProps = new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0" });

            return new Paragraph(paraProps, new Run(runProps, new Text(texto) { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static string FormatarLinhaPontilhada(string descricao, string quantidade, int totalCaracteres = 120)
        {
            int pontos = totalCaracteres - (descricao.Length + quantidade.Length);
            if (pontos < 1) pontos = 1;
            return descricao + new string('.', pontos) + quantidade;
        }

        private static TableCell CriarCelula(
            string texto, int largura,
            bool negrito = false, bool sombreado = false,
            bool alinharDireita = false, string tamanhoFonte = "18")
        {
            var runProps = new RunProperties();
            if (negrito) runProps.AppendChild(new Bold());
            runProps.AppendChild(new FontSize { Val = tamanhoFonte });

            var paraProps = new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0" });
            if (alinharDireita)
                paraProps.AppendChild(new Justification { Val = JustificationValues.Right });

            var cellProps = new TableCellProperties(
                new TableCellWidth { Width = largura.ToString(), Type = TableWidthUnitValues.Dxa });
            if (sombreado)
                cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "EEEEEE" });

            return new TableCell(
                cellProps,
                new Paragraph(paraProps, new Run(runProps, new Text(texto ?? "") { Space = SpaceProcessingModeValues.Preserve })));
        }

        private static TableCell CriarCelulaSpan(
            string texto, int largura,
            bool negrito = false, bool sombreado = false, bool alinharDireita = false)
        {
            var runProps = new RunProperties();
            if (negrito) runProps.AppendChild(new Bold());
            runProps.AppendChild(new FontSize { Val = "18" });

            var paraProps = new ParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "0" });
            if (alinharDireita)
                paraProps.AppendChild(new Justification { Val = JustificationValues.Right });

            var cellProps = new TableCellProperties(
                new TableCellWidth { Width = largura.ToString(), Type = TableWidthUnitValues.Dxa });
            if (sombreado)
                cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "D9EAD3" });

            return new TableCell(
                cellProps,
                new Paragraph(paraProps, new Run(runProps, new Text(texto ?? "") { Space = SpaceProcessingModeValues.Preserve })));
        }

        // -----------------------------------------------------------------------
        // Estilos
        // -----------------------------------------------------------------------

        private static void AdicionarEstilos(MainDocumentPart mainPart)
        {
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles(
                CriarEstilo("Titulo",              "Arial", "28", bold: true),
                CriarEstilo("TituloRelatorio",     "Arial", "24", bold: true),
                CriarEstilo("SubTitulo",           "Arial", "20", bold: false),
                CriarEstilo("CabecalhoInstituicao","Arial", "22", bold: true,  sombreado: "D9EAD3"),
                CriarEstilo("TotalGeral",          "Arial", "24", bold: true,  sombreado: "C6EFCE"));
        }

        private static Style CriarEstilo(
            string styleId, string fonte, string tamanho,
            bool bold = false, string? sombreado = null)
        {
            var rpr = new StyleRunProperties();
            rpr.AppendChild(new RunFonts { Ascii = fonte, HighAnsi = fonte });
            rpr.AppendChild(new FontSize { Val = tamanho });
            if (bold) rpr.AppendChild(new Bold());

            var ppr = new StyleParagraphProperties(
                new SpacingBetweenLines { Before = "0", After = "80" });
            if (sombreado != null)
                ppr.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = sombreado });

            return new Style
            {
                Type    = StyleValues.Paragraph,
                StyleId = styleId,
                StyleName  = new StyleName  { Val = styleId },
                StyleRunProperties      = rpr,
                StyleParagraphProperties = ppr
            };
        }
    }
}
