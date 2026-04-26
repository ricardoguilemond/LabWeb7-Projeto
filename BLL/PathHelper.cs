using Microsoft.AspNetCore.Hosting;

namespace BLL
{
    public interface IPathHelper
    {
        string? GetPathTrue(string? pathInicial, string? nomeArquivo);
    }

    public class PathHelper : IPathHelper
    {
        private readonly IWebHostEnvironment _env;

        public PathHelper(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string? GetPathTrue(string? pathInicial, string? nomeArquivo)
        {
            string caminhoBase = string.IsNullOrWhiteSpace(pathInicial) ? _env.WebRootPath : pathInicial;

            if (!Directory.Exists(caminhoBase))
                return null;

            if (!string.IsNullOrWhiteSpace(nomeArquivo))
            {
                foreach (string item in Directory.GetDirectories(caminhoBase))
                {
                    string fullPath = Path.Combine(item, nomeArquivo);
                    if (File.Exists(fullPath))
                        return item + Path.DirectorySeparatorChar;
                }
            }
            return caminhoBase;
        }
    }
}