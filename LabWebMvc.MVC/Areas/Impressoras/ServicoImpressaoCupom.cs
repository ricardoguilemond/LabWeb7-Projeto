using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.Areas.Impressoras
{
    public class ServicoImpressaoCupom
    {
        private readonly string _conteudo;
        private readonly Db _db;
        private readonly IImpressoraCupom _impressora;

        public ServicoImpressaoCupom(string conteudo, Db db, IImpressoraCupom impressora)
        {
            _conteudo = conteudo;
            _db = db;
            _impressora = impressora;
        }
        public ResultadoImpressao Executar(string codigoExame = "0")
        {
            return _impressora.Imprimir(_conteudo, _db, codigoExame);
        }
    }
}
