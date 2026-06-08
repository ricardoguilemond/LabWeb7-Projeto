using System.Text;
using System.Text.RegularExpressions;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LabWebMvc.MVC.Areas.Utils
{
    //Feito pelo Kiro em 19/06/2026

    /// <summary>
    /// DTO com todos os dados necessários para geração do PDF de resultado de exame.
    /// Desacoplado do DbContext — recebe apenas valores primitivos e listas.
    /// </summary>
    public class DadosPdfResultado
    {
        // Empresa (cabeçalho/timbre do laudo — sempre da tabela Empresa)
        public string TituloEmpresa { get; set; } = "";
        public string SubTituloEmpresa { get; set; } = "";
        public string EnderecoEmpresa { get; set; } = "";
        public string TelefoneEmpresa { get; set; } = "";
        public string EmailEmpresa { get; set; } = "";

        // Instituição (logo + dados para convênio origem)
        public byte[]? LogoInstituicao { get; set; }
        public string NomeInstituicao { get; set; } = "";
        public string SiglaInstituicao { get; set; } = "";

        // Paciente
        public int PacienteId { get; set; }
        public string NomePaciente { get; set; } = "";
        public DateTime? Nascimento { get; set; }
        public string Sexo { get; set; } = "";
        public string Procedencia { get; set; } = "";

        // Médico
        public string NomeMedico { get; set; } = "";

        // Exame header
        public int ExameId { get; set; }
        public string ControleApoioFormatado { get; set; } = "";
        public string SequencialFormatado { get; set; } = "";
        public string DataExameColeta { get; set; } = "";
        public string DataLaudoLiberado { get; set; } = "";
        public string DataImpressao { get; set; } = "";
        public string HoraImpressao { get; set; } = "";

        // Itens (lista ordenada por ContaExame)
        public List<ItemPdfResultado> Itens { get; set; } = [];

        // Assinaturas (até 4)
        public List<AssinaturaPdf> Assinaturas { get; set; } = [];

        // Caminho base para laudos fixos (.DOC)
        public string CaminhoLaudos { get; set; } = "";
    }

    public class ItemPdfResultado
    {
        public string ContaExame { get; set; } = "";
        public string Folha { get; set; } = "";
        public string Descricao { get; set; } = "";
        public string Resultado { get; set; } = "";
        public string UnidadeMedida { get; set; } = "";
        public string Referencia { get; set; } = "";
        public bool EhPrincipal { get; set; }
        public int AlinhaLaudo { get; set; } // 0=esquerda, 1=direita (do PlanoExames)
    }

    public class AssinaturaPdf
    {
        public byte[]? ImagemAssinatura { get; set; }
        public string Credenciais { get; set; } = "";
    }

    /// <summary>
    /// Classe helper para geração do PDF de resultado de exame no layout profissional
    /// baseado no modelo Delphi original. Usa PdfSharpCore (MIT).
    /// </summary>
    public class GeradorPdfResultado
    {
        // Constantes de layout (A4: 595.28 x 841.89 pontos)
        private const double MargemEsquerda = 40;
        private const double MargemDireita = 40;
        private const double MargemTopo = 30;
        private const double ReservaRodape = 155;
        private const double LarguraPagina = 595.28;
        private const double AlturaPagina = 841.89;
        private const double AreaUtil = LarguraPagina - MargemEsquerda - MargemDireita;
        private const double LimiteY = AlturaPagina - ReservaRodape;

        // Fontes
        private readonly XFont _fontTitulo = new("Arial", 14, XFontStyle.Bold);
        private readonly XFont _fontSubtitulo = new("Arial", 10, XFontStyle.Regular);
        private readonly XFont _fontNormal = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontNormalBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontDadosPaciente = new("Arial", 11, XFontStyle.Regular);
        private readonly XFont _fontDadosPacienteBold = new("Arial", 11, XFontStyle.Bold);
        private readonly XFont _fontFolha = new("Arial", 18, XFontStyle.Bold);
        private readonly XFont _fontPrincipal = new("Arial", 10, XFontStyle.Bold);
        private readonly XFont _fontItem = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontItemNormal = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontLaudo = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontRodape = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontRodapeBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontPequena = new("Arial", 8, XFontStyle.Regular);

        // Cores
        private readonly XColor _corFaixaCinza = XColor.FromArgb(226, 226, 226); // #E2E2E2
        private readonly XColor _corVerde = XColor.FromArgb(0, 128, 0);
        private readonly XColor _corBarraVerde = XColor.FromArgb(0, 100, 0);
        private readonly XSolidBrush _brushCinzaEscuro = new(XColor.FromArgb(40, 40, 40)); // quase preto

        public byte[] Gerar(DadosPdfResultado dados)
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = $"Resultado de Exame - {dados.ExameId}";
            pdfDocument.Info.Author = "LabWeb7";

            // Agrupar itens por folha (mudança de folha = nova página)
            var gruposFolha = AgruparPorFolha(dados.Itens);

            foreach (var grupo in gruposFolha)
            {
                // Cada folha inicia em nova página
                PdfPage page = pdfDocument.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);
                double y = MargemTopo;

                // ZONA 1 + ZONA 2 — Cabeçalho + Dados Paciente/Exame
                y = DesenharCabecalho(gfx, dados, y);

                // ZONA 3 — Título da Folha
                y = DesenharTituloFolha(gfx, grupo.NomeFolha, y);

                // ZONA 4 — Resultados dos itens
                int indiceItem = 0;
                foreach (var item in grupo.Itens)
                {
                    // Verificar se precisa nova página
                    double alturaEstimada = item.EhPrincipal ? 24 : 14;
                    if (y + alturaEstimada > LimiteY)
                    {
                        // Desenhar rodapé na página atual
                        DesenharRodape(gfx, page, dados);

                        // Nova página com cabeçalho repetido
                        page = pdfDocument.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);
                        y = MargemTopo;
                        y = DesenharCabecalho(gfx, dados, y);
                        y = DesenharTituloFolha(gfx, grupo.NomeFolha, y);
                    }

                    y = DesenharItem(gfx, item, y, indiceItem, dados.CaminhoLaudos);
                    indiceItem++;
                }

                // ZONA 5 — Assinaturas (antes do rodapé)
                y = DesenharAssinaturas(gfx, dados.Assinaturas, y);

                // ZONA 6 — Rodapé
                DesenharRodape(gfx, page, dados);
            }

            // Se não há itens, gerar página vazia com cabeçalho
            if (gruposFolha.Count == 0)
            {
                PdfPage page = pdfDocument.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);
                double y = MargemTopo;
                y = DesenharCabecalho(gfx, dados, y);
                DesenharRodape(gfx, page, dados);
            }

            using var ms = new MemoryStream();
            pdfDocument.Save(ms, false);
            return ms.ToArray();
        }

        #region ZONA 1 — Cabeçalho com Logo + ZONA 2 — Dados Paciente/Exame

        private double DesenharCabecalho(XGraphics gfx, DadosPdfResultado dados, double y)
        {
            // --- Logo da instituição (centralizada) ---
            if (dados.LogoInstituicao != null && dados.LogoInstituicao.Length > 0)
            {
                try
                {
                    using var logoStream = new MemoryStream(dados.LogoInstituicao);
                    var logoImage = XImage.FromStream(() => logoStream);

                    // Escalar mantendo proporção, max 160x80
                    double maxW = 160, maxH = 80;
                    double escala = Math.Min(maxW / logoImage.PixelWidth, maxH / logoImage.PixelHeight);
                    double imgW = logoImage.PixelWidth * escala;
                    double imgH = logoImage.PixelHeight * escala;
                    double imgX = MargemEsquerda + (AreaUtil - imgW) / 2;

                    gfx.DrawImage(logoImage, imgX, y, imgW, imgH);
                    y += imgH + 4;
                }
                catch
                {
                    // Se falhar ao carregar imagem, segue sem logo
                }
            }

            // --- Nome da empresa (timbre padrão) ---
            gfx.DrawString(dados.TituloEmpresa, _fontTitulo, XBrushes.Black,
                new XRect(MargemEsquerda, y, AreaUtil, 18), XStringFormats.TopCenter);
            y += 18;

            // --- SubTítulo da empresa ---
            if (!string.IsNullOrWhiteSpace(dados.SubTituloEmpresa))
            {
                gfx.DrawString(dados.SubTituloEmpresa, _fontSubtitulo, XBrushes.Black,
                    new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopCenter);
                y += 14;
            }

            // --- Endereço da empresa ---
            if (!string.IsNullOrWhiteSpace(dados.EnderecoEmpresa))
            {
                gfx.DrawString(dados.EnderecoEmpresa, _fontPequena, XBrushes.Black,
                    new XRect(MargemEsquerda, y, AreaUtil, 11), XStringFormats.TopCenter);
                y += 11;
            }

            // --- Telefone + Email da empresa ---
            string contatoLinha = "";
            if (!string.IsNullOrWhiteSpace(dados.TelefoneEmpresa))
                contatoLinha += "Tel: " + dados.TelefoneEmpresa;
            if (!string.IsNullOrWhiteSpace(dados.EmailEmpresa))
                contatoLinha += (contatoLinha.Length > 0 ? "   |   " : "") + "Email: " + dados.EmailEmpresa;

            if (!string.IsNullOrWhiteSpace(contatoLinha))
            {
                gfx.DrawString(contatoLinha, _fontPequena, XBrushes.Black,
                    new XRect(MargemEsquerda, y, AreaUtil, 11), XStringFormats.TopCenter);
                y += 11;
            }

            y += 6;

            // Linha separadora verde
            var penVerde = new XPen(_corVerde, 1.5);
            gfx.DrawLine(penVerde, MargemEsquerda, y, LarguraPagina - MargemDireita, y);
            y += 10;

            // --- ZONA 2: Dados do Paciente e Exame (duas colunas) ---
            double colEsquerdaW = AreaUtil * 0.58;
            double colDireitaX = MargemEsquerda + colEsquerdaW + 10;
            double colDireitaW = AreaUtil - colEsquerdaW - 10;
            double yInicioZona2 = y;

            // Coluna esquerda
            string idPaciente = $"( {dados.PacienteId} )    Nome: {dados.NomePaciente}";
            gfx.DrawString(idPaciente, _fontDadosPacienteBold, XBrushes.Black,
                new XRect(MargemEsquerda, y, colEsquerdaW, 14), XStringFormats.TopLeft);
            y += 15;

            string idadeTexto = "";
            if (dados.Nascimento.HasValue)
            {
                int idade = CalcularIdade(dados.Nascimento.Value);
                idadeTexto = $"Data Nascimento: {dados.Nascimento.Value:dd/MM/yyyy} ({idade} anos), Sexo: {dados.Sexo}";
            }
            else
            {
                idadeTexto = $"Sexo: {dados.Sexo}";
            }
            gfx.DrawString(idadeTexto, _fontDadosPaciente, XBrushes.Black,
                new XRect(MargemEsquerda, y, colEsquerdaW, 14), XStringFormats.TopLeft);
            y += 15;

            string medicoTexto = $"Médico Solicitante: Dr(a). {dados.NomeMedico}";
            gfx.DrawString(medicoTexto, _fontDadosPaciente, XBrushes.Black,
                new XRect(MargemEsquerda, y, colEsquerdaW, 14), XStringFormats.TopLeft);
            y += 15;

            string convenioTexto = $"Convênio Origem: {dados.SiglaInstituicao} - {dados.SequencialFormatado} / {dados.NomeInstituicao}";
            gfx.DrawString(convenioTexto, _fontDadosPaciente, XBrushes.Black,
                new XRect(MargemEsquerda, y, colEsquerdaW, 14), XStringFormats.TopLeft);
            y += 15;

            if (!string.IsNullOrWhiteSpace(dados.Procedencia))
            {
                string procedenciaTexto = $"Procedência: {dados.Procedencia}";
                gfx.DrawString(procedenciaTexto, _fontDadosPaciente, XBrushes.Black,
                    new XRect(MargemEsquerda, y, colEsquerdaW, 14), XStringFormats.TopLeft);
                y += 15;
            }

            // Coluna direita (alinhada à direita)
            double yDir = yInicioZona2;
            gfx.DrawString($"Código do Exame: {dados.ExameId}", _fontDadosPaciente, XBrushes.Black,
                new XRect(colDireitaX, yDir, colDireitaW, 14), XStringFormats.TopRight);
            yDir += 15;

            if (!string.IsNullOrWhiteSpace(dados.ControleApoioFormatado))
            {
                gfx.DrawString($"Código de Coleta: {dados.ControleApoioFormatado}", _fontDadosPaciente, XBrushes.Black,
                    new XRect(colDireitaX, yDir, colDireitaW, 14), XStringFormats.TopRight);
                yDir += 15;
            }

            if (!string.IsNullOrWhiteSpace(dados.DataExameColeta))
            {
                gfx.DrawString($"Data do Exame/Coleta: {dados.DataExameColeta}", _fontDadosPaciente, XBrushes.Black,
                    new XRect(colDireitaX, yDir, colDireitaW, 14), XStringFormats.TopRight);
                yDir += 15;
            }

            if (!string.IsNullOrWhiteSpace(dados.DataLaudoLiberado))
            {
                gfx.DrawString($"Laudo Liberado em: {dados.DataLaudoLiberado}", _fontDadosPaciente, XBrushes.Black,
                    new XRect(colDireitaX, yDir, colDireitaW, 14), XStringFormats.TopRight);
                yDir += 15;
            }

            gfx.DrawString($"Laudo Impresso em: {dados.DataImpressao}", _fontDadosPaciente, XBrushes.Black,
                new XRect(colDireitaX, yDir, colDireitaW, 14), XStringFormats.TopRight);
            yDir += 15;

            // Usar o maior Y entre as duas colunas
            y = Math.Max(y, yDir) + 8;

            // Linha separadora
            gfx.DrawLine(penVerde, MargemEsquerda, y, LarguraPagina - MargemDireita, y);
            y += 10;

            return y;
        }

        #endregion

        #region ZONA 3 — Título da Folha de Exame

        private double DesenharTituloFolha(XGraphics gfx, string nomeFolha, double y)
        {
            // Barra vertical verde à esquerda (3px de largura) — conectada à linha horizontal acima
            var penBarra = new XPen(_corVerde, 3);
            gfx.DrawLine(penBarra, MargemEsquerda, y - 10, MargemEsquerda, y + 28);

            // Nome da folha em font grande bold
            gfx.DrawString(nomeFolha, _fontFolha, XBrushes.Black,
                new XRect(MargemEsquerda + 10, y, AreaUtil - 10, 24), XStringFormats.CenterLeft);
            y += 28;

            // Sub-cabeçalho de colunas — alinhados com as posições reais dos dados
            // "Valores Obtidos / Unidade de Medida" alinha com a coluna Resultado (posição 300)
            y += 4;
            gfx.DrawString("Valores Obtidos /Unidade de Medida", _fontPequena, _brushCinzaEscuro,
                new XRect(300, y, 150, 10), XStringFormats.TopLeft);
            gfx.DrawString("Valores de Referência", _fontPequena, _brushCinzaEscuro,
                new XRect(MargemEsquerda, y, AreaUtil, 10), XStringFormats.TopRight);
            y += 12;

            gfx.DrawString("RESULTADO", _fontNormalBold, XBrushes.Black,
                new XRect(MargemEsquerda, y, AreaUtil, 12), XStringFormats.TopLeft);
            y += 16;

            return y;
        }

        #endregion

        #region ZONA 4 — Resultados

        private double DesenharItem(XGraphics gfx, ItemPdfResultado item, double y, int indice, string caminhoLaudos)
        {
            if (item.EhPrincipal)
            {
                // Item Principal: descrição em bold maiúscula, font 10pt, sem indentação
                y += 6;
                gfx.DrawString(item.Descricao.ToUpper(), _fontPrincipal, XBrushes.Black,
                    new XRect(MargemEsquerda, y, AreaUtil, 14), XStringFormats.TopLeft);
                y += 16;
            }
            else
            {
                // Sub-item: faixa alternada
                if (indice % 2 == 0)
                {
                    var brush = new XSolidBrush(_corFaixaCinza);
                    gfx.DrawRectangle(brush, MargemEsquerda, y - 1, AreaUtil, 13);
                }

                // Descrição indentada 36px (maiúscula conforme Delphi)
                double xDesc = MargemEsquerda + 36;
                gfx.DrawString(item.Descricao.ToUpper(), _fontItem, XBrushes.Black,
                    new XRect(xDesc, y, 200, 12), XStringFormats.TopLeft);

                // Resultado + Unidade no centro (~300pt)
                string resultadoUnidade = item.Resultado;
                if (!string.IsNullOrWhiteSpace(item.UnidadeMedida))
                    resultadoUnidade += "  " + item.UnidadeMedida;
                gfx.DrawString(resultadoUnidade, _fontItemNormal, XBrushes.Black,
                    new XRect(300, y, 120, 12), XStringFormats.TopLeft);

                // Referência à direita (~420pt)
                gfx.DrawString(item.Referencia, _fontItemNormal, _brushCinzaEscuro,
                    new XRect(420, y, 135, 12), XStringFormats.TopLeft);

                y += 14;

                // Laudo fixo (.DOC)
                y = DesenharLaudoFixo(gfx, item, y, caminhoLaudos);
            }

            return y;
        }

        private double DesenharLaudoFixo(XGraphics gfx, ItemPdfResultado item, double y, string caminhoLaudos)
        {
            if (string.IsNullOrWhiteSpace(caminhoLaudos) || string.IsNullOrWhiteSpace(item.ContaExame))
                return y;

            string caminhoDoc = Path.Combine(caminhoLaudos, item.ContaExame + ".DOC");
            if (!File.Exists(caminhoDoc))
                return y;

            try
            {
                // Ler arquivo .DOC (RTF simples do Delphi) — Windows-1252 é o encoding padrão do Delphi
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                string conteudo;
                try
                {
                    conteudo = File.ReadAllText(caminhoDoc, Encoding.GetEncoding(1252));
                }
                catch
                {
                    conteudo = File.ReadAllText(caminhoDoc, Encoding.UTF8);
                }

                // Decodificar caracteres RTF acentuados (\'XX = byte hexadecimal Windows-1252)
                conteudo = Regex.Replace(conteudo, @"\\'([0-9a-fA-F]{2})", match =>
                {
                    byte b = Convert.ToByte(match.Groups[1].Value, 16);
                    return Encoding.GetEncoding(1252).GetString(new[] { b });
                });

                // Strip RTF tags preservando texto
                conteudo = Regex.Replace(conteudo, @"\{\\[^}]*\}", ""); // Remove grupos RTF
                conteudo = Regex.Replace(conteudo, @"\\par\b\s?", "\n"); // Quebras de parágrafo
                conteudo = Regex.Replace(conteudo, @"\\[a-z]+\d*\s?", ""); // Remove comandos RTF
                conteudo = Regex.Replace(conteudo, @"[{}]", ""); // Remove chaves restantes
                conteudo = conteudo.Trim();

                if (string.IsNullOrWhiteSpace(conteudo))
                    return y;

                // Alinhamento conforme AlinhaLaudo: 0=esquerda, 1=direita
                var formatoLaudo = item.AlinhaLaudo == 1
                    ? XStringFormats.TopRight
                    : XStringFormats.TopLeft;

                double maxLarguraLaudo = AreaUtil - 40;

                // Quebrar em linhas (limite ~100 chars por linha), ignorar linhas vazias
                var linhas = QuebrarTexto(conteudo, 110);
                foreach (var linha in linhas)
                {
                    if (string.IsNullOrWhiteSpace(linha))
                        continue; // Não pular linha — sem espaço vazio entre parágrafos

                    if (y + 11 > LimiteY)
                        break; // não ultrapassar área reservada ao rodapé

                    gfx.DrawString(linha, _fontLaudo, _brushCinzaEscuro,
                        new XRect(MargemEsquerda + 40, y, maxLarguraLaudo, 11), formatoLaudo);
                    y += 11;
                }
                y += 3;
            }
            catch
            {
                // Se falhar ao ler laudo, ignorar
            }

            return y;
        }

        #endregion

        #region ZONA 5 — Assinaturas

        private double DesenharAssinaturas(XGraphics gfx, List<AssinaturaPdf> assinaturas, double y)
        {
            if (assinaturas.Count == 0)
                return y;

            y += 10;

            // Posicionar assinaturas lado a lado
            int qtdAss = assinaturas.Count;
            double larguraBloco = AreaUtil / qtdAss;
            double imgW = 160, imgH = 96;

            // Verificar espaço disponível (assinatura ~110pt de altura)
            if (y + 110 > LimiteY)
                return y; // não desenhar se não cabe antes do rodapé

            for (int i = 0; i < qtdAss; i++)
            {
                var ass = assinaturas[i];
                double xBloco = MargemEsquerda + (i * larguraBloco);
                double xCentro = xBloco + (larguraBloco - imgW) / 2;

                // Imagem da assinatura
                if (ass.ImagemAssinatura != null && ass.ImagemAssinatura.Length > 0)
                {
                    try
                    {
                        using var assStream = new MemoryStream(ass.ImagemAssinatura);
                        var assImage = XImage.FromStream(() => assStream);

                        // Escalar para 160x96 máximo
                        double escala = Math.Min(imgW / assImage.PixelWidth, imgH / assImage.PixelHeight);
                        double w = assImage.PixelWidth * escala;
                        double h = assImage.PixelHeight * escala;
                        double xImg = xBloco + (larguraBloco - w) / 2;

                        gfx.DrawImage(assImage, xImg, y, w, h);
                    }
                    catch
                    {
                        // Se falhar ao carregar imagem, segue sem
                    }
                }

                // Credenciais abaixo da imagem
                if (!string.IsNullOrWhiteSpace(ass.Credenciais))
                {
                    gfx.DrawString(ass.Credenciais, _fontPequena, XBrushes.Black,
                        new XRect(xBloco, y + imgH + 2, larguraBloco, 10), XStringFormats.TopCenter);
                }
            }

            y += imgH + 14;
            return y;
        }

        #endregion

        #region ZONA 6 — Rodapé

        private void DesenharRodape(XGraphics gfx, PdfPage page, DadosPdfResultado dados)
        {
            double yRodape = AlturaPagina - ReservaRodape + 10;

            // Linha separadora
            var penVerde = new XPen(_corVerde, 1);
            gfx.DrawLine(penVerde, MargemEsquerda, yRodape, LarguraPagina - MargemDireita, yRodape);
            yRodape += 10;

            // Texto 1: Laudo liberado...
            string textoLiberacao = $"Laudo liberado na \"Data do Exame\" e Impresso por ADMINISTRADOR DO SISTEMA em {dados.DataImpressao} às {dados.HoraImpressao} horas.";
            gfx.DrawString(textoLiberacao, _fontRodape, _brushCinzaEscuro,
                new XRect(MargemEsquerda, yRodape, AreaUtil, 11), XStringFormats.TopLeft);
            yRodape += 12;

            // Texto 2
            string textoResp = "As amostras enviadas para análises dos exames são de responsabilidade do Laboratório ou Convênio de origem.";
            gfx.DrawString(textoResp, _fontRodape, _brushCinzaEscuro,
                new XRect(MargemEsquerda, yRodape, AreaUtil, 11), XStringFormats.TopLeft);
            yRodape += 12;

            // Texto 3
            string textoPrazo = "Exames possuem datas PREVISTAS para resultado, todavia a data pode ser alterada de acordo com a necessidade aplicada nas análises.";
            gfx.DrawString(textoPrazo, _fontRodape, _brushCinzaEscuro,
                new XRect(MargemEsquerda, yRodape, AreaUtil, 11), XStringFormats.TopLeft);
            yRodape += 16;

            // Barra verde no fundo
            var brushBarraVerde = new XSolidBrush(_corBarraVerde);
            gfx.DrawRectangle(brushBarraVerde, MargemEsquerda, yRodape, AreaUtil, 18);

            // Texto PNCQ à esquerda (branco)
            string textoPncq = "Este Laboratório está inscrito no Programa Nacional de Controle de Qualidade (PNCQ)";
            gfx.DrawString(textoPncq, _fontRodapeBold, XBrushes.White,
                new XRect(MargemEsquerda + 4, yRodape + 4, AreaUtil - 100, 12), XStringFormats.TopLeft);

            // Página à direita (branco)
            int numeroPagina = 0;
            for (int i = 0; i < page.Owner.PageCount; i++)
            {
                if (page.Owner.Pages[i] == page)
                {
                    numeroPagina = i + 1;
                    break;
                }
            }
            gfx.DrawString($"Página Nº {numeroPagina}", _fontRodapeBold, XBrushes.White,
                new XRect(MargemEsquerda, yRodape + 4, AreaUtil - 4, 12), XStringFormats.TopRight);

            yRodape += 22;

            // Sistema LabWeb7 (centralizado abaixo)
            gfx.DrawString("Sistema LabWeb7 (Desde 2005) Ricardo Guilemond", _fontRodape, XBrushes.Gray,
                new XRect(MargemEsquerda, yRodape, AreaUtil, 9), XStringFormats.TopCenter);
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Agrupa itens por folha de exame. Mudança de folha detectada quando ContaExame.Substring(2,2) muda.
        /// </summary>
        private List<GrupoFolha> AgruparPorFolha(List<ItemPdfResultado> itens)
        {
            var grupos = new List<GrupoFolha>();
            if (itens.Count == 0)
                return grupos;

            string folhaAtual = "";
            GrupoFolha? grupoAtual = null;

            foreach (var item in itens)
            {
                // Detectar mudança de folha: posições 2-3 do ContaExame (índice 2, comprimento 2)
                string codigoFolha = item.ContaExame.Length >= 4
                    ? item.ContaExame.Substring(2, 2)
                    : "";

                if (codigoFolha != folhaAtual || grupoAtual == null)
                {
                    folhaAtual = codigoFolha;
                    grupoAtual = new GrupoFolha
                    {
                        NomeFolha = item.Folha,
                        Itens = []
                    };
                    grupos.Add(grupoAtual);
                }

                grupoAtual.Itens.Add(item);
            }

            return grupos;
        }

        private static int CalcularIdade(DateTime nascimento)
        {
            var hoje = DateTime.Today;
            int idade = hoje.Year - nascimento.Year;
            if (nascimento.Date > hoje.AddYears(-idade))
                idade--;
            return idade;
        }

        private static List<string> QuebrarTexto(string texto, int maxChars)
        {
            var linhas = new List<string>();
            if (string.IsNullOrWhiteSpace(texto))
                return linhas;

            // Primeiro quebrar por \n existentes
            var paragrafos = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var paragrafo in paragrafos)
            {
                if (paragrafo.Length <= maxChars)
                {
                    linhas.Add(paragrafo);
                }
                else
                {
                    // Quebrar em pedaços por palavras
                    var palavras = paragrafo.Split(' ');
                    var linhaAtual = new StringBuilder();
                    foreach (var palavra in palavras)
                    {
                        if (linhaAtual.Length + palavra.Length + 1 > maxChars && linhaAtual.Length > 0)
                        {
                            linhas.Add(linhaAtual.ToString());
                            linhaAtual.Clear();
                        }
                        if (linhaAtual.Length > 0)
                            linhaAtual.Append(' ');
                        linhaAtual.Append(palavra);
                    }
                    if (linhaAtual.Length > 0)
                        linhas.Add(linhaAtual.ToString());
                }
            }

            return linhas;
        }

        #endregion

        private class GrupoFolha
        {
            public string NomeFolha { get; set; } = "";
            public List<ItemPdfResultado> Itens { get; set; } = [];
        }
    }
    //..Kiro
}
