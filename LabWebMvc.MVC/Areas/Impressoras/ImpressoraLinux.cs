using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.Impressoras
{
    public class ImpressoraLinux : IImpressoraCupom
    {
        public ResultadoImpressao Imprimir(string conteudo, Db db, string codigoExame = "0")
        {
            // Lógica específica para Linux (ex: usando CUPS, comandos shell, etc.)
            Console.WriteLine("Imprimindo no Linux: " + conteudo);
            return new ResultadoImpressao { Sucesso = true, Mensagem = "Impressão simulada no Linux." };    
        }
    }
}
