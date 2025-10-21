using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.Impressoras
{
    public interface IImpressoraCupom
    {
        ResultadoImpressao Imprimir(string conteudo, Db db, string codigoExame = "0");
    }
}
