using System.Text;
using System.Text.RegularExpressions;
using LabWebMvc.MVC.Areas.ServicosDatabase;
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

        // Referências de exames do cache (Dictionary: ContaExame → lista de itens)
        public Dictionary<string, List<ExameReferenciaItem>>? ReferenciasPorContaExame { get; set; }

        // Histórico evolutivo de valores por ContaExame (ex.: Glicose)
        public Dictionary<string, List<DadoHistoricoExame>>? HistoricoPorContaExame { get; set; }
    }

    public class DadoHistoricoExame
    {
        public DateTime DataExame { get; set; }
        public decimal Valor { get; set; }
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
        private const double ReservaRodape = 165;
        private const double LarguraPagina = 595.28;
        private const double AlturaPagina = 841.89;
        private const double AreaUtil = LarguraPagina - MargemEsquerda - MargemDireita;
        private const double LimiteY = AlturaPagina - ReservaRodape;

        // Fontes
        private readonly XFont _fontTitulo = new("Arial", 14, XFontStyle.Bold);
        private readonly XFont _fontSubtitulo = new("Arial", 10, XFontStyle.Regular);
        private readonly XFont _fontNormal = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontNormalBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontDadosPaciente = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontDadosPacienteBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontDadosLabel = new("Arial", 9, XFontStyle.Italic);
        private readonly XFont _fontCodigoExame = new("Arial", 12, XFontStyle.Bold);
        private readonly XFont _fontFolha = new("Arial", 14, XFontStyle.Bold);
        private readonly XFont _fontPrincipal = new("Arial", 10, XFontStyle.Bold);
        private readonly XFont _fontItem = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontItemNormal = new("Arial", 9, XFontStyle.Regular);
        // Fonte do valor do resultado: 40% maior que a fonte normal do item (9pt → ~12,6pt)
        private readonly XFont _fontItemResultado = new("Arial", 12.6, XFontStyle.Regular);
        private readonly XFont _fontLaudo = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontLaudoBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontRodape = new("Arial", 9, XFontStyle.Regular);
        private readonly XFont _fontRodapeBold = new("Arial", 9, XFontStyle.Bold);
        private readonly XFont _fontPequena = new("Arial", 8, XFontStyle.Regular);
        private readonly XFont _fontPequenaBold = new("Arial", 8, XFontStyle.Bold);

        // Cores
        private readonly XColor _corFaixaCinza = XColor.FromArgb(226, 226, 226); // #E2E2E2
        private readonly XColor _corVerde = XColor.FromArgb(0, 128, 0);
        private readonly XColor _corBarraVerde = XColor.FromArgb(0, 100, 0);
        private readonly XColor _corVermelha = XColor.FromArgb(220, 20, 60);
        private readonly XSolidBrush _brushCinzaEscuro = new(XColor.FromArgb(40, 40, 40)); // quase preto
        private readonly XSolidBrush _brushVermelho = new(XColor.FromArgb(220, 20, 60));

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
                // Pré-varredura: verifica se algum sub-item do grupo possui laudo ou gráfico
                var cacheAlturaLaudo = new Dictionary<string, double>();
                bool grupoTemConteudo = false;
                foreach (var itemPre in grupo.Itens)
                {
                    if (itemPre.EhPrincipal) continue;
                    double altLaudo = CalcularAlturaLaudo(itemPre, dados.ReferenciasPorContaExame);
                    cacheAlturaLaudo[itemPre.ContaExame] = altLaudo;
                    bool temGraf = dados.HistoricoPorContaExame != null
                        && dados.HistoricoPorContaExame.ContainsKey(itemPre.ContaExame);
                    if (altLaudo > 0 || temGraf)
                        grupoTemConteudo = true;
                }

                int indiceItem = 0;
                foreach (var item in grupo.Itens)
                {
                    // Verificar se precisa nova página
                    double alturaEstimada = item.EhPrincipal ? 24 : 16;
                    if (!item.EhPrincipal)
                    {
                        double altLaudoPre = cacheAlturaLaudo.TryGetValue(item.ContaExame, out var alp) ? alp : 0;
                        alturaEstimada += altLaudoPre;
                        bool possuiGrafico = dados.HistoricoPorContaExame != null
                            && dados.HistoricoPorContaExame.ContainsKey(item.ContaExame);
                        if (possuiGrafico)
                            alturaEstimada += 110;
                    }

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

                    y = DesenharItem(gfx, item, y, indiceItem, dados.ReferenciasPorContaExame, dados.HistoricoPorContaExame, grupoTemConteudo, cacheAlturaLaudo);
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

            // --- ZONA 2: Dados do Paciente e Exame (modelo 2004 — labels itálico, dados bold) ---
            //Feito pelo Kiro em 26/06/2025
            double colEsquerdaW = AreaUtil * 0.60;
            double colDireitaX = MargemEsquerda + colEsquerdaW + 10;
            double colDireitaW = AreaUtil - colEsquerdaW - 10;
            double yInicioZona2 = y;

            // Larguras fixas para alinhamento dos ":" (labels alinhados à direita no bloco)
            double labelWidthEsq = 115; // coluna esquerda — todos os ":" ficam na posição 115
            double labelWidthDir = 145; // coluna direita — labels mais à direita, próximos dos valores

            // === Coluna esquerda — labels em itálico alinhados à direita, valores em bold ===

            // Linha 1: ( código )  Nome: NOME DO PACIENTE
            // O "(código)" fica antes do label; label "Nome:" alinhado com os demais
            string codigoPaciente = $"( {dados.PacienteId} )";
            double codPacW = gfx.MeasureString(codigoPaciente, _fontDadosPaciente).Width + 4;
            gfx.DrawString(codigoPaciente, _fontDadosPaciente, XBrushes.Black,
                new XRect(MargemEsquerda, y, codPacW, 13), XStringFormats.TopLeft);
            gfx.DrawString("Nome:", _fontDadosLabel, XBrushes.Black,
                new XRect(MargemEsquerda + codPacW, y, labelWidthEsq - codPacW, 13), XStringFormats.TopRight);
            gfx.DrawString(dados.NomePaciente, _fontDadosPacienteBold, XBrushes.Black,
                new XRect(MargemEsquerda + labelWidthEsq + 2, y, colEsquerdaW - labelWidthEsq - 2, 13), XStringFormats.TopLeft);
            y += 14;

            // Linha 2: Data Nascimento: dd/MM/yyyy (XX anos), Sexo: X
            string sexoExibir = !string.IsNullOrWhiteSpace(dados.Sexo)
                ? dados.Sexo
                : InferirSexoPeloNome(dados.NomePaciente);

            string idadeTexto = "";
            if (dados.Nascimento.HasValue)
            {
                int idade = CalcularIdade(dados.Nascimento.Value);
                idadeTexto = $"{dados.Nascimento.Value:dd/MM/yyyy} ({idade} anos), Sexo: {sexoExibir}";
            }
            else
            {
                idadeTexto = !string.IsNullOrWhiteSpace(sexoExibir) ? $"Sexo: {sexoExibir}" : "";
            }
            gfx.DrawString("Data Nascimento:", _fontDadosLabel, XBrushes.Black,
                new XRect(MargemEsquerda, y, labelWidthEsq, 13), XStringFormats.TopRight);
            gfx.DrawString(idadeTexto, _fontDadosPacienteBold, XBrushes.Black,
                new XRect(MargemEsquerda + labelWidthEsq + 2, y, colEsquerdaW - labelWidthEsq - 2, 13), XStringFormats.TopLeft);
            y += 14;

            // Linha 3: Médico Solicitante: Dr(a). NOME
            gfx.DrawString("Médico Solicitante:", _fontDadosLabel, XBrushes.Black,
                new XRect(MargemEsquerda, y, labelWidthEsq, 13), XStringFormats.TopRight);
            gfx.DrawString($"Dr(a). {dados.NomeMedico}", _fontDadosPacienteBold, XBrushes.Black,
                new XRect(MargemEsquerda + labelWidthEsq + 2, y, colEsquerdaW - labelWidthEsq - 2, 13), XStringFormats.TopLeft);
            y += 14;

            // Linha 4: Convênio Origem: SIGLA - SEQ / NOME
            string convenioValor = $"{dados.SiglaInstituicao} - {dados.SequencialFormatado} / {dados.NomeInstituicao}";
            gfx.DrawString("Convênio Origem:", _fontDadosLabel, XBrushes.Black,
                new XRect(MargemEsquerda, y, labelWidthEsq, 13), XStringFormats.TopRight);
            gfx.DrawString(convenioValor, _fontDadosPacienteBold, XBrushes.Black,
                new XRect(MargemEsquerda + labelWidthEsq + 2, y, colEsquerdaW - labelWidthEsq - 2, 13), XStringFormats.TopLeft);
            y += 14;

            // Linha 5: Procedência: CIDADE / UF
            if (!string.IsNullOrWhiteSpace(dados.Procedencia))
            {
                gfx.DrawString("Procedência:", _fontDadosLabel, XBrushes.Black,
                    new XRect(MargemEsquerda, y, labelWidthEsq, 13), XStringFormats.TopRight);
                gfx.DrawString(dados.Procedencia, _fontDadosPacienteBold, XBrushes.Black,
                    new XRect(MargemEsquerda + labelWidthEsq + 2, y, colEsquerdaW - labelWidthEsq - 2, 13), XStringFormats.TopLeft);
                y += 14;
            }

            // === Coluna direita — labels em itálico alinhados à direita, valores à direita ===
            double yDir = yInicioZona2;

            // Linha 1: Código do Exame: XXXXX (número em fonte grande alinhado totalmente à direita)
            gfx.DrawString("Código do Exame:", _fontDadosLabel, XBrushes.Black,
                new XRect(colDireitaX, yDir, labelWidthDir, 13), XStringFormats.TopRight);
            gfx.DrawString(dados.ExameId.ToString(), _fontCodigoExame, XBrushes.Black,
                new XRect(colDireitaX, yDir - 2, colDireitaW, 15), XStringFormats.TopRight);
            yDir += 16;

            // Linha 2: Código de Coleta
            if (!string.IsNullOrWhiteSpace(dados.ControleApoioFormatado))
            {
                gfx.DrawString("Código de Coleta:", _fontDadosLabel, XBrushes.Black,
                    new XRect(colDireitaX, yDir, labelWidthDir, 13), XStringFormats.TopRight);
                gfx.DrawString(dados.ControleApoioFormatado, _fontDadosPaciente, XBrushes.Black,
                    new XRect(colDireitaX + labelWidthDir + 2, yDir, colDireitaW - labelWidthDir - 2, 13), XStringFormats.TopRight);
                yDir += 14;
            }

            // Linha 3: Data do Exame/Coleta
            if (!string.IsNullOrWhiteSpace(dados.DataExameColeta))
            {
                gfx.DrawString("Data do Exame/Coleta:", _fontDadosLabel, XBrushes.Black,
                    new XRect(colDireitaX, yDir, labelWidthDir, 13), XStringFormats.TopRight);
                gfx.DrawString(dados.DataExameColeta, _fontDadosPaciente, XBrushes.Black,
                    new XRect(colDireitaX + labelWidthDir + 2, yDir, colDireitaW - labelWidthDir - 2, 13), XStringFormats.TopRight);
                yDir += 14;
            }

            // Linha 4: Laudo Liberado em
            if (!string.IsNullOrWhiteSpace(dados.DataLaudoLiberado))
            {
                gfx.DrawString("Laudo Liberado em:", _fontDadosLabel, XBrushes.Black,
                    new XRect(colDireitaX, yDir, labelWidthDir, 13), XStringFormats.TopRight);
                gfx.DrawString(dados.DataLaudoLiberado, _fontDadosPaciente, XBrushes.Black,
                    new XRect(colDireitaX + labelWidthDir + 2, yDir, colDireitaW - labelWidthDir - 2, 13), XStringFormats.TopRight);
                yDir += 14;
            }

            // Linha 5: Laudo Impresso em
            gfx.DrawString("Laudo Impresso em:", _fontDadosLabel, XBrushes.Black,
                new XRect(colDireitaX, yDir, labelWidthDir, 13), XStringFormats.TopRight);
            gfx.DrawString(dados.DataImpressao, _fontDadosPaciente, XBrushes.Black,
                new XRect(colDireitaX + labelWidthDir + 2, yDir, colDireitaW - labelWidthDir - 2, 13), XStringFormats.TopRight);
            yDir += 14;
            //..Kiro

            // Usar o maior Y entre as duas colunas
            y = Math.Max(y, yDir) + 12;

            return y;
        }

        #endregion

        #region ZONA 3 — Título da Folha de Exame

        private double DesenharTituloFolha(XGraphics gfx, string nomeFolha, double y)
        {
            // Moldura completa (caixa) em volta do título da folha — conectada à linha horizontal acima
            double alturaBox = 24;
            var penCaixa = new XPen(_corVerde, 1.5);
            gfx.DrawRectangle(penCaixa, MargemEsquerda, y - 2, AreaUtil, alturaBox);

            // Nome da folha em font bold centralizado dentro da caixa (reduzido 20%: de 18pt para 14pt)
            gfx.DrawString(nomeFolha, _fontFolha, XBrushes.Black,
                new XRect(MargemEsquerda, y, AreaUtil, alturaBox - 4), XStringFormats.Center);
            y += alturaBox + 4;

            // Sub-cabeçalho de colunas — alinhados com as posições reais dos dados
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

        private double DesenharItem(XGraphics gfx, ItemPdfResultado item, double y, int indice,
            Dictionary<string, List<ExameReferenciaItem>>? referencias,
            Dictionary<string, List<DadoHistoricoExame>>? historicoPorContaExame,
            bool grupoTemConteudo,
            Dictionary<string, double> cacheAlturaLaudo)
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
                // Fundo do item: sempre 15px (apenas a linha do item, não o laudo/gráfico)
                // Se o grupo tem conteúdo (laudo ou gráfico): todos os itens recebem fundo cinza
                // Se não tem conteúdo: listras alternadas (índice par)
                bool escurecer = grupoTemConteudo || (indice % 2 == 0);

                if (escurecer)
                {
                    var brush = new XSolidBrush(_corFaixaCinza);
                    gfx.DrawRectangle(brush, MargemEsquerda, y - 1, AreaUtil, 15);
                }

                // Descrição indentada 36px (maiúscula conforme Delphi)
                double xDesc = MargemEsquerda + 36;
                gfx.DrawString(item.Descricao.ToUpper(), _fontItem, XBrushes.Black,
                    new XRect(xDesc, y, 200, 12), XStringFormats.TopLeft);

                // Resultado + Unidade no centro (~300pt) — fonte 40% maior
                string resultadoUnidade = item.Resultado;
                if (!string.IsNullOrWhiteSpace(item.UnidadeMedida))
                    resultadoUnidade += "  " + item.UnidadeMedida;
                gfx.DrawString(resultadoUnidade, _fontItemResultado, XBrushes.Black,
                    new XRect(300, y, 150, 14), XStringFormats.TopLeft);

                // Referência à direita (~460pt)
                double xReferencia = 460;
                double larguraReferencia = MargemEsquerda + AreaUtil - xReferencia;
                gfx.DrawString(item.Referencia, _fontItemNormal, _brushCinzaEscuro,
                    new XRect(xReferencia, y, larguraReferencia, 12), XStringFormats.TopLeft);

                y += 16;

                // Laudo fixo (exclusivo de ExameReferencia)
                y = DesenharLaudoFixo(gfx, item, y, referencias);

                // Gráfico evolutivo (se houver histórico para este ContaExame)
                if (historicoPorContaExame != null
                    && historicoPorContaExame.TryGetValue(item.ContaExame, out var historicoGlicose)
                    && historicoGlicose.Count >= 2)
                {
                    y = DesenharGraficoEvolucao(gfx, historicoGlicose, item.Descricao, item.Referencia, y);
                }
            }

            return y;
        }

        private double DesenharGraficoEvolucao(XGraphics gfx, List<DadoHistoricoExame> historico, string descricaoExame, string referencia, double y)
        {
            const double chartHeight = 100;
            const double marginLeftValues = 40;
            const double marginBottomDates = 24;
            const double marginTopTitle = 18;

            double chartTop = y + 4;
            double chartBottom = chartTop + chartHeight;
            double chartLeft = MargemEsquerda + 36;
            double chartRight = MargemEsquerda + AreaUtil - 10;

            double plotLeft = chartLeft + marginLeftValues;
            double plotRight = chartRight - 5;
            double plotTop = chartTop + marginTopTitle;
            double plotBottom = chartBottom - marginBottomDates;

            // Título
            string titulo = $"Evolução da {descricaoExame.ToUpper()} — últimos exames";
            gfx.DrawString(titulo, _fontNormalBold, XBrushes.Black,
                new XRect(chartLeft, chartTop, plotRight - chartLeft, 14), XStringFormats.TopLeft);

            // Limites
            decimal minValor = historico.Min(h => h.Valor);
            decimal maxValor = historico.Max(h => h.Valor);
            if (minValor == maxValor)
            {
                minValor -= 5;
                maxValor += 5;
            }
            decimal margemValor = (maxValor - minValor) * 0.1m;
            minValor -= margemValor;
            maxValor += margemValor;

            DateTime minData = historico.Min(h => h.DataExame);
            DateTime maxData = historico.Max(h => h.DataExame);
            if (minData == maxData)
            {
                minData = minData.AddDays(-1);
                maxData = maxData.AddDays(1);
            }
            long ticksRange = maxData.Ticks - minData.Ticks;

            // Eixos
            var penEixo = new XPen(XColors.DarkGray, 0.8);
            gfx.DrawLine(penEixo, plotLeft, plotTop, plotLeft, plotBottom);
            gfx.DrawLine(penEixo, plotLeft, plotBottom, plotRight, plotBottom);

            var penLinha = new XPen(_corVerde, 1.2);
            var penPonto = new XPen(_corVerde, 1.2);
            var brushPonto = new XSolidBrush(_corVerde);
            var penPontoFora = new XPen(_corVermelha, 1.2);
            var brushPontoFora = new XSolidBrush(_corVermelha);

            // Faixa de referência (se conseguir extrair do texto)
            var (refMin, refMax) = ExtrairFaixaReferencia(referencia);

            double MapX(DateTime data)
            {
                double ratio = (double)((data.Ticks - minData.Ticks) / (double)ticksRange);
                return plotLeft + ratio * (plotRight - plotLeft);
            }

            double MapY(decimal valor)
            {
                double ratio = (double)((valor - minValor) / (maxValor - minValor));
                return plotBottom - ratio * (plotBottom - plotTop);
            }

            bool EstaForaDaReferencia(decimal valor)
            {
                if (refMin.HasValue && valor < refMin.Value) return true;
                if (refMax.HasValue && valor > refMax.Value) return true;
                return false;
            }

            // Desenhar pontos e conectá-los (dados já estão ordenados crescente por data)
            var pontos = historico.Select(h => new { X = MapX(h.DataExame), Y = MapY(h.Valor), h.Valor, h.DataExame }).ToList();

            for (int i = 0; i < pontos.Count; i++)
            {
                // Linha para o próximo ponto
                if (i < pontos.Count - 1)
                    gfx.DrawLine(penLinha, pontos[i].X, pontos[i].Y, pontos[i + 1].X, pontos[i + 1].Y);

                bool fora = EstaForaDaReferencia(pontos[i].Valor);
                var penAtual = fora ? penPontoFora : penPonto;
                var brushAtual = fora ? brushPontoFora : brushPonto;
                var fonteData = fora ? _fontPequenaBold : _fontPequena;

                // Ponto (marcador maior: raio externo 4, interno 2.5)
                gfx.DrawEllipse(penAtual, pontos[i].X - 4, pontos[i].Y - 4, 8, 8);
                gfx.DrawEllipse(brushAtual, pontos[i].X - 2.5, pontos[i].Y - 2.5, 5, 5);

                // Valor do ponto acima da interseção
                string labelValor = pontos[i].Valor.ToString("G", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
                gfx.DrawString(labelValor, _fontPequena, _brushCinzaEscuro,
                    new XRect(pontos[i].X - 18, pontos[i].Y - 16, 36, 12), XStringFormats.TopCenter);

                // Rótulo da data no eixo X (abaixo) — negrito quando fora da referência
                string labelData = pontos[i].DataExame.ToString("dd/MM/yy");
                gfx.DrawString(labelData, fonteData, _brushCinzaEscuro,
                    new XRect(pontos[i].X - 20, plotBottom + 2, 40, 12), XStringFormats.TopCenter);
            }

            return chartBottom + 6;
        }

        /// <summary>
        /// Extrai a faixa numérica de referência a partir do texto livre da referência.
        /// Entende padrões como "3,6 a 7,7", "menor 170,0" e "maior que 199,0".
        /// </summary>
        private static (decimal? Min, decimal? Max) ExtrairFaixaReferencia(string referencia)
        {
            if (string.IsNullOrWhiteSpace(referencia))
                return (null, null);

            var matches = System.Text.RegularExpressions.Regex.Matches(referencia, @"\d+(?:[.,]\d+)?");
            if (matches.Count == 0)
                return (null, null);

            var valores = matches
                .Select(m => m.Value.Replace(".", "").Replace(",", "."))
                .Select(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            if (valores.Count == 0)
                return (null, null);

            bool temMaior = referencia.Contains("maior", System.StringComparison.OrdinalIgnoreCase);
            bool temMenor = referencia.Contains("menor", System.StringComparison.OrdinalIgnoreCase);

            if (valores.Count >= 2)
            {
                decimal v1 = valores[0];
                decimal v2 = valores[1];
                return (Math.Min(v1, v2), Math.Max(v1, v2));
            }

            if (temMaior)
                return (valores[0], null);
            if (temMenor)
                return (null, valores[0]);

            return (null, null);
        }

        /// <summary>
        /// Calcula a altura total do laudo (em pontos) sem renderizar.
        /// Fonte exclusiva: ExameReferencia (cache).
        /// </summary>
        private double CalcularAlturaLaudo(ItemPdfResultado item,
            Dictionary<string, List<ExameReferenciaItem>>? referencias)
        {
            if (string.IsNullOrWhiteSpace(item.ContaExame))
                return 0;

            if (referencias != null && referencias.TryGetValue(item.ContaExame, out var listaRefs))
            {
                double altura = 0;
                foreach (var refItem in listaRefs)
                {
                    string conteudo = ProcessarConteudoBinario(refItem.ConteudoBinario, refItem.FormatoOrigem);
                    if (!string.IsNullOrWhiteSpace(conteudo))
                    {
                        var linhas = QuebrarTexto(conteudo, 110);
                        int naoVazias = linhas.Count(l => !string.IsNullOrWhiteSpace(l));
                        altura += (naoVazias * 11) + 3;
                    }
                }
                return altura;
            }

            return 0;
        }

        //Feito pelo Kiro em 11/07/2025
        private double DesenharLaudoFixo(XGraphics gfx, ItemPdfResultado item, double y,
            Dictionary<string, List<ExameReferenciaItem>>? referencias)
        {
            if (string.IsNullOrWhiteSpace(item.ContaExame))
                return y;

            // Fonte exclusiva: ExameReferencia (cache)
            if (referencias != null && referencias.TryGetValue(item.ContaExame, out var listaRefs))
            {
                foreach (var refItem in listaRefs)
                {
                    string conteudo = ProcessarConteudoBinario(refItem.ConteudoBinario, refItem.FormatoOrigem);
                    if (!string.IsNullOrWhiteSpace(conteudo))
                    {
                        // AlinhaLaudo: 0=Esquerda (padrão da tela), 1=Direita
                        var formatoLaudo = refItem.AlinhaLaudo == 1
                            ? XStringFormats.TopRight
                            : XStringFormats.TopLeft;

                        y = RenderizarTextoLaudo(gfx, conteudo, y, formatoLaudo);
                    }
                }
            }

            return y;
        }

        private double RenderizarTextoLaudo(XGraphics gfx, string conteudo, double y, XStringFormat formato)
        {
            double maxLarguraLaudo = AreaUtil - 40;
            var linhas = QuebrarTexto(conteudo, 110);

            foreach (var linha in linhas)
            {
                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                if (y + 11 > LimiteY)
                    break;

                // Verificar se a linha contém marcadores de bold «B»...«/B»
                if (linha.Contains("«B»"))
                {
                    // Renderizar segmentos com bold intercalado
                    RenderizarLinhaComBold(gfx, linha, y, maxLarguraLaudo, formato);
                }
                else
                {
                    gfx.DrawString(linha, _fontLaudo, _brushCinzaEscuro,
                        new XRect(MargemEsquerda + 40, y, maxLarguraLaudo, 11), formato);
                }
                y += 11;
            }
            y += 3;

            return y;
        }

        /// <summary>
        /// Renderiza uma linha que contém segmentos bold (marcados com «B»...«/B»).
        /// Suporta bold parcial com alinhamento à esquerda e à direita.
        /// </summary>
        private void RenderizarLinhaComBold(XGraphics gfx, string linha, double y, double maxLargura, XStringFormat formato)
        {
            // Separar em segmentos alternando normal/bold
            var segmentos = Regex.Split(linha, @"(«B»|«/B»)");
            var partes = new List<(string texto, bool bold)>();
            bool emBold = false;

            foreach (var seg in segmentos)
            {
                if (seg == "«B»") { emBold = true; continue; }
                if (seg == "«/B»") { emBold = false; continue; }
                if (string.IsNullOrEmpty(seg)) continue;
                partes.Add((seg, emBold));
            }

            if (partes.Count == 0) return;

            double xBase = MargemEsquerda + 40;

            // XStringFormats.TopRight cria uma nova instância a cada acesso, portanto
            // a comparação por referência falharia silenciosamente. Usamos a propriedade
            // Alignment (Far = direita) para decidir o fluxo de renderização.
            if (formato.Alignment == XStringAlignment.Far)
            {
                // Alinhamento direita: calcular posição de cada segmento de trás para frente
                double xFim = xBase + maxLargura;
                // Percorrer de trás para frente para posicionar à direita
                for (int i = partes.Count - 1; i >= 0; i--)
                {
                    var fonte = partes[i].bold ? _fontLaudoBold : _fontLaudo;
                    double largSeg = gfx.MeasureString(partes[i].texto, fonte).Width;
                    xFim -= largSeg;
                    gfx.DrawString(partes[i].texto, fonte, _brushCinzaEscuro,
                        new XRect(xFim, y, largSeg + 2, 11), XStringFormats.TopLeft);
                }
            }
            else
            {
                // Alinhamento esquerda: renderizar segmento por segmento da esquerda para direita
                double xPos = xBase;
                foreach (var (texto, bold) in partes)
                {
                    var fonte = bold ? _fontLaudoBold : _fontLaudo;
                    gfx.DrawString(texto, fonte, _brushCinzaEscuro,
                        new XRect(xPos, y, maxLargura - (xPos - xBase), 11), XStringFormats.TopLeft);
                    xPos += gfx.MeasureString(texto, fonte).Width;
                }
            }
        }

        private static string ProcessarConteudoBinario(byte[] binario, string formato)
        {
            if (binario == null || binario.Length == 0) return "";

            if (formato == "RTF")
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                string conteudo = Encoding.GetEncoding(1252).GetString(binario);

                // Decodificar \'XX
                conteudo = Regex.Replace(conteudo, @"\\'([0-9a-fA-F]{2})", match =>
                {
                    byte b = Convert.ToByte(match.Groups[1].Value, 16);
                    return Encoding.GetEncoding(1252).GetString(new[] { b });
                });

                // Strip RTF tags
                conteudo = Regex.Replace(conteudo, @"\{\\[^}]*\}", "");
                conteudo = Regex.Replace(conteudo, @"\\par\b\s?", "\n");
                conteudo = Regex.Replace(conteudo, @"\\[a-z]+\d*\s?", "");
                conteudo = Regex.Replace(conteudo, @"[{}]", "");
                return conteudo.Trim();
            }
            else if (formato == "HTML")
            {
                // Strip HTML preservando bold como marcador especial «B»texto«/B»
                string html = Encoding.UTF8.GetString(binario);

                // Preservar bold: <strong>texto</strong> ou <b>texto</b> → «B»texto«/B»
                html = Regex.Replace(html, @"<strong>|<b>", "«B»", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"</strong>|</b>", "«/B»", RegexOptions.IgnoreCase);

                // Converter <br>, <br/>, <br /> em quebra de linha
                html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

                // Converter </p>, </div>, </li> em quebra de linha
                html = Regex.Replace(html, @"</p>|</div>|</li>", "\n", RegexOptions.IgnoreCase);

                // Remover todas as demais tags HTML
                html = Regex.Replace(html, @"<[^>]+>", "");

                // Decodificar entidades HTML comuns
                html = html.Replace("&nbsp;", " ")
                           .Replace("&amp;", "&")
                           .Replace("&lt;", "<")
                           .Replace("&gt;", ">")
                           .Replace("&quot;", "\"");

                return html.Trim();
            }

            return Encoding.UTF8.GetString(binario);
        }
        //..Kiro

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
            yRodape = DesenharTextoComQuebraAutomatica(gfx, textoLiberacao, _fontRodape, _brushCinzaEscuro, MargemEsquerda, yRodape, AreaUtil, 11);

            // Texto 2
            string textoResp = "As amostras enviadas para análises dos exames são de responsabilidade do Laboratório ou Convênio de origem.";
            yRodape = DesenharTextoComQuebraAutomatica(gfx, textoResp, _fontRodape, _brushCinzaEscuro, MargemEsquerda, yRodape, AreaUtil, 11);

            // Texto 3
            string textoPrazo = "Exames possuem datas PREVISTAS para resultado, todavia a data pode ser alterada de acordo com a necessidade aplicada nas análises.";
            yRodape = DesenharTextoComQuebraAutomatica(gfx, textoPrazo, _fontRodape, _brushCinzaEscuro, MargemEsquerda, yRodape, AreaUtil, 11);

            yRodape += 4;

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

        /// <summary>
        /// Desenha um texto com quebra automática de linha quando excede a largura disponível.
        /// Mede a largura real do texto usando XGraphics.MeasureString e quebra por palavras.
        /// Retorna o Y atualizado após todas as linhas desenhadas.
        /// </summary>
        private double DesenharTextoComQuebraAutomatica(XGraphics gfx, string texto, XFont font, XSolidBrush brush,
            double x, double y, double larguraMaxima, double alturaLinha)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return y + alturaLinha;

            var palavras = texto.Split(' ');
            var linhaAtual = new StringBuilder();

            foreach (var palavra in palavras)
            {
                string teste = linhaAtual.Length > 0
                    ? linhaAtual + " " + palavra
                    : palavra;

                var tamanho = gfx.MeasureString(teste, font);

                if (tamanho.Width > larguraMaxima && linhaAtual.Length > 0)
                {
                    // Imprimir a linha atual e começar nova
                    gfx.DrawString(linhaAtual.ToString(), font, brush,
                        new XRect(x, y, larguraMaxima, alturaLinha), XStringFormats.TopLeft);
                    y += alturaLinha;
                    linhaAtual.Clear();
                    linhaAtual.Append(palavra);
                }
                else
                {
                    if (linhaAtual.Length > 0)
                        linhaAtual.Append(' ');
                    linhaAtual.Append(palavra);
                }
            }

            // Última linha
            if (linhaAtual.Length > 0)
            {
                gfx.DrawString(linhaAtual.ToString(), font, brush,
                    new XRect(x, y, larguraMaxima, alturaLinha), XStringFormats.TopLeft);
                y += alturaLinha;
            }

            return y;
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

        //Feito pelo Kiro em 26/06/2025
        /// <summary>
        /// Infere o sexo (M/F) a partir da terminação do primeiro nome.
        /// Baseado no algoritmo do sistema Delphi (FGlobal.NomeSexo).
        /// </summary>
        private static string InferirSexoPeloNome(string nomeCompleto)
        {
            if (string.IsNullOrWhiteSpace(nomeCompleto))
                return "";

            // Pegar apenas o primeiro nome
            string primeiroNome = nomeCompleto.Trim().Split(' ')[0].ToUpper();
            if (string.IsNullOrEmpty(primeiroNome))
                return "";

            // Terminais masculinos (verificar do mais longo para o mais curto)
            string[] masculino = { "NOEL", "NUEL", "ARD", "MIR", "ELY", "AEL", "AUL", "ARK", "IUS",
                "AN", "ON", "IN", "IM", "EU", "UE", "EY", "ME", "EL", "OS", "OR",
                "ES", "EZ", "UR", "US", "VI", "PE", "AS", "RE", "EB", "AH", "IZ",
                "IS", "RI", "ID", "UA", "AL",
                "O", "C", "K", "U" };

            // Terminais femininos (verificar do mais longo para o mais curto)
            string[] feminino = { "QUEL", "CHEL", "EIA", "SSA",
                "LY", "EN", "LE", "TE", "AS", "NE", "MI", "EA", "ÊS", "CE", "GE",
                "A" };

            // Verificar masculino primeiro
            foreach (var term in masculino)
            {
                if (primeiroNome.EndsWith(term))
                    return "M";
            }

            // Verificar feminino
            foreach (var term in feminino)
            {
                if (primeiroNome.EndsWith(term))
                    return "F";
            }

            return "";
        }
        //..Kiro

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
