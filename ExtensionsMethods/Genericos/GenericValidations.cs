using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.Net.Sockets;

namespace ExtensionsMethods.Genericos
{
    public class GenericValidations
    {
        /* Vai armazenar 100% o conteúdo completo do Response do ReCaptcha do Google */
        public static HttpResponseMessage? ConteudoCompletoReponseCaptchaGoogle { get; set; }

        /* URL do Google para envio de chave secreta e retorno do response com o Token
           IMPORTANTÍSSIMO: ************************************************************************************
           URL do serviço V3 gratuito:     "https://www.google.com/recaptcha/api.js?render="
           URL do serviço Enerprise PAGO:  "https://www.google.com/recaptcha/enterprise.js?render=";
           *****************************************************************************************************
         */

        /*
           Exemplo de uso: IPLocal
           GenericValidations genericValidations = new GenericValidations();
            _ = genericValidations.GetIPLocal(HttpContext);   // pega o IP Local do usuário, e coloca na var IPLocal.
         */
        public static string? IPLocal { get; set; }

        public static Task<string> GetIPLocal(HttpContext httpContext)
        {
            //Recuperando a lista de IPs do DNS (em teste)
            //var hostName = System.Net.Dns.GetHostName();
            //var listaIps = await System.Net.Dns.GetHostAddressesAsync(hostName);
            //Fim do DNS em teste

            //Recuperando o IP Azure, senão recupera do Local.
            string? LocalIPAddr = httpContext.Request.Headers["X-Azure-ClientIP"];
            if (string.IsNullOrWhiteSpace(LocalIPAddr))
            {
                //Retreive server/ local IP address
                IHttpConnectionFeature? feature = httpContext.Features.Get<IHttpConnectionFeature>();
                try
                {
                    LocalIPAddr = feature?.LocalIpAddress?.ToString();
                    if (LocalIPAddr == "::1") LocalIPAddr = "127.0.0.1";
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                }
                finally
                { }
            }
            IPLocal = !string.IsNullOrEmpty(LocalIPAddr) ? LocalIPAddr : string.Empty;
            return Task.FromResult(!string.IsNullOrEmpty(LocalIPAddr) ? LocalIPAddr : string.Empty);
        }

        private static IPAddress[] _ips = new IPAddress[1];

        /*
           Exemplo de uso: IPLocal
           GenericValidations genericValidations = new GenericValidations();
            _ = genericValidations.GetIPLocal(HttpContext);   // pega o IP Local do usuário, e coloca na var IPLocal.
         */

        /* Pega o Nome do Computador Local
         * string machineName = GetMachineNameFromIPAddress(yourIPAdress);
         */

        public static string GetMachineNameFromIPAddress(string ipAdress)
        {
            string machineName = string.Empty;
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ipAdress);

                machineName = hostEntry.HostName;
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            return machineName;
        }

        public static string GetLocalHostName()
        {
            string hostname = string.Empty;
            try
            {
                // Get the local computer host name.
                hostname = Dns.GetHostName();
            }
            catch (SocketException e)
            {
                LoggerFile.Write("SocketException caught!!!");
                LoggerFile.Write("Source : " + e.Source);
                LoggerFile.Write("Message : " + e.Message);
            }
            catch (Exception e)
            {
                LoggerFile.Write("Exception caught!!!");
                LoggerFile.Write("Source : " + e.Source);
                LoggerFile.Write("Message : " + e.Message);
            }
            finally
            { }
            return hostname;
        }

        public static Task<IPAddress[]> GetIPLista()
        {
            try
            {
                string hostName = Dns.GetHostName();
                _ips = Dns.GetHostAddresses(hostName);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            { }
            return Task.FromResult(_ips);
        }

        /* Traz a lista de IPs que estão em uso pela máquina do usuário.
         * Traz IPV4 e IPV6 pela ordem da fila.
         * USO:
         *       GenericValidations genericValidations = new GenericValidations();
         *       ICollection<string> listaIPs = genericValidations.ListaDeIPs();
         * */

        public static ICollection<string> ListaDeIPs()
        {
            Task<IPAddress[]> lista = GetIPLista();

            ICollection<string> IPAdresses = [];

            for (int i = 0; i < lista.Result.Length; i++)
            {
                IPAdresses.Add(lista.Result[i].ToString());
            }
            return IPAdresses;
        }

        /* Um Host pode ter múltiplos endereços IP (método muito bom)
         * USO:
         *      GenericValidations genericValidations = new GenericValidations();
         *      ICollection<string> listaIPs = genericValidations.GetIPs();
         */

        public static ICollection<string> GetIPs(bool ip4Wanted = true, bool ip6Wanted = false)
        {
            List<string> listaIP = [];

            string hostName = GetLocalHostName();

            IPAddress[] addressList;
            try
            {
                addressList = Dns.GetHostAddresses(hostName);
            }
            catch
            {
                return listaIP; // Retorna vazio em caso de erro
            }

            IEnumerable<string> filtrados = addressList
                .Where(ip =>
                    (ip4Wanted && ip.AddressFamily == AddressFamily.InterNetwork) ||
                    (ip6Wanted && ip.AddressFamily == AddressFamily.InterNetworkV6))
                .Select(ip => ip.ToString());

            listaIP.AddRange(filtrados);

            return listaIP;
        }
    }
}