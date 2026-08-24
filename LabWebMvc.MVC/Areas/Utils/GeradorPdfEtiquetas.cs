using LabWebMvc.MVC.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LabWebMvc.MVC.Areas.Utils
{
    //Feito pelo Qoder em 23/08/2026
    // DTO de uma etiqueta de exame hematológico/citológico (padrão FEtiquetasHemograma).
    public class EtiquetaExameDto
    {
        public int ExamesRealizadosId { get; set; }
        public int PacienteId { get; set; }
        public string NomePaciente { get; set; } = "";
        public DateTime Nascimento { get; set; }
        public string? Sexo { get; set; }
        public DateTime DataIni { get; set; }
        public string SiglaInstituicao { get; set; } = "";
        public int Sequencial { get; set; }
        public string NomeMedico { get; set; } = "";
        public string? HistoricoClinico { get; set; }
        public bool TemVhs { get; set; }
        public bool TemFatorRh { get; set; }
        public bool TemGrupoSanguineo { get; set; }
    }

    //Feito pelo Qoder em 23/08/2026
    // Gerador do PDF de Etiquetas (portabilidade do FEtiquetasHemograma QuickReport):
    // 3 etiquetas por linha com sequencial, data/nº exame, sexo/idade, nome, médico,
    // histórico e marcadores VHS / Fator RH / Grupo Sanguíneo.
    public class GeradorPdfEtiquetas
    {
        private const double MargemLateral = 20;
        private const double MargemTopo = 24;
        private const double LarguraEtiqueta = 185;   // ~6,5 cm
        private const double AlturaEtiqueta = 64;     // ~2,2 cm
        private const double EspacamentoHorizontal = 6;
        private const double EspacamentoVertical = 8;
        private const int EtiquetasPorLinha = 3;

        private readonly XFont _fontNormal = new("Arial", 7, XFontStyle.Regular);
        private readonly XFont _fontBold = new("Arial", 7, XFontStyle.Bold);
        private readonly XFont _fontNome = new("Arial", 8, XFontStyle.Bold);
        private readonly XPen _penEtiqueta = new(XColors.Black, 0.7);

        public byte[] Gerar(List<EtiquetaExameDto> etiquetas, DateTime dataMapa, Empresa? empresa)
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = "Etiquetas de Exames";
            pdfDocument.Info.Author = "LabWeb7";

            PdfPage page = pdfDocument.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            XGraphics gfx = XGraphics.FromPdfPage(page);

            gfx.DrawString($"{empresa?.TituloEmpresa ?? "LABORATÓRIO"} — ETIQUETAS DOS EXAMES — DIA: {dataMapa:dd/MM/yyyy}",
                _fontBold, XBrushes.Black, new XRect(0, 8, 595, 12), XStringFormats.TopCenter);

            int coluna = 0;
            double y = MargemTopo;

            foreach (var etiqueta in etiquetas)
            {
                if (y + AlturaEtiqueta > 820)
                {
                    DesenharRodape(gfx, pdfDocument.PageCount);
                    page = pdfDocument.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    y = MargemTopo;
                    coluna = 0;
                }

                double x = MargemLateral + coluna * (LarguraEtiqueta + EspacamentoHorizontal);
                DesenharEtiqueta(gfx, etiqueta, x, y);

                coluna++;
                if (coluna == EtiquetasPorLinha)
                {
                    coluna = 0;
                    y += AlturaEtiqueta + EspacamentoVertical;
                }
            }

            DesenharRodape(gfx, pdfDocument.PageCount);

            using var stream = new MemoryStream();
            pdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private void DesenharEtiqueta(XGraphics gfx, EtiquetaExameDto etiqueta, double x, double y)
        {
            gfx.DrawRectangle(_penEtiqueta, x, y, LarguraEtiqueta, AlturaEtiqueta);

            double yi = y + 10;
            double xTexto = x + 5;
            double largura = LarguraEtiqueta - 10;

            // Linha 1: instituição - sequencial (XXX.YYY) + data e nº do exame.
            string seq = $"{etiqueta.SiglaInstituicao.Trim()} - {etiqueta.Sequencial:000\\,000}";
            gfx.DrawString(seq, _fontBold, XBrushes.Black, new XRect(xTexto, yi, 80, 9), XStringFormats.TopLeft);
            gfx.DrawString($"{etiqueta.DataIni:dd/MM/yyyy}, Nº Exame: {etiqueta.ExamesRealizadosId}",
                _fontNormal, XBrushes.Black, new XRect(xTexto + 78, yi, largura - 78, 9), XStringFormats.TopLeft);
            yi += 10;

            // Linha 2: sexo + idade (cálculo simplificado por ano, como no Delphi).
            int idade = etiqueta.Nascimento.Year > 0 ? DateTime.Today.Year - etiqueta.Nascimento.Year : 0;
            gfx.DrawString($"Sx:{etiqueta.Sexo}   Id:{idade}", _fontNormal, XBrushes.Black,
                new XRect(xTexto, yi, largura, 9), XStringFormats.TopLeft);
            yi += 10;

            // Linha 3: nome do paciente.
            gfx.DrawString(Truncar(etiqueta.NomePaciente, 38), _fontNome, XBrushes.Black,
                new XRect(xTexto, yi, largura, 10), XStringFormats.TopLeft);
            yi += 11;

            // Linha 4: médico.
            gfx.DrawString("Dr.(a) " + Truncar(etiqueta.NomeMedico, 34), _fontNormal, XBrushes.Black,
                new XRect(xTexto, yi, largura, 9), XStringFormats.TopLeft);
            yi += 10;

            // Linha 5: histórico clínico (44 caracteres no Delphi).
            gfx.DrawString(Truncar(etiqueta.HistoricoClinico, 44), _fontNormal, XBrushes.Black,
                new XRect(xTexto, yi, largura, 9), XStringFormats.TopLeft);
            yi += 10;

            // Linha 6: marcadores VHS / Fator RH / Grupo Sanguíneo.
            string marcadores = "";
            if (etiqueta.TemVhs) marcadores += "VHS       ";
            if (etiqueta.TemFatorRh) marcadores += "Fator RH       ";
            if (etiqueta.TemGrupoSanguineo) marcadores += "Grupo Sanguíneo";
            if (marcadores.Length > 0)
                gfx.DrawString(marcadores, _fontBold, XBrushes.Black, new XRect(xTexto, yi, largura, 9), XStringFormats.TopLeft);
        }

        private static string Truncar(string? texto, int tamanho)
        {
            texto = (texto ?? "").Trim();
            return texto.Length <= tamanho ? texto : texto.Substring(0, tamanho);
        }

        private void DesenharRodape(XGraphics gfx, int numeroPagina)
        {
            gfx.DrawString($"Página {numeroPagina}", _fontNormal, XBrushes.Gray,
                new XRect(0, 826, 595, 12), XStringFormats.TopCenter);
        }
    }
    //..Qoder
}
