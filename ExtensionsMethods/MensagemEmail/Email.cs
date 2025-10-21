using System.Net;

namespace ExtensionsMethods.MensagemEmail
{
    public class Email : IMensagem
    {
        public void Enviar(string destinatario, string conteudo)
        {
            //falta construir a lógica para enviar um email no método privado "EnviarEmail"
            Console.WriteLine($"Enviando email para {destinatario}: {conteudo}");
        }

        #region EnviarEmail

        private static string EnviarEmail(string Assunto, string Mensagem, string Email, bool html = true)
        {
            string log = "";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            System.Net.Mail.SmtpClient smtp = new();

            if ((NetworkCredential?)smtp.Credentials != null)
            {
                log += "Credential UserName: " + ((NetworkCredential)smtp.Credentials).UserName + "\n";
            }
            else
                throw new ApplicationException("Credenciais de Email estão nulas");

            System.Net.Mail.MailMessage mail = new(((NetworkCredential)smtp.Credentials).UserName, Email)
            {
                From = new System.Net.Mail.MailAddress(((NetworkCredential)smtp.Credentials).UserName),
                Subject = Assunto,
                Body = Mensagem,
                IsBodyHtml = html
            };
            //Envia
            smtp.Send(mail);

            return log;
        }

        #endregion EnviarEmail
    }
}