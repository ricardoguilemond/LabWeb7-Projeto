using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace BLL
{
    public static class ConversoresPdf
    {
        public static MemoryStream ReadFileMemoryStream(string FileNamePath)
        {
            using FileStream file = new(FileNamePath, FileMode.Open, FileAccess.Read);
            MemoryStream ms = new();
            file.CopyTo(ms);
            return ms;
        }

        private static byte[] ConverteHtmlToByte(string FilePathHtml)
        {
            return File.ReadAllBytes(FilePathHtml);
        }

        //Retorna uma imagem convertida em Base64
        public static string ConverteArquivoImagemToBase64(string imageFilePath)
        {
            if (string.IsNullOrWhiteSpace(imageFilePath) || !File.Exists(imageFilePath))
            {
                throw new FileNotFoundException("Arquivo de imagem não encontrado.", imageFilePath);
            }
            try
            {
                using Image<Rgba32> image = Image.Load<Rgba32>(imageFilePath);
                using MemoryStream ms = new();
                image.SaveAsPng(ms); // Salva como PNG diretamente
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao converter a imagem: {ex.Message}", ex);
            }
        }

        //Retorna uma lista de imagens convertidas para Base64
        public static List<string> ConverteMultiplasImagensParaBase64(IEnumerable<string> caminhosImagens)
        {
            List<string> listaBase64 = [];

            foreach (string caminho in caminhosImagens)
            {
                if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
                    continue;

                try
                {
                    using Image<Rgba32> imagem = Image.Load<Rgba32>(caminho);
                    using MemoryStream ms = new();
                    imagem.SaveAsPng(ms);
                    string base64 = Convert.ToBase64String(ms.ToArray());
                    listaBase64.Add(base64);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Erro ao converter imagens: {ex.Message}", ex);
                }
            }
            return listaBase64;
        }

        //Converte imagem para URI
        public static string ConverteImagemParaDataUri(string caminhoImagem)
        {
            if (string.IsNullOrWhiteSpace(caminhoImagem) || !File.Exists(caminhoImagem))
                throw new FileNotFoundException("Imagem não encontrada.", caminhoImagem);

            try
            {
                using Image<Rgba32> imagem = Image.Load<Rgba32>(caminhoImagem);
                using MemoryStream ms = new();
                imagem.SaveAsPng(ms);
                string base64 = Convert.ToBase64String(ms.ToArray());
                return $"data:image/png;base64,{base64}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao gerar Data URI: {ex.Message}", ex);
            }
        }

        public static Image<Rgba32>? ConverteBase64ToImageSharpImage(string base64Image)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64Image);
                using MemoryStream ms = new(imageBytes);
                return Image.Load<Rgba32>(ms);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao converter Base64 em imagem: {ex.Message}", ex);
            }
        }

        public static string ConverteStringToBase64(string FilePath)
        {
            return Convert.ToBase64String(Encoding.Unicode.GetBytes(FilePath));
        }

        private static string ReadHtmlToText(string FilePath)
        {
            return File.ReadAllText(FilePath);
        }
    }
}