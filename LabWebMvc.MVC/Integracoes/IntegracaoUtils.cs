using LabWebMvc.MVC.Models;
using System.Globalization;
using static LabWebMvc.MVC.Integracoes.Enum;

namespace LabWebMvc.MVC.Integracoes
{
    public static class IntegracaoUtils
    {
        public static String FormatNumber(decimal number, String format = "{0:0.00}")
        {
            return string.Format(new CultureInfo("en-US"), format, number);
        }

        /* Sobrescrita do método */
        //public static Tuple<int, string> GetTiposServicosImpactoFinanceiroFiscal_CPagar(this int tipoServico)
        //{
        //    /* Somente serviços do Contas à Pagar */
        //    Tuple<int, string>[] lista = {
        //                               new Tuple<int,string>(13, "Serviço Gerador de Comissão"),
        //                               new Tuple<int,string>(34, "Serviço Gerador do PagFor"),
        //                               new Tuple<int,string>(35, "Serviço de Baixa do PagFor")
        //                              };
        //    Tuple<int, string> res = lista.Where(c => c.Item1 == tipoServico).SingleOrDefault();
        //    if (res == null) return new Tuple<int, string>(0, "");
        //    else return res;
        //}

        /// <summary>
        /// * Controlando os serviços de integração financeira para o Conta a Receber */
        /// </summary>
        //public static ICollection<int> GetTiposServicosImpactoFinanceiroFiscal_CReceber()
        //{
        //    var lista = new HashSet<int>() { 5, 6, 8, 9, 100, 101, 103, 104 };
        //    return lista;
        //}

        /// <summary>
        /// * Sobrescrita do método */
        /// </summary>
        //public static Tuple<int, string> GetTiposServicosImpactoFinanceiroFiscal_CReceber(this int tipoServico)
        //{
        //    /* Somente serviços do Contas a Receber  */
        //    Tuple<int, string>[] lista = {
        //                               new Tuple<int,string>(5, "Serviço Gerador de Débito Automático"),
        //                               new Tuple<int,string>(6, "Serviço de Baixa de Débito Automático"),
        //                               new Tuple<int,string>(8, "Serviço de Geração de Boleto"),
        //                               new Tuple<int,string>(9, "Serviço de Baixa de Boleto"),
        //                               new Tuple<int,string>(100, "Serviço Gerador de Débito Automático PF"),    //GERPF
        //                               new Tuple<int,string>(101, "Serviço de Baixa de Débito Automático PF"),   //GERPF
        //                               new Tuple<int,string>(103, "Serviço Gerador de Boleto PF"),   //GERPF
        //                               new Tuple<int,string>(104, "Serviço de Baixa de Boleto PF")   //GERPF
        //                             };
        //    Tuple<int, string> res = lista.Where(c => c.Item1 == tipoServico).SingleOrDefault();
        //    if (res == null) return new Tuple<int, string>(0, "");
        //    else return res;
        //}

        /* Método genérico que cria o nome do arquivo de serviços de integração de forma incremental/sequencial por data
         */

        public static string NomeArquivoIncremental(this string nomeArquivo, DateTime ultimaData, Db db, IntegracaoDadosExecucao dadosExecucao)
        {
            int lay = dadosExecucao.IntegracaoDadosLayoutId;
            //No linq abaixo estamos usando (DateTime.Compare(data.Value.Date))==0, para evitar DbFunction.TruncateTime(data), mas tem que testar!
            LogArquivos? Lista = db.LogArquivos.Where(c => c.IntegracaoDadosLayoutId == lay && c.DataPeriodoFinal.HasValue &&
                                                   DateTime.Compare(c.DataPeriodoFinal.Value.Date, ultimaData.Date) == 0).ToList().LastOrDefault();

            string strTotal = string.Empty;
            int Total = 1;    /* primeiro arquivo do período quando a lista é nula/vazia */
            if (Lista != null)
            {
                strTotal = Lista.NomeArquivo!.Substring(Lista.NomeArquivo.LastIndexOf('_'), Lista.NomeArquivo.Length - Lista.NomeArquivo.LastIndexOf('_'));
                Total = strTotal.RetornaSomenteNumeros() + 1;
            }
            return string.Format("{0}{1}{2}{3}{4}", nomeArquivo, ultimaData.ToString("yyyyMMdd"), "_", Total.ToString(), ".txt");
        }

        /* Retorna somente os números de dentro de uma string */

        public static Int32 RetornaSomenteNumeros(this string Texto)
        {
            return Convert.ToInt32(string.Join("", Texto.ToCharArray().Where(Char.IsDigit)));
        }

        /* retorna qualquer texto do tipo string entre aspas duplas */

        public static string InsereAspasDuplas(this string texto)
        {
            string parametro = "\"";
            return string.Format("{0}{1}{2}", parametro, texto, parametro);
        }

        /*
         * 1) Seja a extração de qualquer DIA da semana atual, retornaremos apenas o DIA do período da semana ANTERIOR!
         * 2) Seja a extração de qualquer DIA do mês corrente, retornaremos apenas o DIA do período do mês anterior!
         */

        public static DateTime[]? ResolveDataExtracaoPeriodoAnterior(this DateTime dataHoje, TipoPeriodoExtracao TipoPeriodo)
        {
            if (TipoPeriodo == TipoPeriodoExtracao.Diario) //pega todos os registros de onten e de hoje até um momento qualquer
            {
                DateTime dataInicio = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                DateTime dataFim = dataInicio;
                dataInicio = dataFim.AddDays(-1);
                return new DateTime[] { dataInicio, dataFim };
            }
            else if (TipoPeriodo == TipoPeriodoExtracao.Semanal)
            {
                int diaSemana = (int)dataHoje.DayOfWeek;  // dia da semana atual/vigente
                /* Mas se o dia da semana JÁ for um domingo, então ainda continua pegando o domingo ANTERIOR,
                 * porque o período considerado completo é de segunda a domingo, e, o domingo atual só se completa após 23:59!
                 */
                if (diaSemana == 0)  // ops, se estamos em um domingo, então continua pegando até o domingo passado!
                {
                    diaSemana = (int)dataHoje.AddDays(-1).DayOfWeek;
                }
                DateTime dataPassadaFinal = dataHoje.AddDays(-diaSemana);        // pegando o último domingo
                DateTime dataPassadaInicial = dataPassadaFinal.AddDays(-6);      // pegando a última segunda ANTERIOR ao último domingo
                return new DateTime[] { dataPassadaInicial, dataPassadaFinal };
            }
            else if (TipoPeriodo == TipoPeriodoExtracao.Mensal)
            {
                DateTime dataFim = DateTime.Today;
                DateTime dataInicio = dataFim;

                // Pega o mês anterior
                dataFim = dataFim.AddMonths(-1);
                // Pega a data já com o último dia do mês anterior
                dataFim = new DateTime(dataFim.Year, dataFim.Month, DateTime.DaysInMonth(dataFim.Year, dataFim.Month));

                //pega o primeiro dia do mês anterior
                dataInicio = new DateTime(dataFim.Year, dataFim.Month, 1);

                return new DateTime[] { dataInicio, dataFim };
            }
            else if (TipoPeriodo == TipoPeriodoExtracao.Retroativo)
            {
                //qualquer período informado pelo usuário
                return null;
            }
            return new DateTime[] { };
        }

        public static DateTime DataCalculada(this DateTime data, TipoPeriodoExtracao tipo, string per = "final")
        {
            DateTime[]? datas = data.ResolveDataExtracaoPeriodoAnterior(tipo);
            if (datas == null) return data;

            return per == "final" ? datas[1] : datas[0];   /* 1 = temos aqui a última data do período, 0 = primeira data do período */
        }

        public static string RetornaDecimalComPonto(this string valor)
        {
            /* método importante para tratar questões em que o valor chega com virgula e ponto ou com dois pontos */
            valor = valor.Replace(",", ".");
            string valorInvertido = new(valor.Reverse().ToArray());
            int pos = valorInvertido.IndexOf('.');
            valorInvertido = valorInvertido.Replace(".", "");
            valor = string.Format("{0}{1}{2}", valorInvertido.Substring(0, pos), ".", valorInvertido.Substring(pos, valorInvertido.Length - pos));
            valor = new string(valor.Reverse().ToArray());
            return valor.Replace(",", ".");
        }

        public static string TamanhoMaximo(this string valor, int tam = 1)
        {
            if (string.IsNullOrEmpty(valor)) return "";
            tam = (tam < 1) ? 1 : tam;
            return (tam > valor.Trim().Length) ? valor.Trim() : valor.Trim().Substring(0, tam);
        }
    }
}