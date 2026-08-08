using LabWebMvc.MVC.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LabWebMvc.MVC.Areas.Utils
{
    public class DadosPdfFaturamento
    {
        public DateTime DataIni { get; set; }
        public DateTime DataFim { get; set; }
        public int Ordenacao { get; set; }
        public int MostragemPrecos { get; set; }
        public bool ExibirDataConclusao { get; set; }
        public List<ExameFaturamentoDto> Exames { get; set; } = [];
        public List<TotalFaturamentoDto> TotaisPorInstituicao { get; set; } = [];
        public List<string> TabelasUtilizadas { get; set; } = [];
        public List<QuantitativoItemDto> QuantitativoItens { get; set; } = [];
    }

    public class QuantitativoItemDto
    {
        public string ContaExame { get; set; } = "";
        public string Folha { get; set; } = "";
        public string Item { get; set; } = "";
        public int Quantidade { get; set; }

        public string DescricaoCompleta =>
            Folha.Equals(Item, StringComparison.OrdinalIgnoreCase)
                ? Folha.ToUpperInvariant()
                : $"{Folha.ToUpperInvariant()},{Item.ToUpperInvariant()}";
    }

    public class ExameFaturamentoDto
    {
        public int Sequencia { get; set; }
        public int ExameId { get; set; }
        public int PacienteId { get; set; }
        public string NomePaciente { get; set; } = "";
        public string SiglaInstituicao { get; set; } = "";
        public string NomeInstituicao { get; set; } = "";
        public string SiglaTabela { get; set; } = "";
        public string NomeTabela { get; set; } = "";
        public int Sequencial { get; set; }
        public DateTime? DataExame { get; set; }
        public List<ItemFaturamentoDto> Itens { get; set; } = [];
        public decimal ValorTotal => Itens.Sum(i => i.ValorItem);
    }

    public class ItemFaturamentoDto
    {
        public string Descricao { get; set; } = "";
        public decimal ValorItem { get; set; }
        public string ClasseExamesNome { get; set; } = "";
    }

    public class TotalFaturamentoDto
    {
        public string Descricao { get; set; } = "";
        public string Sigla { get; set; } = "";
        public decimal Valor { get; set; }
    }

    public class GeradorPdfFaturamento
    {
        private const double MargemEsquerda = 40;
        private const double MargemDireita = 40;
        private const double MargemTopo = 30;
        private const double ReservaRodape = 80;
        private const double LarguraPagina = 595.28;
        private const double AlturaPagina = 841.89;
        private const double AreaUtil = LarguraPagina - MargemEsquerda - MargemDireita;
        private const double LimiteY = AlturaPagina - ReservaRodape;
        private const double LarguraColunaValor = 70;
        private const double XFinalValor = LarguraPagina - MargemDireita;
        private readonly double XColunaValor = MargemEsquerda + AreaUtil - LarguraColunaValor;

        private readonly XFont _fontTitulo = new("Arial", 14, XFontStyle.Bold);
        private readonly XFont _fontTituloRelatorio = new("Arial", 12, XFontStyle.Bold);
        private readonly XFont _fontSubtitulo = new("Arial", 10, XFontStyle.Regular);
        private readonly XFont _fontNormal = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontNormalBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontPequena = new("Arial", 8, XFontStyle.Regular);
        private readonly XFont _fontPequenaBold = new("Arial", 8, XFontStyle.Bold);
        private readonly XFont _fontTotal = new("Arial", 10, XFontStyle.Bold);
        private readonly XFont _fontQuantitativo = new("Courier New", 10, XFontStyle.Bold);
        private readonly XFont _fontQuantitativoTitulo = new("Arial", 11, XFontStyle.Bold);
        private readonly XBrush _brushQuantitativo = new XSolidBrush(XColor.FromArgb(51, 51, 51));

        public byte[] Gerar(DadosPdfFaturamento dados, Empresa? empresa, bool duasColunas)
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = "Relatório de Faturamento por Período";
            pdfDocument.Info.Author = "LabWeb7";

            PdfPage page = pdfDocument.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            XGraphics gfx = XGraphics.FromPdfPage(page);
            double y = MargemTopo;

            y = DesenharCabecalho(gfx, dados, empresa, y);
            y = DesenharResumoFiltros(gfx, dados, y);

            decimal totalGeral = 0;
            string? instituicaoAtual = null;
            decimal totalInstituicao = 0;

            foreach (var exame in dados.Exames)
            {
                if (instituicaoAtual != exame.SiglaInstituicao)
                {
                    if (instituicaoAtual != null)
                    {
                        y = DesenharTotalInstituicao(gfx, instituicaoAtual, totalInstituicao, y);
                        totalInstituicao = 0;
                    }

                    instituicaoAtual = exame.SiglaInstituicao;
                }

                double alturaEstimada = 40 + (exame.Itens.Count * 14) + 20;
                if (y + alturaEstimada > LimiteY)
                {
                    DesenharRodape(gfx, page);
                    page = pdfDocument.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    y = MargemTopo;
                    y = DesenharCabecalho(gfx, dados, empresa, y);
                }

                y = DesenharExame(gfx, exame, y, duasColunas, dados);
                totalGeral += exame.ValorTotal;
                totalInstituicao += exame.ValorTotal;
            }

            if (instituicaoAtual != null)
            {
                y = DesenharTotalInstituicao(gfx, instituicaoAtual, totalInstituicao, y);
            }

            y = DesenharTotalGeral(gfx, totalGeral, dados, y);
            (y, gfx, page) = DesenharQuantitativoItens(gfx, page, dados, y);
            DesenharRodape(gfx, page);

            using var stream = new MemoryStream();
            pdfDocument.Save(stream, false);
            return stream.ToArray();
        }

        private double DesenharCabecalho(XGraphics gfx, DadosPdfFaturamento dados, Empresa? empresa, double y)
        {
            string titulo = empresa?.TituloEmpresa ?? "LABORATÓRIO";
            gfx.DrawString(titulo, _fontTitulo, XBrushes.Black, new XRect(0, y, LarguraPagina, 20), XStringFormats.TopCenter);
            y += 18;

            if (!string.IsNullOrEmpty(empresa?.SubTituloEmpresa))
            {
                gfx.DrawString(empresa.SubTituloEmpresa, _fontSubtitulo, XBrushes.Black, new XRect(0, y, LarguraPagina, 16), XStringFormats.TopCenter);
                y += 14;
            }

            string endereco = MontarEndereco(empresa);
            if (!string.IsNullOrEmpty(endereco))
            {
                gfx.DrawString(endereco, _fontPequena, XBrushes.Black, new XRect(0, y, LarguraPagina, 14), XStringFormats.TopCenter);
                y += 12;
            }

            if (!string.IsNullOrEmpty(empresa?.Telefones))
            {
                gfx.DrawString($"Tel: {empresa.Telefones}", _fontPequena, XBrushes.Black, new XRect(0, y, LarguraPagina, 14), XStringFormats.TopCenter);
                y += 12;
            }

            y += 10;

            string tituloRelatorio = dados.Exames.Select(e => e.SiglaInstituicao).Distinct().Count() > 1
                ? "FATURAMENTO GERAL"
                : $"Faturado para {dados.Exames.FirstOrDefault()?.NomeInstituicao} ({dados.Exames.FirstOrDefault()?.SiglaInstituicao})";

            gfx.DrawString(tituloRelatorio, _fontTituloRelatorio, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 20), XStringFormats.TopLeft);
            y += 20;

            gfx.DrawString($"Período de {dados.DataIni:dd/MM/yyyy} até {dados.DataFim:dd/MM/yyyy}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 16), XStringFormats.TopLeft);
            y += 16;

            if (dados.TabelasUtilizadas.Count > 0)
            {
                string tabelasTexto = "Tabelas de Preços: " + string.Join(", ", dados.TabelasUtilizadas);
                gfx.DrawString(tabelasTexto, _fontPequena, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
                y += 16;
            }

            return y;
        }

        private double DesenharResumoFiltros(XGraphics gfx, DadosPdfFaturamento dados, double y)
        {
            string ordenacao = dados.Ordenacao switch
            {
                0 => "Alfabética (Nome Paciente)",
                1 => "Sigla Instituição + Sequencial",
                _ => "Data, Sigla Instituição + Sequencial"
            };

            string mostragem = dados.MostragemPrecos switch
            {
                0 => "Aceitar zerados nos Exames dos Pacientes",
                1 => "Aceitar todos os zerados baseado no Plano de Exames",
                _ => "Não imprimir itens com valores zerados"
            };

            gfx.DrawString($"Ordenação: {ordenacao} | Mostragem: {mostragem}", _fontPequena, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;

            return y;
        }

        private double DesenharExame(XGraphics gfx, ExameFaturamentoDto exame, double y, bool duasColunas, DadosPdfFaturamento dados)
        {
            // Linha separadora
            gfx.DrawLine(XPens.LightGray, MargemEsquerda, y, LarguraPagina - MargemDireita, y);
            y += 6;

            // Referência: Instituicao-Sequencial formatado
            string referencia = $"{exame.SiglaInstituicao}-{exame.Sequencial:D6}";
            if (referencia.Length > 10)
            {
                referencia = referencia.Substring(0, 10) + "." + referencia.Substring(10);
            }

            gfx.DrawString($"{exame.Sequencia:N0}", _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, 40, 14), XStringFormats.TopLeft);
            gfx.DrawString($"Código único de Exame: {exame.ExameId}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 40, y, AreaUtil - 40, 14), XStringFormats.TopLeft);
            y += 14;

            double larguraColuna = AreaUtil / 3;
            gfx.DrawString($"Referência: {referencia}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, larguraColuna, 14), XStringFormats.TopLeft);
            gfx.DrawString($"Tabela: {exame.SiglaTabela}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + larguraColuna, y, larguraColuna, 14), XStringFormats.TopLeft);
            if (dados.ExibirDataConclusao)
                gfx.DrawString($"Data: {(exame.DataExame.HasValue ? exame.DataExame.Value.ToString("dd/MM/yyyy") : "")}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + 2 * larguraColuna, y, larguraColuna, 14), XStringFormats.TopLeft);
            y += 14;

            gfx.DrawString(exame.NomePaciente, _fontNormalBold, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 18;

            if (duasColunas)
            {
                double colunaLargura = (AreaUtil - 20) / 2;
                int metade = (exame.Itens.Count + 1) / 2;
                var coluna1 = exame.Itens.Take(metade).ToList();
                var coluna2 = exame.Itens.Skip(metade).ToList();
                double alturaMaxima = 0;

                for (int i = 0; i < Math.Max(coluna1.Count, coluna2.Count); i++)
                {
                    if (i < coluna1.Count)
                    {
                        gfx.DrawString(Truncar(coluna1[i].Descricao, 35), _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, colunaLargura - 60, 14), XStringFormats.TopLeft);

                        string valorTexto1 = coluna1[i].ValorItem.ToString("N2");
                        double larguraValor1 = gfx.MeasureString(valorTexto1, _fontNormal).Width;
                        gfx.DrawString(valorTexto1, _fontNormal, XBrushes.Black, MargemEsquerda + colunaLargura - larguraValor1, y);
                    }

                    if (i < coluna2.Count)
                    {
                        gfx.DrawString(Truncar(coluna2[i].Descricao, 35), _fontNormal, XBrushes.Black, new XRect(MargemEsquerda + colunaLargura + 10, y, colunaLargura - 70, 14), XStringFormats.TopLeft);

                        string valorTexto2 = coluna2[i].ValorItem.ToString("N2");
                        double larguraValor2 = gfx.MeasureString(valorTexto2, _fontNormal).Width;
                        gfx.DrawString(valorTexto2, _fontNormal, XBrushes.Black, XFinalValor - larguraValor2, y);
                    }

                    y += 14;
                    alturaMaxima += 14;
                }
            }
            else
            {
                foreach (var item in exame.Itens)
                {
                    gfx.DrawString(Truncar(item.Descricao, 70), _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil - LarguraColunaValor, 14), XStringFormats.TopLeft);

                    string valorItemTexto = item.ValorItem.ToString("N2");
                    double larguraValorItem = gfx.MeasureString(valorItemTexto, _fontNormal).Width;
                    gfx.DrawString(valorItemTexto, _fontNormal, XBrushes.Black, XFinalValor - larguraValorItem, y);

                    y += 14;
                }
            }

            // Total do exame/paciente
            string totalTexto = exame.ValorTotal.ToString("N2");
            double larguraTotal = gfx.MeasureString(totalTexto, _fontNormalBold).Width;
            gfx.DrawString("Total:", _fontNormalBold, XBrushes.Black, new XRect(XColunaValor - 60, y, 60, 14), XStringFormats.TopLeft);
            gfx.DrawString(totalTexto, _fontNormalBold, XBrushes.Black, XFinalValor - larguraTotal, y);
            y += 18;

            return y;
        }

        private double DesenharTotalInstituicao(XGraphics gfx, string siglaInstituicao, decimal total, double y)
        {
            if (y + 30 > LimiteY)
            {
                // Se não couber, deixa para próxima página não implementado aqui por simplicidade
            }

            gfx.DrawLine(XPens.Gray, MargemEsquerda, y, LarguraPagina - MargemDireita, y);
            y += 4;
            gfx.DrawString($"Total Instituição {siglaInstituicao}:", _fontTotal, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil - 70, 16), XStringFormats.TopLeft);
            gfx.DrawString(total.ToString("N2"), _fontTotal, XBrushes.Black, new XRect(MargemEsquerda + AreaUtil - 70, y, 70, 16), XStringFormats.TopRight);
            y += 18;

            return y;
        }

        private double DesenharTotalGeral(XGraphics gfx, decimal totalGeral, DadosPdfFaturamento dados, double y)
        {
            gfx.DrawLine(XPens.Black, MargemEsquerda, y, LarguraPagina - MargemDireita, y);
            y += 6;

            gfx.DrawString("TOTAL GERAL:", _fontTitulo, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil - 80, 20), XStringFormats.TopLeft);
            gfx.DrawString(totalGeral.ToString("N2"), _fontTitulo, XBrushes.Black, new XRect(MargemEsquerda + AreaUtil - 80, y, 80, 20), XStringFormats.TopRight);
            y += 22;

            int totalPacientes = dados.Exames.Select(e => e.PacienteId).Distinct().Count();
            int totalItens = dados.Exames.Sum(e => e.Itens.Count);

            gfx.DrawString($"Total de Registros: {dados.Exames.Count:N0}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 14;
            gfx.DrawString($"Total de Pacientes: {totalPacientes:N0}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 14;
            gfx.DrawString($"Total de Itens: {totalItens:N0}", _fontNormal, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 18;

            return y;
        }

        private (double y, XGraphics gfx, PdfPage page) DesenharQuantitativoItens(XGraphics gfx, PdfPage page, DadosPdfFaturamento dados, double y)
        {
            if (dados.QuantitativoItens.Count == 0)
                return (y, gfx, page);

            // Verifica espaco na pagina; se necessario, cria nova pagina
            double alturaEstimada = 50 + (dados.QuantitativoItens.Count * 14) + 20;
            if (y + alturaEstimada > LimiteY)
            {
                DesenharRodape(gfx, page);
                page = gfx.PdfPage.Owner.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                y = MargemTopo;
            }

            y += 10;
            gfx.DrawString("QUANTITATIVO DE ITENS DE EXAMES REALIZADOS:", _fontQuantitativoTitulo, XBrushes.Black, new XRect(MargemEsquerda, y, AreaUtil, 16), XStringFormats.TopLeft);
            y += 18;

            string cabecalhoQuant = RetornaLinhaPontilhada("Folha de Exame, Item", "Quantidade");
            gfx.DrawString(cabecalhoQuant, _fontQuantitativo, _brushQuantitativo, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
            y += 16;

            foreach (var item in dados.QuantitativoItens)
            {
                string descricao = item.DescricaoCompleta;
                string quantidade = item.Quantidade.ToString("N0");
                string linha = RetornaLinhaPontilhada(descricao, quantidade);

                gfx.DrawString(linha, _fontQuantitativo, _brushQuantitativo, new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
                y += 14;
            }

            return (y, gfx, page);
        }

        private static string RetornaLinhaPontilhada(string descricao, string quantidade)
        {
            const int totalCaracteres = 85;
            int pontos = totalCaracteres - (descricao.Length + quantidade.Length);
            if (pontos < 1) pontos = 1;
            return descricao + new string('.', pontos) + quantidade;
        }

        private void DesenharRodape(XGraphics gfx, PdfPage page)
        {
            double yRodape = AlturaPagina - 50;
            gfx.DrawLine(XPens.LightGray, MargemEsquerda, yRodape, LarguraPagina - MargemDireita, yRodape);
            gfx.DrawString("Relatório de Faturamento", _fontPequena, XBrushes.Black, new XRect(MargemEsquerda, yRodape + 5, AreaUtil, 14), XStringFormats.TopLeft);
            gfx.DrawString($"Impresso em {DateTime.Now:dd/MM/yyyy HH:mm}", _fontPequena, XBrushes.Black, new XRect(MargemEsquerda, yRodape + 5, AreaUtil, 14), XStringFormats.TopRight);
        }

        private static string MontarEndereco(Empresa? empresa)
        {
            if (empresa == null) return "";

            var partes = new List<string>();
            if (!string.IsNullOrEmpty(empresa.Endereco))
                partes.Add(empresa.Endereco);
            if (!string.IsNullOrEmpty(empresa.Numero))
                partes.Add(empresa.Numero);
            if (!string.IsNullOrEmpty(empresa.Bairro))
                partes.Add(empresa.Bairro);
            if (!string.IsNullOrEmpty(empresa.Cidade))
                partes.Add($"{empresa.Cidade}/{empresa.UF}");

            return string.Join(", ", partes);
        }

        private static string Truncar(string texto, int maximo)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto.Length <= maximo ? texto : texto.Substring(0, maximo - 3) + "...";
        }
    }
}
