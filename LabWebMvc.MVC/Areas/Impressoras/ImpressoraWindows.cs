using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.Utils;
using System.Drawing;
using System.Drawing.Printing;
using global::LabWebMvc.MVC.Models;
using System.Runtime.Versioning;

namespace LabWebMvc.MVC.Areas.Impressoras
{
    public class ImpressoraWindows : IImpressoraCupom
    {

        [SupportedOSPlatform("windows")]
        public ResultadoImpressao Imprimir(string conteudo, Db db, string codigoExame = "0")
        {
            IEventLogHelper eventLogHelper = new EventLogHelper();

            var config = db.Configuracoes.FirstOrDefault(c => c.Id == 1);
            string? nomeImpressora = ObterImpressoraCupomAtiva(config);

            if (string.IsNullOrEmpty(nomeImpressora) || config == null)
            {
                string msg = "Configuração ou impressora não encontrada na tentativa de imprimir cupom.";
                eventLogHelper.LogEventViewer(msg, "wError");
                return new ResultadoImpressao { Sucesso = false, Mensagem = msg };
            }

            string pastaTemp = Path.GetTempPath();
            string nomeArquivo = $"Cupom-{codigoExame}-{Random.Shared.Next(10000)}.txt";
            string caminhoCompleto = Path.Combine(pastaTemp, nomeArquivo);
            File.WriteAllText(caminhoCompleto, conteudo);

            PrintDocument pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = nomeImpressora;

            if (!pd.PrinterSettings.IsValid)
            {
                string msg = $"Impressora '{nomeImpressora}' não encontrada ou inválida.";
                eventLogHelper.LogEventViewer(msg, "wError");
                return new ResultadoImpressao { Sucesso = false, Mensagem = msg };
            }

            Font fonte = new Font(
                string.IsNullOrWhiteSpace(config.FonteNome) ? "Consolas" : config.FonteNome.ToCapitalizeNotNull(),
                config.FonteTamanho > 0 ? config.FonteTamanho : 8,
                FontStyle.Bold
            );

            pd.DefaultPageSettings.PaperSize = new PaperSize("Custom",
                config.LarguraPapel > 0 ? config.LarguraPapel : 283,
                config.AlturaPapel > 0 ? config.AlturaPapel : 32767);

            pd.DefaultPageSettings.Margins = new Margins(
                config.MargemEsquerda > 0 ? config.MargemEsquerda : 5,
                config.MargemDireita > 0 ? config.MargemDireita : 5,
                config.MargemSuperior > 0 ? config.MargemSuperior : 5,
                config.MargemInferior > 0 ? config.MargemInferior : 5);

            pd.PrintPage += (sender, e) =>
            {
                float x = e.MarginBounds.Left;
                float y = e.MarginBounds.Top;
                if (e.Graphics == null)
                {
                    eventLogHelper.LogEventViewer("Graphics não disponível para impressão (ImpressoraWindows).", "wError");
                    throw new InvalidOperationException("Graphics não disponível para impressão.");
                }
                float linhaAltura = fonte.GetHeight(e.Graphics!);

                using var reader = new StringReader(conteudo);
                string? linha;
                while ((linha = reader.ReadLine()) != null)
                {
                    e.Graphics!.DrawString(linha, fonte, Brushes.Black, x, y);
                    y += linhaAltura;
                }
            };

            try
            {
                pd.Print();
                return new ResultadoImpressao { Sucesso = true, Mensagem = "Cupom impresso com sucesso." };
            }
            catch (Exception ex)
            {
                string msg = $"Erro ao tentar imprimir cupom: {ex.Message}";
                eventLogHelper.LogEventViewer(msg, "wError");
                return new ResultadoImpressao { Sucesso = false, Mensagem = msg };
            }
        }


        private string? ObterImpressoraCupomAtiva(Configuracoes? config)
        {
            if (config == null) return null;

            if (config.UsarImpressoraCupom1 == 1 && !string.IsNullOrWhiteSpace(config.ImpressoraCupom1))
                return config.ImpressoraCupom1;

            if (config.UsarImpressoraCupom2 == 1 && !string.IsNullOrWhiteSpace(config.ImpressoraCupom2))
                return config.ImpressoraCupom2;

            if (config.UsarImpressoraCupom3 == 1 && !string.IsNullOrWhiteSpace(config.ImpressoraCupom3))
                return config.ImpressoraCupom3;

            return null;
        }
    }
}


