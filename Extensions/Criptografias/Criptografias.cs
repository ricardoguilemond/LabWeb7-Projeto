using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Extensions
{
    public static class Criptografias
    {

        private static void Embaralhar(ref char[] array, int vezes)
        {
            // Feito pelo Qoder em 22/08/2026 — Random previsível trocado por gerador criptograficamente seguro (Dívida Técnica §2.3)
            for (int i = 1; i <= vezes; i++)
            {
                for (int x = 1; x <= array.Length; x++)
                {
                    Trocar(ref array[RandomNumberGenerator.GetInt32(0, array.Length)],
                      ref array[RandomNumberGenerator.GetInt32(0, array.Length)]);
                }
            }
        }

        private static void Trocar(ref char arg1, ref char arg2)
        {
            char strTemp = arg1;
            arg1 = arg2;
            arg2 = strTemp;
        }

        public static string GeraSenhaAleatoria()
        {
            // Feito pelo Qoder em 22/08/2026 — aleatoriedade criptograficamente segura (Dívida Técnica §2.3),
            // mantendo exatamente o formato original da senha gerada: "@" + 4 letras + 4 dígitos embaralhados.
            int numero = RandomNumberGenerator.GetInt32(1000, 9000);

            //Algumas letras foram retiradas para evitar que usuários confundam com certos números por problemas visuais
            const string chars = "ABCDEGHKLMNPRTWXYZabcdfghmnpqrtxyz";
            string caracter = new string(Enumerable.Repeat(chars, 4).Select(s => s[RandomNumberGenerator.GetInt32(0, s.Length)]).ToArray());

            string senhaGerada = caracter + numero.ToString();

            // converte em uma matriz de caracteres
            char[] letras = senhaGerada.ToCharArray();

            // vamos embaralhar 5 vezes
            Embaralhar(ref letras, 5);

            // junta as partes da string novamente
            senhaGerada = new string(letras);
            senhaGerada = "@" + senhaGerada;

            return senhaGerada;
        }

    }
}
