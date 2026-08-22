using System.Security.Cryptography;
using System.Text;

namespace LabWebMvc.MVC.Areas.Validations
{
    /* Feito pelo Qoder em 22/08/2026 — TOKEN ASSINADO PARA RECUPERAÇÃO DE SENHA POR E-MAIL (Dívida Técnica §4).
     *
     * Formato: Base64Url( idUsuario | expiraEpochUtc | loginUsuario | assinaturaHmacSha256 )
     *   - assinatura = HMACSHA256("id|expira|login", Settings.Secret) em hexadecimal.
     *   - Sem dependência de pacotes novos (usa System.Security.Cryptography, nativo do .NET 8).
     *
     * PROPRIEDADES DE SEGURANÇA:
     *   - Autenticidade: qualquer adulteração invalida a assinatura (comparação em tempo constante).
     *   - Expiração curta (padrão 30 minutos) limita a janela de uso de um token interceptado.
     *   - Vínculo ao Id E ao login do usuário impede reaproveitamento em outra conta.
     *   - O token NUNCA permite descobrir a senha: ele apenas autoriza a definição de uma NOVA senha.
     *
     * O segredo (Settings.Secret) aceita override por variável de ambiente LABWEB7_SECRET (ver Settings.cs).
     */
    public static class TokenRecuperacao
    {
        private static readonly TimeSpan ValidadePadrao = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gera o token de recuperação para o usuário informado.
        /// </summary>
        public static string Gerar(int idUsuario, string loginUsuario, TimeSpan? validade = null)
        {
            long expira = DateTimeOffset.UtcNow.Add(validade ?? ValidadePadrao).ToUnixTimeSeconds();
            string carga = string.Format("{0}|{1}|{2}", idUsuario, expira, loginUsuario);
            string assinatura = Assinar(carga);

            string pacote = string.Format("{0}|{1}", carga, assinatura);
            return Base64UrlEncode(Encoding.UTF8.GetBytes(pacote));
        }

        /// <summary>
        /// Valida o token recebido pelo link do e-mail. Retorna true somente se a assinatura
        /// confere, o prazo não expirou e id/login batem com o registro do banco.
        /// </summary>
        public static bool Validar(string? token, int idUsuario, string loginUsuario)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                string pacote = Encoding.UTF8.GetString(Base64UrlDecode(token));
                string[] partes = pacote.Split('|');
                if (partes.Length != 4)
                    return false;

                if (!int.TryParse(partes[0], out int idToken))
                    return false;
                if (!long.TryParse(partes[1], out long expiraToken))
                    return false;

                // Assinatura deve cobrir exatamente a carga original
                string carga = string.Format("{0}|{1}|{2}", partes[0], partes[1], partes[2]);
                if (!ComparacaoConstante(Assinar(carga), partes[3]))
                    return false;

                // Prazo de validade
                if (DateTimeOffset.FromUnixTimeSeconds(expiraToken) <= DateTimeOffset.UtcNow)
                    return false;

                // Vínculo com o usuário destinatário do link
                return idToken == idUsuario
                    && string.Equals(partes[2], loginUsuario, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Token malformado (Base64 inválido, etc.) nunca deve derrubar a requisição
                return false;
            }
        }

        private static string Assinar(string carga)
        {
            using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(Settings.Secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(carga));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool ComparacaoConstante(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            int diferenca = 0;
            for (int i = 0; i < a.Length; i++)
                diferenca |= a[i] ^ b[i];
            return diferenca == 0;
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string texto)
        {
            string base64 = texto.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
