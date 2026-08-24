using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.Utils
{
    //Feito pelo Qoder em 23/08/2026
    // Gerador da planilha XLSX do Mapa Planilhado (portabilidade do FMapaExcel.Exportar_Excel).
    // Layout: linha 1 com o laboratório e a data de geração, linha 2 com o título do lote,
    // linha 3 com os títulos das colunas (MapaHorizontal ou Descricao) e, abaixo, uma linha
    // por coleta (ControleApoio) com a célula "NNNN=        " para cada item selecionado.
    public class GeradorXlsxMapaPlanilhado
    {
        public byte[] Gerar(List<FichasPlanilhas> fichas, List<string> descricoes, string tituloEmpresa, DateTime dataMapa, int lote)
        {
            using var stream = new MemoryStream();
            using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                var workbookPart = doc.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = CriarStylesheet();
                stylesPart.Stylesheet.Save();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                //Feito pelo Qoder em 23/08/2026 — NÃO salvar a worksheet antes de populá-la:
                // o Save() prematuro faz o SDK recarregar o DOM da parte a partir do stream,
                // orfando o sheetData em memória (o XLSX saía só com o esqueleto vazio).
                // A persistência ocorre uma única vez, no final (Save abaixo).

                // Títulos das colunas: abreviação MapaHorizontal quando existir, senão a Descrição.
                var titulos = fichas
                    .Where(f => descricoes.Contains(f.Descricao ?? ""))
                    .GroupBy(f => f.Descricao ?? "")
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Descricao = g.Key,
                        Titulo = string.IsNullOrWhiteSpace(g.First().MapaHorizontal) ? g.Key : g.First().MapaHorizontal!
                    })
                    .ToList();

                int totalColunas = titulos.Count;
                string ultimaColuna = ReferenciaColuna(totalColunas);

                //Feito pelo Qoder em 23/08/2026 — refinamento de layout (pedido do usuário):
                // cada item de exame ocupa UMA única coluna, com largura consistente com a
                // descrição; os textos longos (linhas 1 e 2) são mesclados em várias colunas
                // para não alargar a coluna A (mantendo os títulos dos itens próximos).
                var larguras = new List<double>();
                for (int c = 0; c < totalColunas; c++)
                    larguras.Add(Math.Max((titulos[c].Titulo ?? "").Length, 13) + 2);

                var colunas = new Columns();
                for (int c = 0; c < totalColunas; c++)
                    colunas.Append(new Column { Min = (uint)(c + 1), Max = (uint)(c + 1), Width = larguras[c], CustomWidth = true });

                worksheetPart.Worksheet = totalColunas > 0 ? new Worksheet(colunas, sheetData) : new Worksheet(sheetData);

                var mergeCells = new MergeCells();

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = $"Lote {lote}"
                };
                sheets.Append(sheet);

                // Linha 1: empresa + data de geração, mesclada em todas as colunas.
                sheetData.AppendChild(CriarLinha(1, new[]
                {
                    CriarCelula("A1", $"{tituloEmpresa}, Hoje: {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}", estiloNormal: true)
                }));
                if (totalColunas > 1)
                    mergeCells.Append(new MergeCell { Reference = $"A1:{ultimaColuna}1" });

                // Linha 2: título do lote em A2 (mesclado até caber) e, na sequência,
                // "Por ordem de Código de Exame" mesclado nas colunas seguintes.
                string tituloLote = $"Mapa da Data de Coleta: {dataMapa:dd/MM/yyyy} - Lote Nº {lote}";
                double acumulado = 0;
                int fimLote = 1;
                for (int c = 0; c < totalColunas; c++)
                {
                    acumulado += larguras[c];
                    fimLote = c + 1;
                    if (acumulado >= tituloLote.Length) break;
                }

                const string ordemCodigo = "Por ordem de Código de Exame";
                int inicioOrdem = fimLote + 1;
                int fimOrdem = 0;
                if (inicioOrdem <= totalColunas)
                {
                    acumulado = 0;
                    for (int c = inicioOrdem - 1; c < totalColunas; c++)
                    {
                        acumulado += larguras[c];
                        fimOrdem = c + 1;
                        if (acumulado >= ordemCodigo.Length) break;
                    }
                }

                var celulasLinha2 = new List<Cell>
                {
                    CriarCelula("A2", tituloLote, estiloNormal: false, negrito: true)
                };
                if (fimOrdem > 0)
                    celulasLinha2.Add(CriarCelula(ReferenciaColuna(inicioOrdem) + "2", ordemCodigo, estiloNormal: true));
                sheetData.AppendChild(CriarLinha(2, celulasLinha2));

                if (fimLote > 1)
                    mergeCells.Append(new MergeCell { Reference = $"A2:{ReferenciaColuna(fimLote)}2" });
                if (fimOrdem > inicioOrdem)
                    mergeCells.Append(new MergeCell { Reference = $"{ReferenciaColuna(inicioOrdem)}2:{ReferenciaColuna(fimOrdem)}2" });

                // Linha 3: títulos dos itens (uma coluna por item).
                uint linhaAtual = 3;
                var celulasTitulo = new List<Cell>();
                for (int c = 0; c < titulos.Count; c++)
                    celulasTitulo.Add(CriarCelula(ReferenciaColuna(c + 1) + linhaAtual, titulos[c].Titulo, estiloNormal: false, negrito: true));
                sheetData.AppendChild(CriarLinha(linhaAtual, celulasTitulo));

                // Uma linha por coleta: células "NNNN=        " nas colunas dos itens do paciente.
                var porControle = fichas
                    .Where(f => descricoes.Contains(f.Descricao ?? ""))
                    .GroupBy(f => f.ControleApoio ?? "")
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var grupo in porControle)
                {
                    linhaAtual++;
                    var celulas = new List<Cell>();
                    for (int c = 0; c < titulos.Count; c++)
                    {
                        var ficha = grupo.FirstOrDefault(f => (f.Descricao ?? "") == titulos[c].Descricao);
                        if (ficha == null) continue;

                        string controle = ficha.ControleApoio ?? "";
                        string sequencia = controle.Length >= 4 ? controle.Substring(controle.Length - 4) : controle;
                        celulas.Add(CriarCelula(ReferenciaColuna(c + 1) + linhaAtual, $"{sequencia}={new string(' ', 8)}", estiloNormal: true));
                    }
                    sheetData.AppendChild(CriarLinha(linhaAtual, celulas));
                }

                if (mergeCells.Any())
                    worksheetPart.Worksheet.Append(mergeCells);

                worksheetPart.Worksheet.Save();
                workbookPart.Workbook.Save();
            }
            return stream.ToArray();
        }

        private static Row CriarLinha(uint indice, IEnumerable<Cell> celulas)
        {
            var row = new Row { RowIndex = indice };
            foreach (var celula in celulas)
                row.AppendChild(celula);
            return row;
        }

        private static Cell CriarCelula(string referencia, string texto, bool estiloNormal, bool negrito = false)
        {
            return new Cell
            {
                CellReference = referencia,
                DataType = CellValues.InlineString,
                StyleIndex = (uint)(negrito ? 2 : estiloNormal ? 1 : 0),
                InlineString = new InlineString(new Text(texto))
            };
        }

        /// <summary>
        /// Converte o índice de coluna (1 = A) na referência literal (A, B, ..., AA...).
        /// </summary>
        private static string ReferenciaColuna(int indice)
        {
            string referencia = "";
            while (indice > 0)
            {
                int resto = (indice - 1) % 26;
                referencia = (char)('A' + resto) + referencia;
                indice = (indice - 1) / 26;
            }
            return referencia;
        }

        /// <summary>
        /// Estilos mínimos: 0 = padrão, 1 = Arial 8 com borda fina, 2 = Arial 8 negrito com borda fina.
        /// </summary>
        private static Stylesheet CriarStylesheet()
        {
            var fonts = new Fonts(
                new Font(new FontSize { Val = 10 }, new FontName { Val = "Arial" }),
                new Font(new FontSize { Val = 8 }, new FontName { Val = "Arial" }),
                new Font(new Bold(), new FontSize { Val = 8 }, new FontName { Val = "Arial" }));

            var bordaFina = new Border(
                new LeftBorder { Style = BorderStyleValues.Thin },
                new RightBorder { Style = BorderStyleValues.Thin },
                new TopBorder { Style = BorderStyleValues.Thin },
                new BottomBorder { Style = BorderStyleValues.Thin },
                new DiagonalBorder());

            var fills = new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }));

            var borders = new Borders(new Border(), bordaFina);

            var cellFormats = new CellFormats(
                new CellFormat { FontId = 0, BorderId = 0, FillId = 0 },
                new CellFormat { FontId = 1, BorderId = 1, FillId = 0, ApplyFont = true, ApplyBorder = true },
                new CellFormat { FontId = 2, BorderId = 1, FillId = 0, ApplyFont = true, ApplyBorder = true });

            return new Stylesheet(fonts, fills, borders, cellFormats);
        }
    }
    //..Qoder
}
