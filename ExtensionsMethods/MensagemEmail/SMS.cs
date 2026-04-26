namespace ExtensionsMethods.MensagemEmail
{
    public class SMS : IMensagem
    {
        public void Enviar(string destinatario, string conteudo)
        {
            //falta construir a lógica para enviar um email
            Console.WriteLine($"Enviando SMS para {destinatario}: {conteudo}");
        }
    }
}