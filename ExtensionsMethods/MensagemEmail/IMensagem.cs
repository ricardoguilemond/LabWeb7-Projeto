namespace ExtensionsMethods.MensagemEmail
{
    public interface IMensagem
    {
        void Enviar(string destinatario, string conteudo);

        /* Feito pelo Qoder em 22/08/2026 — sobrecarga com assunto (Dívida Técnica §4).
         * Implementação padrão delega para a assinatura original, de modo que implementações
         * existentes (ex.: SMS) não são obrigadas a mudar. */
        void Enviar(string destinatario, string conteudo, string assunto)
        {
            Enviar(destinatario, conteudo);
        }
    }
}
