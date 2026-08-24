using LabWebMvc.MVC.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LabWebMvc.MVC.Areas.Utils
{
    //Feito pelo Qoder em 23/08/2026
    // DTO do bloco de exame impresso no Mapa Agrupado (um paciente/exame por bloco).
    public class ExameMapaAgrupadoDto
    {
        public int ExameRealizadoId { get; set; }
        public int PacienteId { get; set; }
        public string NomePaciente { get; set; } = "";
        public DateTime Nascimento { get; set; }
        public string? Sexo { get; set; }
        public string SiglaInstituicao { get; set; } = "";
        public int Sequencial { get; set; }
        public string SiglaTabela { get; set; } = "";
        public string NomeMedico { get; set; } = "";
        public string CRM { get; set; } = "";
        public string? ControleApoio { get; set; }
        public string? HistoricoClinico { get; set; }
        /// <summary>Itens do exame dentro do lote (abreviação MapaHorizontal ou Descrição).</summary>
        public List<string> Itens { get; set; } = new();
        /// <summary>Todos os itens do exame (abreviação ou descrição) para a linha "Exames:".</summary>
        public List<string> ExamesDoPaciente { get; set; } = new();
    }

    //Feito pelo Qoder em 23/08/2026
    // Gerador do PDF do Mapa Agrupado (portabilidade do FRelMapaAgrupado QuickReport):
    // uma seção por folha (NomeFicha) com cabeçalho de lote, blocos de paciente com
    // seus itens em até 5 colunas, linha de exames e histórico clínico.
    public class GeradorPdfMapaAgrupado
    {
        private const double MargemTopo = 30;
        private const double MargemEsquerda = 30;
        private const double AreaUtil = 535;   // A4 595pt - 2*30
        private const double LimiteY = 800;    // A4 842pt - rodapé

        private readonly XFont _fontTitulo = new("Arial", 11, XFontStyle.Bold);
        private readonly XFont _fontSecao = new("Arial", 10, XFontStyle.Bold);
        private readonly XFont _fontNormal = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontNormalBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontPequena = new("Arial", 8, XFontStyle.Regular);
        private readonly XFont _fontItem = new("Arial", 9, XFontStyle.Regular);
        private readonly XPen _penLinha = new(XColors.Gray, 0.6);

        public byte[] Gerar(string nomeFolha, int lote, DateTime dataMapa,
            List<ExameMapaAgrupadoDto> exames, Empresa? empresa)
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = $"Mapa Agrupado - {nomeFolha} - Lote {lote}";
            pdfDocument.Info.Author = "LabWeb7";

            PdfPage page = pdfDocument.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            XGraphics gfx = XGraphics.FromPdfPage(page);
            double y = MargemTopo;

            y = DesenharCabecalho(gfx, nomeFolha, lote, dataMapa, empresa, y);

            foreach (var exame in exames)
            {
                double alturaNecessaria = CalcularAlturaBloco(exame);

                // O bloco do paciente deve caber inteiro na página (padrão Pula_Pagina do Delphi).
                if (y + alturaNecessaria > LimiteY)
                {
                    DesenharRodape(gfx, pdfDocument.PageCount);
                    page = pdfDocument.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    y = MargemTopo;
                    y = DesenharCabecalho(gfx, nomeFolha, lote, dataMapa, empresa, y);
                }

                y = DesenharBlocoExame(gfx, exame, y);
            }

            DesenharRodape(gfx, pdfDocument.PageCount);

            using var stream = new MemoryStream();
            pdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private double DesenharCabecalho(XGraphics gfx, string nomeFolha, int lote, DateTime dataMapa, Empresa? empresa, double y)
        {
            string titulo = $"{empresa?.Sigla ?? ""} - {empresa?.TituloEmpresa ?? "LABORATÓRIO"}".TrimStart('-', ' ');
            gfx.DrawString(titulo, _fontTitulo, XBrushes.Black, new XRect(0, y, 595, 18), XStringFormats.TopCenter);
            y += 18;

            gfx.DrawString(nomeFolha, _fontSecao, XBrushes.Black, new XRect(0, y, 595, 16), XStringFormats.TopCenter);
            y += 16;

            gfx.DrawString($"LOTE Nº {lote},   EXAMES COLETADOS NA DATA: {dataMapa:dd/MM/yyyy}",
                _fontNormalBold, XBrushes.Black, new XRect(0, y, 595, 14), XStringFormats.TopCenter);
            y += 14;

            gfx.DrawString("Ordenado por \"Código do Exame\", Formato deste Mapa: Código Paciente-Nome Paciente-Nascimento-Idade-Sexo.",
                _fontPequena, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 12), XStringFormats.TopLeft);
            y += 16;

            gfx.DrawLine(_penLinha, MargemEsquerda, y, MargemEsquerda + AreaUtil, y);
            y += 8;

            return y;
        }

        private double DesenharBlocoExame(XGraphics gfx, ExameMapaAgrupadoDto exame, double y)
        {
            int idade = CalcularIdade(exame.Nascimento);

            string linhaPaciente = $"  {exame.PacienteId}: {Truncar(exame.NomePaciente, 30)}" +
                $",  Nascimento: {exame.Nascimento:dd/MM/yyyy},  I: {idade},  Sx: {exame.Sexo}" +
                $",  Cód.Exame: {exame.ExameRealizadoId}";
            gfx.DrawString(linhaPaciente, _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 13;

            string linhaLaboratorio = $"  Instituição: ({exame.SiglaInstituicao}-{exame.Sequencial:000\\,000})" +
                $", Tab.Preços({exame.SiglaTabela})" +
                $",  Médico: {exame.NomeMedico.Trim()}/CRM: {exame.CRM.Trim()}" +
                $",  Controle Apoio: {FormatarControleApoio(exame.ControleApoio)}";
            gfx.DrawString(linhaLaboratorio, _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 13;

            // Itens do lote em linhas de até 5 colunas (Item1..Item5 do QuickReport).
            double larguraColuna = AreaUtil / 5;
            for (int i = 0; i < exame.Itens.Count; i += 5)
            {
                var linhaItens = exame.Itens.Skip(i).Take(5).ToList();
                for (int c = 0; c < linhaItens.Count; c++)
                {
                    string texto = linhaItens[c];
                    gfx.DrawString(" " + Truncar(texto, 20), _fontItem, XBrushes.Black,
                        new XRect(MargemEsquerda + c * larguraColuna, y, larguraColuna, 12), XStringFormats.TopLeft);
                }
                y += 11;
            }

            // Linha de todos os exames do paciente (BandaOutrosExames).
            if (exame.ExamesDoPaciente.Count > 0)
            {
                string linhaExames = "  Exames: " + string.Join(", ", exame.ExamesDoPaciente);
                y = DesenharTextoQuebrado(gfx, linhaExames, _fontPequena, y);
            }

            // Histórico clínico ou linha "OBS:" (BandaHistorico).
            string historico = !string.IsNullOrWhiteSpace(exame.HistoricoClinico) && exame.HistoricoClinico.Trim().Length > 10
                ? "  Histórico: " + exame.HistoricoClinico.Trim()
                : "  OBS:";
            y = DesenharTextoQuebrado(gfx, historico, _fontPequena, y);

            y += 14; // SaltoPaciente
            gfx.DrawLine(_penLinha, MargemEsquerda, y - 6, MargemEsquerda + AreaUtil, y - 6);

            return y;
        }

        /// <summary>
        /// Desenha um texto quebrando linhas quando ultrapassa a área útil.
        /// </summary>
        private double DesenharTextoQuebrado(XGraphics gfx, string texto, XFont fonte, double y)
        {
            var medida = gfx.MeasureString(texto, fonte);
            if (medida.Width <= AreaUtil)
            {
                gfx.DrawString(texto, fonte, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 12), XStringFormats.TopLeft);
                return y + 11;
            }

            var palavras = texto.Split(' ');
            string linha = "";
            foreach (var palavra in palavras)
            {
                string candidata = linha.Length == 0 ? palavra : linha + " " + palavra;
                if (gfx.MeasureString(candidata, fonte).Width > AreaUtil && linha.Length > 0)
                {
                    gfx.DrawString(linha, fonte, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 12), XStringFormats.TopLeft);
                    y += 11;
                    linha = palavra;
                }
                else
                {
                    linha = candidata;
                }
            }
            if (linha.Length > 0)
            {
                gfx.DrawString(linha, fonte, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 12), XStringFormats.TopLeft);
                y += 11;
            }
            return y;
        }

        private static double CalcularAlturaBloco(ExameMapaAgrupadoDto exame)
        {
            int linhasItens = Math.Max(1, (int)Math.Ceiling(exame.Itens.Count / 5.0));
            // paciente (13) + instituição (13) + itens + exames (~2 linhas) + histórico (~2 linhas) + salto (14)
            return 13 + 13 + linhasItens * 11 + 22 + 22 + 14;
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
            gfx.DrawString($"Página {numeroPagina}", _fontPequena, XBrushes.Gray,
                new XRect(0, 820, 595, 14), XStringFormats.TopCenter);
        }
    }
    //..Qoder
}
