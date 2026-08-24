using LabWebMvc.MVC.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LabWebMvc.MVC.Areas.Utils
{
    //Feito pelo Qoder em 23/08/2026
    // DTO de uma ficha (metade de folha) do Mapa Meia-Folha: um exame + seus itens.
    public class FichaMeiaFolhaDto
    {
        public int Pagina { get; set; }
        public int ExamesRealizadosId { get; set; }
        public int PacienteId { get; set; }
        public string NomePaciente { get; set; } = "";
        public DateTime Nascimento { get; set; }
        public DateTime DataIni { get; set; }
        public string? ControleApoio { get; set; }
        public string SiglaInstituicao { get; set; } = "";
        public int Sequencial { get; set; }
        public string NomeMedico { get; set; } = "";
        public string? HistoricoClinico { get; set; }
        /// <summary>Folhas de exame (NomeFicha) presentes na página/exame — banda "Exame".</summary>
        public List<string> Folhas { get; set; } = new();
        /// <summary>Linhas da grade: contas principais viram título, itens com resultado opcional.</summary>
        public List<LinhaGradeMeiaFolha> Linhas { get; set; } = new();
    }

    public class LinhaGradeMeiaFolha
    {
        public bool ContaPrincipal { get; set; }
        public string Descricao { get; set; } = "";
        public string? Resultado { get; set; }
    }

    //Feito pelo Qoder em 23/08/2026
    // Gerador do PDF do Mapa Meia-Folha (portabilidade do FRelMapaMeiaFolha QuickReport):
    // A4 retrato com duas fichas por página (cada "Página" do Delphi = meia folha),
    // cabeçalho do paciente + grade de até 48 linhas (3 colunas, 1 coluna p/ FEZES).
    public class GeradorPdfMapaMeiaFolha
    {
        private const double MargemTopo = 28;
        private const double MargemEsquerda = 30;
        private const double AreaUtil = 535;      // A4 595pt - 2*30
        private const int LinhasPorGrade = 48;
        private const int ColunasGrade = 3;
        // Duas fichas por página A4 (meia folha cada).
        private const double AlturaMeiaFolha = 398;
        private const double AlturaCabecalhoFicha = 78;

        private readonly XFont _fontTitulo = new("Arial", 10, XFontStyle.Bold);
        private readonly XFont _fontNormal = new("Arial", 8, XFontStyle.Regular);
        private readonly XFont _fontNormalBold = new("Arial", 8, XFontStyle.Bold);
        private readonly XFont _fontGrade = new("Courier New", 7.5, XFontStyle.Regular);
        private readonly XFont _fontGradeTitulo = new("Courier New", 7.5, XFontStyle.Bold);
        private readonly XPen _penCaixa = new(XColors.Black, 0.8);
        private readonly XPen _penLinha = new(XColors.Gray, 0.5);

        public byte[] Gerar(List<FichaMeiaFolhaDto> fichas, DateTime dataMapa, Empresa? empresa)
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = "Mapa Meia-Folha";
            pdfDocument.Info.Author = "LabWeb7";

            PdfPage page = pdfDocument.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            XGraphics gfx = XGraphics.FromPdfPage(page);

            int posicao = 0; // 0 = metade superior, 1 = metade inferior
            double yTopo = MargemTopo;

            for (int indice = 0; indice < fichas.Count; indice++)
            {
                var ficha = fichas[indice];

                if (posicao == 0)
                {
                    yTopo = MargemTopo;
                    DesenharCabecalhoPagina(gfx, dataMapa, empresa);
                    yTopo += 16;
                }

                DesenharFicha(gfx, ficha, yTopo);

                posicao++;
                if (posicao == 2)
                {
                    posicao = 0;
                    DesenharRodape(gfx, pdfDocument.PageCount);

                    // Só abre nova página se ainda houver fichas (evita página final em branco).
                    if (indice + 1 < fichas.Count)
                    {
                        page = pdfDocument.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);
                    }
                }
                else
                {
                    yTopo = MargemTopo + 16 + AlturaMeiaFolha + 6;
                }
            }

            if (fichas.Count == 0)
                DesenharRodape(gfx, pdfDocument.PageCount);
            else if (posicao > 0)
                DesenharRodape(gfx, pdfDocument.PageCount);

            using var stream = new MemoryStream();
            pdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private void DesenharCabecalhoPagina(XGraphics gfx, DateTime dataMapa, Empresa? empresa)
        {
            string titulo = $"{empresa?.TituloEmpresa ?? "LABORATÓRIO"} — MAPAS MEIA-FOLHA A4 — DIA: {dataMapa:dd/MM/yyyy}";
            gfx.DrawString(titulo, _fontTitulo, XBrushes.Black, new XRect(0, MargemTopo - 14, 595, 14), XStringFormats.TopCenter);
        }

        private void DesenharFicha(XGraphics gfx, FichaMeiaFolhaDto ficha, double yTopo)
        {
            double alturaGrade = AlturaMeiaFolha - AlturaCabecalhoFicha;
            // Caixa da meia folha.
            gfx.DrawRectangle(_penCaixa, MargemEsquerda, yTopo, AreaUtil, AlturaMeiaFolha);

            double y = yTopo + 10;

            // Linha 1: data + código do exame + instituição-sequencial.
            string instSeq = $"{ficha.SiglaInstituicao}-{ficha.Sequencial:000\\,000}";
            gfx.DrawString($"Data: {ficha.DataIni:dd/MM/yyyy}", _fontNormalBold, XBrushes.Black,
                new XRect(MargemEsquerda + 6, y, 110, 11), XStringFormats.TopLeft);
            gfx.DrawString($"Nº Exame: {ficha.ExamesRealizadosId}", _fontNormalBold, XBrushes.Black,
                new XRect(MargemEsquerda + 118, y, 120, 11), XStringFormats.TopLeft);
            gfx.DrawString(instSeq, _fontNormalBold, XBrushes.Black,
                new XRect(MargemEsquerda + 240, y, 130, 11), XStringFormats.TopLeft);
            gfx.DrawString($"Coleta: {FormatarControleApoio(ficha.ControleApoio)}", _fontNormalBold, XBrushes.Black,
                new XRect(MargemEsquerda + 372, y, AreaUtil - 378, 11), XStringFormats.TopLeft);
            y += 12;

            // Linha 2: paciente + nascimento + idade.
            int idade = CalcularIdade(ficha.Nascimento);
            string linhaCliente = $"({ficha.PacienteId}) {ficha.NomePaciente.Trim()}, Nasc.: {ficha.Nascimento:dd/MM/yyyy} ( {idade} anos )";
            gfx.DrawString(Truncar(linhaCliente, 110), _fontNormal, XBrushes.Black,
                new XRect(MargemEsquerda + 6, y, AreaUtil - 12, 11), XStringFormats.TopLeft);
            y += 12;

            // Linha 3: médico + folhas do exame.
            string folhas = ficha.Folhas.Count > 0 ? string.Join(" + ", ficha.Folhas.Distinct()) : "";
            gfx.DrawString(Truncar($"Dr.(a) {ficha.NomeMedico.Trim()}", 50), _fontNormal, XBrushes.Black,
                new XRect(MargemEsquerda + 6, y, 250, 11), XStringFormats.TopLeft);
            gfx.DrawString(Truncar(folhas, 70), _fontNormalBold, XBrushes.Black,
                new XRect(MargemEsquerda + 258, y, AreaUtil - 264, 11), XStringFormats.TopLeft);
            y += 12;

            // Linha 4: histórico clínico (até 100 caracteres no Delphi).
            string historico = (ficha.HistoricoClinico ?? "").Trim();
            gfx.DrawString("Histórico: " + Truncar(historico, 96), _fontNormal, XBrushes.Black,
                new XRect(MargemEsquerda + 6, y, AreaUtil - 12, 11), XStringFormats.TopLeft);
            y += 13;

            gfx.DrawLine(_penLinha, MargemEsquerda + 4, y, MargemEsquerda + AreaUtil - 4, y);
            y += 4;

            ImprimirGrade(gfx, ficha, y, alturaGrade - 18);
        }

        /// <summary>
        /// Grade de 48 linhas: 3 colunas para exames gerais, 1 coluna para FEZES/COPROCULTURA
        /// (padrão Imprime_Grade do FRelMapaMeiaFolha).
        /// </summary>
        private void ImprimirGrade(XGraphics gfx, FichaMeiaFolhaDto ficha, double y, double altura)
        {
            string tipoExame = ficha.Folhas.Count > 0 ? ficha.Folhas[0] : "";
            bool umaColuna = tipoExame.Contains("FEZES") || tipoExame.Contains("COPROCULTURA");
            int colunas = umaColuna ? 1 : ColunasGrade;

            // Completa até 48 linhas (padrão do Delphi).
            var linhas = ficha.Linhas.Take(LinhasPorGrade).ToList();

            double alturaLinha = altura / LinhasPorGrade;
            double larguraColuna = (AreaUtil - 12) / colunas;
            int linhasPorColuna = LinhasPorGrade / colunas;

            for (int i = 0; i < linhas.Count; i++)
            {
                int coluna = i / linhasPorColuna;
                int linhaNaColuna = i % linhasPorColuna;
                if (coluna >= colunas) break;

                double x = MargemEsquerda + 6 + coluna * larguraColuna;
                double yy = y + linhaNaColuna * alturaLinha;

                var linha = linhas[i];
                string texto;
                XFont fonte;
                if (linha.ContaPrincipal)
                {
                    texto = " ♦ " + Truncar(linha.Descricao, 35);
                    fonte = _fontGradeTitulo;
                }
                else
                {
                    string resultado = (linha.Resultado ?? "").Trim();
                    texto = resultado.Length > 0
                        ? Truncar(linha.Descricao, 26) + resultado.PadLeft(12)
                        : Truncar(linha.Descricao, 32);
                    fonte = _fontGrade;
                }

                gfx.DrawString(texto, fonte, XBrushes.Black, new XRect(x, yy, larguraColuna - 2, alturaLinha), XStringFormats.TopLeft);
            }
        }

        private static int CalcularIdade(DateTime nascimento)
        {
            var hoje = DateTime.Today;
            int idade = hoje.Year - nascimento.Year;
            if (nascimento.Date > hoje.AddYears(-idade)) idade--;
            return idade;
        }

        private static string FormatarControleApoio(string? texto)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length < 12) return texto ?? "";
            return $"{texto.Substring(0, 4)}.{texto.Substring(4, 2)}.{texto.Substring(6, 2)}-{texto.Substring(8, 4)}";
        }

        private static string Truncar(string? texto, int tamanho)
        {
            texto = (texto ?? "").Trim();
            return texto.Length <= tamanho ? texto : texto.Substring(0, tamanho);
        }

        private void DesenharRodape(XGraphics gfx, int numeroPagina)
        {
            gfx.DrawString($"Página {numeroPagina}", _fontNormal, XBrushes.Gray,
                new XRect(0, 824, 595, 12), XStringFormats.TopCenter);
        }
    }
    //..Qoder
}
