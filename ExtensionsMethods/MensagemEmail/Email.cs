using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace ExtensionsMethods.MensagemEmail
{
    /* Feito pelo Qoder em 22/08/2026 — implementação REAL do serviço de e-mail (Dívida Técnica §4).
     *
     * ANTES: Enviar() era um stub (Console.WriteLine) e o método privado EnviarEmail nunca era chamado
     * (falharia sempre, pois SmtpClient sem configuração tem credenciais nulas).
     *
     * AGORA: as credenciais SMTP vêm da cadeia de configuração da aplicação:
     *   appsettings.json → appsettings.Segredos.json (fora do Git) → variáveis de ambiente.
     * A senha usada deve ser uma "Senha de App" do provedor (nunca a senha pessoal da conta).
     *
     * USO (requisito de produto: recuperação de senha):
     *   IMensagem email = new Email();
     *   email.Enviar("usuario@dominio.com", "<h1>...</h1>", "Recuperação de senha");
     *
     * A chave de configuração é "EmailConfiguration" (seção já existente no appsettings).
     */
    public class Email : IMensagem
    {
        private readonly IConfiguration? _configuration;

        public Email()
        {
        }

        /// <summary>
        /// Uso preferencial via injeção de dependência: recebe o IConfiguration já montado pelo host.
        /// </summary>
        public Email(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Envia um e-mail em HTML para o destinatário informado.
        /// </summary>
        /// <param name="destinatario">Endereço de e-mail do destinatário</param>
        /// <param name="conteudo">Corpo da mensagem (HTML)</param>
        /// <param name="assunto">Assunto do e-mail</param>
        public void Enviar(string destinatario, string conteudo, string assunto = "LabWeb")
        {
            IConfiguration configuration = _configuration ?? ConstruirConfiguracaoLocal();

            string servidor = configuration["EmailConfiguration:SmtpServer"] ?? "smtp.gmail.com";
            // Porta 587 com STARTTLS: suportada pelo SmtpClient do .NET (a 465/SSL implícito não é)
            int porta = int.TryParse(configuration["EmailConfiguration:SmtpPortTLS"], out int p) ? p : 587;
            string usuario = configuration["EmailConfiguration:SmtpUsername"] ?? string.Empty;
            string senhaDeApp = configuration["EmailConfiguration:SmtpSenhaApp"] ?? string.Empty;
            string nomeRemetente = configuration["EmailConfiguration:SmtpName"] ?? "LabWeb";

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senhaDeApp))
                throw new ApplicationException("Credenciais de e-mail não configuradas. " +
                    "Defina EmailConfiguration:SmtpUsername e EmailConfiguration:SmtpSenhaApp " +
                    "em appsettings.Segredos.json (ou nas variáveis de ambiente EmailConfiguration__SmtpUsername / __SmtpSenhaApp).");

            using SmtpClient smtp = new(servidor, porta)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(usuario, senhaDeApp)
            };

            using MailMessage mail = new(new MailAddress(usuario, nomeRemetente), new MailAddress(destinatario))
            {
                Subject = assunto,
                Body = conteudo,
                IsBodyHtml = true
            };

            smtp.Send(mail);
        }

        /* Mantém compatibilidade com a assinatura original da interface (assunto padrão). */
        public void Enviar(string destinatario, string conteudo)
        {
            Enviar(destinatario, conteudo, "LabWeb");
        }

        private static IConfiguration ConstruirConfiguracaoLocal()
        {
            // Mesma cadeia usada pelo host web e pela GoogleConfig (o último vence).
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Segredos.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
