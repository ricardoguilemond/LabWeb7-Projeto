using System.IO;
using System.Security.Cryptography;
using System;
using System.Text;
using LabWebMvc.MVC._1_ExtensionsMethods.Utils;
using Microsoft.AspNetCore.Rewrite;
using System.Security.Cryptography.Xml;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;

namespace Extensions
{
    class CriptoDecripto : IDisposable
    {

        private readonly string gatewayUrl = "http://localhost:10000";   //link de leitura do JSON recuperado de um serviço no Servidor
        public readonly HttpClient cliente = new HttpClient();

        /* Abaixo: public const string mySecretKeyGoogle = "6LfRqvshAAAAAKexoIJ5I9GVMAd0ZGg_vZ78SU4R";
         * MINHA CHAVE SECRETA gerada na minha conta Google (rguilemond@gmail) do recaptcha Enterprise 
         * para a etiqueta de nome: "LabWebMvc reCAPTCHA"
         * 
         * Meu acesso a chave reCaptcha Enterprise no Google: 
         * (Começa a cobrança de $1.00/mês a cada 1000 acessos depois de 1 milhão de acessos esgotados no mês!)
         * Nome do Projeto:   LabWebMvc
         * Número do Projeto: 996730271306
         * ID do Projeto:     labwebmvc
         * https://console.cloud.google.com/apis/dashboard?project=labwebmvc&hl=pt-br
         * https://console.cloud.google.com/home/dashboard?project=labwebmvc&hl=pt-br
         * 
         * Essa opção só deve ser usada ao fazer a integração com terceiros que solicitem sua 
         * chave secreta/privada (ou qualquer integração legada que use o 
         * endpoint https://www.google.com/recaptcha/api/siteverify no back-end).
         * 
         * <script src="https://www.google.com/recaptcha/enterprise.js?render=6LfRqvshAAAAALOqM-WV8TQl69IBjjpAypcnOrPW"></script>
         * <script>
         *     grecaptcha.enterprise.ready(function() { 
         *         grecaptcha.enterprise.execute('6LfRqvshAAAAALOqM-WV8TQl69IBjjpAypcnOrPW', { 
         *             action: 'login'}).then(function(token) {
         *                 ...
         *             });
         *     });
         * </script>
         * 
         */
        /* 
         * Esta chave é RESTRITA/SECRETA e é paga a partir de 1 milhão/mês (Google Enterprise) */
        public const string mySecretKeyGoogle = "6LfRqvshAAAAAKexoIJ5I9GVMAd0ZGg_vZ78SU4R";
        /* 
         * Esta chave SITE, que é usada no código HTML */
        public const string mySecretKeyPublic = "6Le_QQYiAAAAANuFhenHQ5DpfJCGIfa2X1O51ltB";
        /* 
         * Esta chave SECRETA, que é RESTRITA e é usada SOMENTE para comunicar o meu SITE com o RECAPTCHA */
        public const string mySecretKeyPrivate = "6Le_QQYiAAAAAAF7jG4PZalVceazfJZnlbJVKodL";

        /* Vai armazenar o conteúdo completo do Response do ReCaptcha do Google, incluindo os códigos de erros */
        public static HttpResponseMessage? ConteudoCompletoReponseCaptchaGoogle;



        public void Dispose()
        {
            cliente.Dispose();
        }

        /* Método: Executa<T>
         * Executa métodos alternativos vindos de outros servidores ou solutions, trafegando com criptografia ponta-a-ponta 
         * PRECISA SER TESTADO E MELHOR IMPLEMENTADO
         */
        public async Task<T> Executa<T>(string servidor, string servico, string metodo, ParametrosGenericos parametros)
        {
            // GatewayURL/SERVIDOR/Generated/SERVICO.smartsvc/json/METODO
            var stringContent = JsonConvert.SerializeObject(parametros);

            #region criptografia dos parametros
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
            var chaveAES = "k7r@E&UHC0nn7ds0"; //ConfigurationManager.AppSettings["KeyAES"]; //Chave de criptografia AES do App.Config do Serviço
            var criptografar = "S".Equals(ConfigurationManager.AppSettings["LigaCriptografia"]);
            if (criptografar)
            {
                stringContent = Criptografa_StringToString(stringContent, chaveAES, chaveAES);
            }
            #endregion

            var content = new StringContent(stringContent, Encoding.UTF8, "application/json");
            // GatewayURL/SERVIDOR/Generated/SERVICO.smartsvc/json/METODO
            var response = await cliente.PostAsync(gatewayUrl + @"/" + servidor + @"/Generated/" + servico + @".smartsvc/json/" + metodo, content);

            var responseString = await response.Content.ReadAsStringAsync();
            #region descriptografa dados retornados
            if (criptografar)
            {
                responseString = Descritografa_StringFromString(responseString, chaveAES, chaveAES);
            }
            #endregion

            return JsonConvert.DeserializeObject<T>(responseString);
        }


        /* 
         * Criptografia padrão AES (Pode ser utilizado para criptografar/descriptografar qualquer dado)
         * 
         * EXEMPLO DE USO:
         * 
         * var criptografia = CriptoDecripto.Criptografa_StringToString("Ricardo Guilemond", "AquiEumaChaveAES", "AquiEumaChaveAES");
         * var descriptografia = CriptoDecripto.Descritografa_StringFromString(criptografia, "AquiEumaChaveAES", "AquiEumaChaveAES");
         * 

         */
        static byte[] Criptografa_StringToBytes_AES(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        static string Descriptografa_BytesToString_AES(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }

        /* Estou usando uma chave qualquer que nem tem valor, apenas como exemplo, 
         * quando vier a correta, todos os dados salvos gerados por ela vão se perder, 
         * porque a chave é o único recurso de criptografia e descriptografia possível de identificação!
         */

        public static string Descritografa_StringFromString(string cipherText, string key = "AquiEumaChaveAES", string iv = "AquiEumaChaveAES")
        {
            return Descriptografa_BytesToString_AES(Convert.FromBase64String(cipherText), Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(iv));
        }
        public static string Criptografa_StringToString(string plainText, string key = "AquiEumaChaveAES", string iv = "AquiEumaChaveAES")
        {
            return Convert.ToBase64String(Criptografa_StringToBytes_AES(plainText, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(iv)));
        }
        public static byte[] Criptografa_StringToBytes(string plainText, string key, string iv)
        {
            return Criptografa_StringToBytes_AES(plainText, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(iv));
        }


    }
}
