using ExtensionsMethods.ParametrosGenericos;
using LabWebMvc.MVC.Areas.Validations;
using Newtonsoft.Json;
using System.Text;

namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public class CriptoDecriptoBase
    {
        public readonly string gatewayUrl = "http://localhost:10000";   //link de leitura do JSON recuperado de um serviço no Servidor
        public readonly HttpClient cliente = new();

        /* Método: Executa<T>
         * Executa métodos alternativos vindos de outros servidores ou solutions, trafegando com criptografia ponta-a-ponta
         * PRECISA SER TESTADO E MELHOR IMPLEMENTADO
         */

        public async Task<T?> Executa<T>(string servidor, string servico, string metodo, ParametrosGenericos parametros)
        {
            // GatewayURL/SERVIDOR/Generated/SERVICO.smartsvc/json/METODO
            string stringContent = JsonConvert.SerializeObject(parametros);

            #region criptografia dos parametros para AES

            /*
               Exemplo de uma chave AES: (colocada no Web.Config ou App.Config de um serviço)
               <add key="KeyAES" value="k7r@E&UHC0nn7ds0" />  <!-- não é uma chave verdadeira, apenas um exemplo qualquer -->
               E deve ser recuperada aqui no código desta forma:
              var chaveAES = ConfigurationManager.AppSettings["KeyAES"]; //Chave de criptografia AES do App.Config do Serviço
             */
            /* Estou usando uma chave qualquer que nem tem valor, apenas como exemplo,
             * quando vier a correta, todos os dados salvos gerados por ela vão se perder,
             * porque a chave é o único recurso de criptografia e descriptografia possível de identificação!
             */
            string chaveAES = "k7r@E&UHC0nn7ds0"; //ConfigurationManager.AppSettings["KeyAES"]; //Chave de criptografia AES do App.Config do Serviço
            bool criptografar = Convert.ToBoolean(Utils.VariavelAppJsonSettings("LigaCriptografia"));
            if (criptografar)
            {
                stringContent = CriptoDecripto.Criptografa_StringToString(stringContent, chaveAES, chaveAES);
            }

            #endregion criptografia dos parametros para AES

            StringContent content = new(stringContent, Encoding.UTF8, "application/json");
            // GatewayURL/SERVIDOR/Generated/SERVICO.smartsvc/json/METODO
            HttpResponseMessage response = await cliente.PostAsync(gatewayUrl + @"/" + servidor + @"/Generated/" + servico + @".smartsvc/json/" + metodo, content);

            string responseString = await response.Content.ReadAsStringAsync();

            #region descriptografa dados retornados

            if (criptografar)
            {
                responseString = CriptoDecripto.Descritografa_StringFromString(responseString, chaveAES, chaveAES);
            }

            #endregion descriptografa dados retornados

            if (responseString != null)
                return JsonConvert.DeserializeObject<T>(responseString);
            else
            {
                T? t = default;
                return t;
            }
        }
    }
}