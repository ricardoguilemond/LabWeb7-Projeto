using System;
using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Text;
using ConfigurationManager = Microsoft.Extensions.Configuration.ConfigurationManager;

namespace Extensions
{
    /*
        ESTE GERADOR DE LOGs DESTINA-SE A GERAR LOGS RESTRITOS AOS SERVIÇOS DO "Windows Service", registrando 
        logs importantes sobre eventos ou processamentos que precisam ser monitorados de forma mais minuciosa.

        Qualquer serviço do "Windows Service" poderá ser adicionado aqui neste LOGGER, com a opção 
        de poder ligar e desligar o LOG diretamente por parâmetro no APP do Serviço!

        Para adicionar neste Logger, basta passar o nome do serviço como parâmetro e colocar a 
        variável: "LogTraceServicoVerbose" no APP pertinente ao serviço, para que o LOG Trace seja ligado ou desligado!

        O nome do serviço está em ProjectInstaller.Designer.cs no DisplayName = "BD_bla bla bla" de cada serviço existente.
     */
    public static class LoggerFile
    {
        private const string DefaultDirPath = "C:\\temp\\log\\";
        private static string LogStatus = ConfigurationManager.AppSettings["LogTraceServicoVerbose"];
        /*
            Colocar dentro do APP.Config exclusivo do serviço que se pretende monitorar:
            <add key="LogTraceServicoVerbose" value="ON" />   <!-- ON=ligar o Log trace, OFF=desligar o Log trace-->
         */
        private static void WriteLog(string line, string methodName = "", string className = "", int lineNumber = 0, string nomeServico = null)
        {
            var folderPath = string.Empty;
            var filename = string.Empty;
            try
            {
                /* Pega o nome do serviço e coloca na pasta de log exclusiva do serviço.
                 */
                if (!string.IsNullOrEmpty(nomeServico))
                {
                    folderPath = DefaultDirPath + nomeServico + "\\";
                    filename = string.Format("Log_{0}_{1}.txt", nomeServico, DateTime.Now.ToString("yyyy-MM-dd"));
                } 
                else
                {
                    folderPath = DefaultDirPath + "\\";
                    filename = string.Format("Log_{0}.txt", DateTime.Now.ToString("yyyy-MM-dd"));
                }
                if (!string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(filename))
                {
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    var path = Path.Combine(folderPath, filename);
                    var fw = File.Open(path, FileMode.Append, FileAccess.Write);
                    var sw = new StreamWriter(fw, Encoding.UTF8);
                    var format = string.Format("{0} {1} -> {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffttzzz"), className,
                                 string.IsNullOrEmpty(methodName) ? "" : string.Format("{0}({1}) -> ", methodName, lineNumber));
                    sw.WriteLine(format + line);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                string erro = e.Message;
            }
            finally
            { }
        }

        public static void Write(string nomeServico, Exception ex, string folderPath = DefaultDirPath, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "", [CallerLineNumber] int lineNumber = 0)
        {
            if ((LogStatus != null && LogStatus == "ON") || string.IsNullOrEmpty(nomeServico))
            {
                if (ex != null)
                {
                    Exception exInner = ex;
                    do
                    {
                        WriteLog(exInner.Message, methodName, className, lineNumber, nomeServico);
                        WriteLog(exInner.StackTrace, methodName, className, lineNumber, nomeServico);
                    } while ((exInner = exInner.InnerException) != null);
                }
            }
        }

        public static void Write(string line, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "", [CallerLineNumber] int lineNumber = 0, string nomeServico = null)
        {
            if ((LogStatus != null && LogStatus == "ON") || string.IsNullOrEmpty(nomeServico))
            {
                WriteLog(line, methodName, className, lineNumber, nomeServico);
            }
        }


    }
}
