using static BLL.UtilBLL;

namespace ExtensionsMethods.Genericos
{
    public static class UtilsMath
    {
        /* LucroVarianteCalc(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4)
         * Calcula lucro variante e retorna valor com decimal para outros cálculos
         * "primeiroValor" é o valor inicial, "segundoValor" é o valor final que foi aumentado ou descrecido do inicial
         */

        public static decimal CalcLucroVarianteDec(decimal primeiroValor = 0, decimal segundoValor = 0, int quantDecimais = 4)
        {
            decimal ret = Convert.ToDecimal("0.00");
            string retCusto = "100.00".ToDecimalInvariant().ToString("N4");
            string retItem = "-100.00".ToDecimalInvariant().ToString("N4");
            if (primeiroValor == Convert.ToDecimal("0.00")) return Convert.ToDecimal(retCusto);
            if (segundoValor == Convert.ToDecimal("0.00")) return Convert.ToDecimal(retItem);
            return Convert.ToDecimal(((segundoValor * 100 / primeiroValor) - 100).ToString("N" + quantDecimais.ToString()));
        }

        /* LucroVariante(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4)
         * Calcula lucro variante e retorna string apenas para mostragem
         * "primeiroValor" é o valor inicial, "segundoValor" é o valor final que foi aumentado ou descrecido do inicial
         */

        public static string CalcLucroVariante(decimal? primeiroValor, decimal? segundoValor, int quantDecimais)
        {
            string? lucroVariante = string.Empty;
            if (string.IsNullOrEmpty(primeiroValor.ToString()) && string.IsNullOrEmpty(segundoValor.ToString()) || primeiroValor == 0 && segundoValor == 0)
                return "";
            else if (primeiroValor == 0)
                return "100,0000";     //primeiro valor 0, então evita divisão por zero e considera apenas o degundo valor dando um lucro de 100%

            if (segundoValor != null && primeiroValor != null)
                lucroVariante = ((segundoValor * 100 / primeiroValor) - 100).GetValueOrDefault().ToString("N" + quantDecimais.ToString());

            return lucroVariante;
        }

        /* LucroVariante(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4, string simboloPercent = "%")
         * Calcula lucro variante e retorna string apenas para mostragem, considerando o símbolo de percentual no valor de texto
         * "primeiroValor" é o valor inicial, "segundoValor" é o valor final que foi aumentado ou descrecido do inicial
         */

        public static string CalcLucroVariante(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4, string simboloPercent = "%")
        {
            string? lucroVariante = string.Empty;
            if (string.IsNullOrEmpty(primeiroValor.ToString()) && string.IsNullOrEmpty(segundoValor.ToString()) || primeiroValor == 0 && segundoValor == 0)
                return "";
            else if (primeiroValor == 0)
                return "100,0000";     //primeiro valor 0, então evita divisão por zero e considera apenas o degundo valor dando um lucro de 100%

            if (segundoValor != null && primeiroValor != null)
                lucroVariante = ((segundoValor * 100 / primeiroValor) - 100).GetValueOrDefault().ToString("N" + quantDecimais.ToString()) + " " + simboloPercent;

            return lucroVariante;
        }
    }
}