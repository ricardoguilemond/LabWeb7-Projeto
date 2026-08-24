using LabWebMvc.MVC.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LabWebMvc.MVC.Areas.Utils
{
    //Feito pelo Qoder em 23/08/2026
    // DTO do registro do Mapa Horizontal (uma ficha por exame, com seus itens).
    public class FichaMapaHorizontalDto
    {
        public string NomeFicha { get; set; } = "";
        public int ExamesRealizadosId { get; set; }
        public string? ControleApoio { get; set; }
        public string SiglaInstituicao { get; set; } = "";
        public int Sequencial { get; set; }
        public string NomePaciente { get; set; } = "";
        public DateTime Nascimento { get; set; }
        /// <summary>Descrições dos itens já na ordem de impressão (abreviação ou descrição).</summary>
        public List<string> Descricoes { get; set; } = new();
    }

    //Feito pelo Qoder em 23/08/2026
    // Gerador do PDF do Mapa Horizontal (portabilidade do FRelMapaHorizontal QuickReport):
    // A4 paisagem, seção por folha de exame, linhas de até 14 colunas de itens por exame.
    // Modelo 1 (paginado): cada folha de exame inicia em página nova.
    // Modelo 2 (não paginado): as folhas se sucedem na mesma página.
    public class GeradorPdfMapaHorizontal
    {
        private const double MargemTopo = 30;
        private const double MargemEsquerda = 30;
        private const double LarguraPagina = 842;  // A4 paisagem
        private const double LimiteY = 560;        // A4 paisagem 595pt - rodapé
        private const int ColunasPorLinha = 14;    // const Colunas = 14 do FFichaHorizontal

        private readonly XFont _fontTitulo = new("Arial", 11, XFontStyle.Bold);
        private readonly XFont _fontSecao = new("Arial", 10, XFontStyle.Bold);
        private readonly XFont _fontNormal = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontNormalBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontPequena = new("Arial", 8, XFontStyle.Regular);
        private readonly XFont _fontMono = new("Courier New", 8, XFontStyle.Regular);
        private readonly XPen _penLinha = new(XColors.Gray, 0.6);

        public byte[] Gerar(List<FichaMapaHorizontalDto> fichas, int modelo,
            DateTime dataMapa, Empresa? empresa)
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = "Mapa Horizontal";
            pdfDocument.Info.Author = "LabWeb7";

            PdfPage page = pdfDocument.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            page.Orientation = PdfSharpCore.PageOrientation.Landscape;
            XGraphics gfx = XGraphics.FromPdfPage(page);
            double y = MargemTopo;

            y = DesenharCabecalho(gfx, modelo, dataMapa, empresa, y);

            string folhaAtual = "";
            foreach (var ficha in fichas)
            {
                if (ficha.NomeFicha != folhaAtual)
                {
                    folhaAtual = ficha.NomeFicha;

                    // Modelo paginado: cada folha de exame inicia em página própria.
                    if (modelo == 1 && y > MargemTopo)
                    {
                        DesenharRodape(gfx, pdfDocument.PageCount);
                        page = pdfDocument.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                        gfx = XGraphics.FromPdfPage(page);
                        y = MargemTopo;
                    }

                    y = DesenharSecaoFolha(gfx, folhaAtual, y);
                }

                double alturaNecessaria = CalcularAlturaBloco(ficha.Descricoes.Count);
                if (y + alturaNecessaria > LimiteY)
                {
                    DesenharRodape(gfx, pdfDocument.PageCount);
                    page = pdfDocument.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                    gfx = XGraphics.FromPdfPage(page);
                    y = MargemTopo;
                    y = DesenharSecaoFolha(gfx, folhaAtual, y);
                }

                y = DesenharBlocoExame(gfx, ficha, y);
            }

            DesenharRodape(gfx, pdfDocument.PageCount);

            using var stream = new MemoryStream();
            pdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private double DesenharCabecalho(XGraphics gfx, int modelo, DateTime dataMapa, Empresa? empresa, double y)
        {
            string titulo = empresa?.TituloEmpresa ?? "LABORATÓRIO";
            gfx.DrawString(titulo, _fontTitulo, XBrushes.Black, new XRect(0, y, LarguraPagina, 18), XStringFormats.TopCenter);
            y += 16;

            gfx.DrawString("Sistema Lab-Web7", _fontPequena, XBrushes.Black, new XRect(0, y, LarguraPagina, 12), XStringFormats.TopCenter);
            y += 12;

            string modeloTexto = modelo == 1
                ? "M A P A    H O R I Z O N T A L   (Modelo 1-A - Paginado)"
                : "M A P A    H O R I Z O N T A L   (Modelo 1-B - Não Paginado)";
            gfx.DrawString(modeloTexto, _fontSecao, XBrushes.Black, new XRect(0, y, LarguraPagina, 16), XStringFormats.TopCenter);
            y += 18;

            gfx.DrawString($"MAPA DO DIA: {dataMapa:dd/MM/yyyy}   -   Impresso em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}",
                _fontPequena, XBrushes.Black, new XRect(0, y, LarguraPagina, 12), XStringFormats.TopCenter);
            y += 16;

            return y;
        }

        private double DesenharSecaoFolha(XGraphics gfx, string nomeFolha, double y)
        {
            gfx.DrawLine(_penLinha, MargemEsquerda, y, LarguraPagina - MargemEsquerda, y);
            y += 12;

            gfx.DrawString(nomeFolha, _fontSecao, XBrushes.Black, new XRect(MargemEsquerda, y, LarguraPagina - 2 * MargemEsquerda, 14), XStringFormats.TopLeft);
            y += 14;

            if (nomeFolha.Contains("HEMATOLOGIA"))
            {
                gfx.DrawString("( Caso haja, será informado aqui também a existência de Hemograma ou Eritrograma ou Leucograma. )",
                    _fontPequena, XBrushes.Black, new XRect(MargemEsquerda, y, LarguraPagina - 2 * MargemEsquerda, 12), XStringFormats.TopLeft);
                y += 12;
            }

            gfx.DrawString("Código  Nome Paciente                    Id", _fontNormalBold, XBrushes.Black,
                new XRect(MargemEsquerda, y, LarguraPagina - 2 * MargemEsquerda, 12), XStringFormats.TopLeft);
            y += 13;

            return y;
        }

        private double DesenharBlocoExame(XGraphics gfx, FichaMapaHorizontalDto ficha, double y)
        {
            int idade = CalcularIdade(ficha.Nascimento);

            // Linha 1: código (ou controle/sequencial) + nome do paciente (padrão Ordena_Fichas).
            string codigo = ficha.ExamesRealizadosId.ToString();
            if (!string.IsNullOrEmpty(ficha.ControleApoio) && ficha.ControleApoio.Length >= 12)
                codigo = FormatarControleApoio(ficha.ControleApoio);

            string linha1 = $"{codigo}  {Truncar(ficha.NomePaciente, 31)}";
            gfx.DrawString(linha1, _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, LarguraPagina - 2 * MargemEsquerda, 12), XStringFormats.TopLeft);
            y += 11;

            // Linhas de itens: idade + até 14 descrições separadas por "|" (Texto2 do Delphi).
            for (int inicio = 0; inicio < ficha.Descricoes.Count; inicio += ColunasPorLinha)
            {
                var lote = ficha.Descricoes.Skip(inicio).Take(ColunasPorLinha).ToList();
                string linha2 = $"{idade:D2}  {string.Join("|", lote)}";
                gfx.DrawString(linha2, _fontMono, XBrushes.Black, new XRect(MargemEsquerda, y, LarguraPagina - 2 * MargemEsquerda, 12), XStringFormats.TopLeft);
                y += 10;
            }

            return y + 4;
        }

        private static double CalcularAlturaBloco(int quantidadeItens)
        {
            int linhasItens = Math.Max(1, (int)Math.Ceiling(quantidadeItens / (double)ColunasPorLinha));
            return 11 + linhasItens * 10 + 4;
        }

        private static int CalcularIdade(DateTime nascimento)
        {
            var hoje = DateTime.Today;
            int idade = hoje.Year - nascimento.Year;
            if (nascimento.Date > hoje.AddYears(-idade)) idade--;
            return idade;
        }

        private static string FormatarControleApoio(string texto)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length < 12) return texto;
            return $"{texto.Substring(0, 4)}.{texto.Substring(4, 2)}.{texto.Substring(6, 2)}-{texto.Substring(8, 4)}";
        }

        private static string Truncar(string? texto, int tamanho)
        {
            texto = (texto ?? "").Trim();
            return texto.Length <= tamanho ? texto : texto.Substring(0, tamanho);
        }

        private void DesenharRodape(XGraphics gfx, int numeroPagina)
        {
            gfx.DrawString($"Página {numeroPagina}", _fontPequena, XBrushes.Gray,
                new XRect(0, 575, LarguraPagina, 14), XStringFormats.TopCenter);
        }
    }
    //..Qoder
}
