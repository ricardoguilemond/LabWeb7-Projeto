using System.Runtime.InteropServices;

namespace ExtensionsMethods.Storages
{
    public class LocalStorageService : IStorageService
    {
        /*  Exemplo de uso:
         *                  public byte[]? LerArquivo(string nomeArquivo)
         *                  {
         *                      string path = Path.Combine(_basePath, nomeArquivo);
         *                      if (File.Exists(path))
         *                      {
         *                          return File.ReadAllBytes(path);
         *                      }
         *                      return null;
         *                  }
         *
         */

        //Lê da pasta "images" do Windows e Linux
        private readonly string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "images");

        //Caso precise definir manualmente um caminho diferente, então verificar com RuntimeInformation:
        private readonly string _basePathInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\Temp\Uploads" : "/home/seuusuario/uploads";

        public async Task<bool> SaveFileAsync(string fileName, byte[] data)
        {
            try
            {
                string path = Path.Combine(_basePath, fileName);
                await File.WriteAllBytesAsync(path, data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]?> GetFileAsync(string fileName)
        {
            string path = Path.Combine(_basePath, fileName);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
        }

        public Task<bool> DeleteFileAsync(string fileName)
        {
            string path = Path.Combine(_basePath, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        /* Feito pelo Qoder em 22/08/2026 — removida a classe de exemplo S3StorageService (credenciais placeholder e dependência
         * AWSSDK.S3 sem uso em produção). Quando houver requisito real de storage em nuvem, implementar um serviço dedicado
         * por trás de IStorageService com credenciais via variável de ambiente (nunca em código).
         */
    }//Fim
}