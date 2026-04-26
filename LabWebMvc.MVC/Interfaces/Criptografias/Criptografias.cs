namespace LabWebMvc.MVC.Interfaces.Criptografias
{
    public static class Criptografias
    {
        private static void Embaralhar(ref char[] array, int vezes)
        {
            Random rand = new(DateTime.UtcNow.Millisecond);

            for (int i = 1; i <= vezes; i++)
            {
                for (int x = 1; x <= array.Length; x++)
                {
                    Trocar(ref array[rand.Next(0, array.Length)],
                      ref array[rand.Next(0, array.Length)]);
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
            Random randomico = new();
            int numero = randomico.Next(1000, 9000);

            //Algumas letras foram retiradas para evitar que usuários confundam com certos números por problemas visuais
            const string chars = "ABCDEGHKLMNPRTWXYZabcdfghmnpqrtxyz";
            string caracter = new(Enumerable.Repeat(chars, 4).Select(s => s[randomico.Next(s.Length)]).ToArray());

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