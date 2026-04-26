namespace ExtensionsMethods.MensagemEmail
{
    public class Notificador
    {
        /*
            EXEMPLO DE USO:

            IMensagem email = new Email();
            IMensagem sms = new SMS();

            Notificador notificadorEmail = new Notificador(email);
            Notificador notificadorSMS = new Notificador(sms);

            notificadorEmail.Notificar("teste@dominio.com", "Olá, este é um email de teste");
            notificadorSMS.Notificar("219912341234", "Olá, este é um SMS de teste");

         */

        private readonly IMensagem _mensagem;

        public Notificador(IMensagem mensagem)
        {
            _mensagem = mensagem;
        }

        public void Notificar(string destinatario, string conteudo)
        {
            _mensagem.Enviar(destinatario, conteudo);
        }
    }
}