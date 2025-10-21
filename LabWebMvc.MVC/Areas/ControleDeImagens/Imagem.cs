using Azure.Storage.Blobs;
using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LabWebMvc.MVC.Areas.ControleDeImagens
{
    public class Imagem
    {
        private readonly Db _db;
        private readonly IWebHostEnvironment _env;
        private readonly IEventLogHelper _eventLog;

        public Imagem(IWebHostEnvironment env, IConnectionService connectionService, IEventLogHelper eventLogHelper)
        {
            _env = env;

            var optionsBuilder = new DbContextOptionsBuilder<Db>().UseNpgsql(connectionService.GetConnectionString());
            _db = new Db(optionsBuilder.Options, connectionService, eventLogHelper);
            _eventLog = eventLogHelper;
        }

        /*
         * Quando a imagem estiver em diretórios de arquivos estáticos
         * Ou se forem lidos diretamente de um servidor Web, a partir do root
         * USO no Controller:
         *
         *              Declarar no construtor:
         *              private readonly Imagem _imagem;
         *              public Imagem(Imagem imagem)
         *              {
         *                 _imagem = imagem;
         *              }
         *
         *              Exemplo de forma de uso:
         *              var bytes = _imagem.ObterImagem("imagens", "foto.jpg");
         *              if (bytes == null) return NotFound();
         *              return File(bytes, "image/jpeg");
         */

        public byte[]? ObterImagem(string subDiretorio, string nomeArquivo)
        {
            if (!string.IsNullOrEmpty(subDiretorio) && !string.IsNullOrEmpty(nomeArquivo))
            {
                string path = Path.Combine(_env.WebRootPath, subDiretorio, nomeArquivo);   //imagem lida de um endpoint

                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
            }
            return null;
        }

        /*
         * Ideal para obter imagens fora do servidor
         */

        public async Task<byte[]?> ObterImagemBlobAsync(string caminhoArquivo)
        {
            BlobClient blobClient = new("suaConnectionString", "seuContainer", caminhoArquivo);

            if (await blobClient.ExistsAsync())
            {
                using MemoryStream ms = new();
                await blobClient.DownloadToAsync(ms);
                return ms.ToArray();
            }
            return null;
        }

        /* Obter o caminho verdadeiro/obrigatório de onde o arquivo vem.
         * Recupera o caminho completo a partir do caminho obrigatório inicial enviado.
         */

        public string? GetPathTrue(string? subDiretorio, string nomeArquivo)
        {
            string basePath = string.IsNullOrWhiteSpace(subDiretorio)
                ? _env.WebRootPath
                : Path.Combine(_env.WebRootPath, subDiretorio);

            if (!Directory.Exists(basePath))
                return null;

            string? pathEncontrado = Directory
                .GetDirectories(basePath)
                .FirstOrDefault(dir => File.Exists(Path.Combine(dir, nomeArquivo)));

            return pathEncontrado ?? basePath;
        }
    }//Fim
}