using static BLL.UtilBLL;

namespace ExtensionsMethods.Genericos
{
    public static class UtilsMath
    {
        /* MargemSobreCustoCalc(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4)
         * Calcula a margem sobre o custo e retorna valor decimal para outros cálculos.
         * Fórmula: ((Venda - Custo) / Custo) * 100
         * "primeiroValor" é o custo, "segundoValor" é o valor de venda.
         */

        public static decimal CalcMargemSobreCustoDec(decimal primeiroValor = 0, decimal segundoValor = 0, int quantDecimais = 4)
        {
            decimal ret = Convert.ToDecimal("0.00");
            string retCusto = "100.00".ToDecimalInvariant().ToString("N4");
            string retItem = "-100.00".ToDecimalInvariant().ToString("N4");
            if (primeiroValor == Convert.ToDecimal("0.00")) return Convert.ToDecimal(retCusto);
            if (segundoValor == Convert.ToDecimal("0.00")) return Convert.ToDecimal(retItem);
            return Convert.ToDecimal(((segundoValor * 100 / primeiroValor) - 100).ToString("N" + quantDecimais.ToString()));
        }

        /* MargemSobreCusto(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4)
         * Calcula a margem sobre o custo e retorna string para mostragem.
         * Fórmula: ((Venda - Custo) / Custo) * 100
         * "primeiroValor" é o custo, "segundoValor" é o valor de venda.
         */

        public static string CalcMargemSobreCusto(decimal? primeiroValor, decimal? segundoValor, int quantDecimais)
        {
            string? margemSobreCusto = string.Empty;
            if (string.IsNullOrEmpty(primeiroValor.ToString()) && string.IsNullOrEmpty(segundoValor.ToString()) || primeiroValor == 0 && segundoValor == 0)
                return "";
            else if (primeiroValor == 0)
                return "100,0000";     //custo zero, então evita divisão por zero e considera apenas a venda dando uma margem de 100%

            if (segundoValor != null && primeiroValor != null)
                margemSobreCusto = ((segundoValor * 100 / primeiroValor) - 100).GetValueOrDefault().ToString("N" + quantDecimais.ToString());

            return margemSobreCusto;
        }

        /* MargemSobreCusto(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4, string simboloPercent = "%")
         * Calcula a margem sobre o custo e retorna string para mostragem, considerando o símbolo de percentual no valor de texto.
         * Fórmula: ((Venda - Custo) / Custo) * 100
         * "primeiroValor" é o custo, "segundoValor" é o valor de venda.
         */

        public static string CalcMargemSobreCusto(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4, string simboloPercent = "%")
        {
            string? margemSobreCusto = string.Empty;
            if (string.IsNullOrEmpty(primeiroValor.ToString()) && string.IsNullOrEmpty(segundoValor.ToString()) || primeiroValor == 0 && segundoValor == 0)
                return "";
            else if (primeiroValor == 0)
                return "100,0000 " + simboloPercent;     //custo zero, então evita divisão por zero e considera apenas a venda dando uma margem de 100%

            if (segundoValor != null && primeiroValor != null)
                margemSobreCusto = ((segundoValor * 100 / primeiroValor) - 100).GetValueOrDefault().ToString("N" + quantDecimais.ToString()) + " " + simboloPercent;

            return margemSobreCusto;
        }

        /* MargemBrutaCalc(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4)
         * Calcula a margem bruta e retorna valor decimal para outros cálculos.
         * Fórmula: ((Venda - Custo) / Venda) * 100
         * "primeiroValor" é o custo, "segundoValor" é o valor de venda.
         */

        public static decimal CalcMargemBrutaDec(decimal primeiroValor = 0, decimal segundoValor = 0, int quantDecimais = 4)
        {
            string retCusto = "-100.00".ToDecimalInvariant().ToString("N4");
            string retVenda = "100.00".ToDecimalInvariant().ToString("N4");
            if (segundoValor == Convert.ToDecimal("0.00")) return Convert.ToDecimal(retCusto);
            if (primeiroValor == Convert.ToDecimal("0.00")) return Convert.ToDecimal(retVenda);
            return Convert.ToDecimal(((segundoValor - primeiroValor) * 100 / segundoValor).ToString("N" + quantDecimais.ToString()));
        }

        /* MargemBruta(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4, string simboloPercent = "%")
         * Calcula a margem bruta e retorna string para mostragem, considerando o símbolo de percentual no valor de texto.
         * Fórmula: ((Venda - Custo) / Venda) * 100
         * "primeiroValor" é o custo, "segundoValor" é o valor de venda.
         */

        public static string CalcMargemBruta(decimal? primeiroValor, decimal? segundoValor, int quantDecimais = 4, string simboloPercent = "%")
        {
            string? margemBruta = string.Empty;
            if (string.IsNullOrEmpty(primeiroValor.ToString()) && string.IsNullOrEmpty(segundoValor.ToString()) || primeiroValor == 0 && segundoValor == 0)
                return "";
            else if (segundoValor == 0)
                return "-100,0000 " + simboloPercent;     //venda zero, então evita divisão por zero e considera apenas o custo dando uma margem de -100%

            if (segundoValor != null && primeiroValor != null)
                margemBruta = ((segundoValor - primeiroValor) * 100 / segundoValor).GetValueOrDefault().ToString("N" + quantDecimais.ToString()) + " " + simboloPercent;

            return margemBruta;
        }
    }
}
