using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Processing;

namespace LabWebMvc.MVC.Areas.Utils
{
    public static class UImages
    {
        public static byte[] ResizeImage(byte[] imageData, int width, int height, string formatoImagem = "png")
        {
            using Image image = Image.Load(imageData);
            image.Mutate(x => x.Resize(width, height));

            using MemoryStream ms = new();

            formatoImagem = "." + formatoImagem;
            IImageEncoder format = FormatoImagem(formatoImagem);
            image.Save(ms, format);
            return ms.ToArray();
        }

        public static byte[] CropFaceFromImage(byte[] imageData, Rectangle section, string formatoImagem = "png")
        {
            using Image image = Image.Load(imageData);
            image.Mutate(x => x.Crop(section));

            using MemoryStream ms = new();

            formatoImagem = "." + formatoImagem;
            IImageEncoder format = FormatoImagem(formatoImagem);
            image.Save(ms, format);
            return ms.ToArray();
        }

        public static byte[] ConvertFileImageToByteArray(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string[] allowedExtensions = { ".png", ".gif", ".jpg", ".jpeg", ".bmp" };
                    string ext = Path.GetExtension(filePath).ToLower();

                    if (allowedExtensions.Contains(ext))
                    {
                        FileInfo fileInfo = new(filePath);
                        if (fileInfo.Length <= 2e+6) // Máximo de 2MB
                        {
                            return File.ReadAllBytes(filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();
                eventLog2.LogEventViewer($"Erro ao processar imagem: {ex.Message}, por favor considere máximo de 2MB, filePath: {filePath}", "wError");
            }
            return Array.Empty<byte>();
        }

        public static IImageEncoder FormatoImagem(string ext)
        {
            return ext.ToLower() switch
            {
                "png" => new PngEncoder(),
                "jpg" or "jpeg" => new JpegEncoder(),
                "bmp" => new BmpEncoder(),
                "gif" => new GifEncoder(),
                "tiff" => new TiffEncoder(),
                _ => new PngEncoder() // Padrão PNG
            };
        }

        public static byte[] RedimensionaImagem(byte[] imageData, string ext = "png", int width = 400, int height = 400)
        {
            using Image image = Image.Load(imageData);
            image.Mutate(x => x.Resize(width, height));

            using MemoryStream ms = new();
            image.Save(ms, FormatoImagem(ext));
            return ms.ToArray();
        }
    }//Fim
}