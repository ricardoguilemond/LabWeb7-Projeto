using Amazon.S3;
using Amazon.S3.Model;
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

        /*
         * AWS (Amazon): Implementa armazenamento na Nuvem
         */

        public class S3StorageService : IStorageService
        {
            private readonly AmazonS3Client _s3Client;
            private readonly string _bucketName = "meu-bucket";

            public S3StorageService()
            {
                _s3Client = new AmazonS3Client("suaAccessKey", "suaSecretKey", Amazon.RegionEndpoint.USEast1);
            }

            public async Task<bool> SaveFileAsync(string fileName, byte[] data)
            {
                using MemoryStream stream = new(data);
                PutObjectRequest request = new()
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    InputStream = stream
                };
                PutObjectResponse response = await _s3Client.PutObjectAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }

            public async Task<byte[]?> GetFileAsync(string fileName)
            {
                GetObjectRequest request = new() { BucketName = _bucketName, Key = fileName };
                using GetObjectResponse response = await _s3Client.GetObjectAsync(request);
                using MemoryStream stream = new();
                await response.ResponseStream.CopyToAsync(stream);
                return stream.ToArray();
            }

            public async Task<bool> DeleteFileAsync(string fileName)
            {
                DeleteObjectRequest request = new() { BucketName = _bucketName, Key = fileName };
                DeleteObjectResponse response = await _s3Client.DeleteObjectAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
            }
        }
    }//Fim
}