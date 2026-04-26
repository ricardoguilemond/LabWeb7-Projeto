namespace ExtensionsMethods.MensagemEmail
{
    public interface IMensagem
    {
        void Enviar(string destinatario, string conteudo);
    }
}