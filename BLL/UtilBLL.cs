using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BLL
{
    public static class UtilBLL
    {
        public static string Getbolinha => " • ";

        public class FeriadoDetalhe
        {
            public int ID { get; set; }
            public int Dia { get; set; }
            public int Mes { get; set; }
            public int Ano { get; set; }
            public string? Descricao { get; set; }
        }

        public class TextoMenuPrincipalResponse
        {
            public static object[] TextoMenuPrincipal { get; set; } = null!;
        }

        public class MensagemHtmlResponse
        {
            public static object[] MensagemHtml { get; set; } = null!;
        }

        public static class PartialFiltro
        {
            public static bool Esconde { get; set; } = false;
            public static string Action { get; set; } = null!;
            public static string Controller { get; set; } = null!;
            public static string ActionButton { get; set; } = null!;
            public static string ControllerButton { get; set; } = null!;
            public static string ActionLink { get; set; } = null!;
            public static string ControllerLink { get; set; } = null!;
        }

        public static bool IsNumeric(this string text) => double.TryParse(text, out _);

        private static bool ContemNumeros(this string text)
        {
            string[] numeros = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            foreach (string s in numeros)
            {
                if (text.Contains(s))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ContemStringAZ(this string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Any(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
        }

        public static string ClearString(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // Remove acentos usando normalização Unicode
            string textoNormalizado = texto.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new();

            foreach (char c in textoNormalizado)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            string semAcento = sb.ToString().Normalize(NormalizationForm.FormC);

            // Remove símbolos indesejados com Regex (tudo que não for letra ou número ou espaço)
            return Regex.Replace(semAcento, @"[^a-zA-Z0-9\s]", string.Empty);
        }

        /* Remove acentuação em qualquer situação */

        public static string RemoveAcentuacao(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new();

            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /* Retorna somente Números de uma string qualquer (Remove letras e símbolos) */

        public static string RetornaNumeros(this string? str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            return Regex.Replace(str, @"\D", "");
        }

        /* Converte a primeira letra de todas as palavras da frase para maiúsculas */

        public static string ConvertStringMaiuscula(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string[] palavras = value.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', palavras.Select(p => char.ToUpper(p[0]) + p[1..]));
        }

        public static bool IsFimDeSemana(this DateTime data)
        {
            //Verifica se a Data passada é referente ao dia Sábado ou Domingo
            if ((data.DayOfWeek == DayOfWeek.Saturday) || (data.DayOfWeek == DayOfWeek.Sunday))
                return true;
            else
                return false;
        }

        public static bool IsFeriado(this DateTime data)
        {
            DateTime dataInicial = new(data.Year, data.Month, data.Day, 0, 0, 0);
            DateTime dataFinal = new(data.Year, data.Month, data.Day, 23, 59, 59);

            return false;
            //montar a tabela abaixo de ano a ano e retirar esse return false de cima
            //    return db.FeriadoAtendimentoVirtuais.Any(a => a.DataFeriado >= dataInicial && a.DataFeriado <= dataFinal);
        }

        /* Retorna uma lista de "n" dias úteis a frente */

        public static List<DateTime> GetProximasDatasUteis(this int qtdDias)
        {
            List<DateTime> datasUteis = new(qtdDias);
            DateTime dataAtual = DateTime.Today;

            while (datasUteis.Count < qtdDias)
            {
                if (!IsFimDeSemana(dataAtual) && !IsFeriado(dataAtual))
                {
                    datasUteis.Add(dataAtual);
                }
                dataAtual = dataAtual.AddDays(1);
            }
            return datasUteis;
        }

        public static bool ValidaSenhaSimples(this string texto)
        {
            return texto.ContemNumeros() && texto.ContemStringAZ();
        }

        public static void RemoverCaracteresEspeciais(this object obj)
        {
            obj.GetType().GetProperties()
               .Where(w => w.PropertyType == typeof(string) && !string.IsNullOrWhiteSpace((string?)w.GetValue(obj)))
               .ToList().ForEach(f =>
               {
                   string? oldValue = (string?)f.GetValue(obj);
                   f.SetValue(obj, string.IsNullOrWhiteSpace(oldValue) ? string.Empty :
                                    Encoding.ASCII.GetString(Encoding.GetEncoding("Cyrillic").GetBytes(oldValue)));
               });
        }

        /*
         * MENU FLUTUANTE:
         * Exemplos de USO: (passamos apenas 3 parâmetros em quantas tuplas desejar!)
         *
         *          ConstroiBotoesFormulario( { "type=type",   "value=value", "id", "onclick=funcaoChamada()" } )
         *          ConstroiBotoesFormulario( { "type=button", "value=Exibir", "id", "onclick=funcaoChamada()" } )
         *          ConstroiBotoesFormulario( { "type=submit", "value=Incluir", "id", "onclick=funcaoChamada()" } )
         *
         *          "type" pode ser: submit, button
         *          "value" pode ser qualquer valor/texto
         *          "função" pode ser: onclick=return funcao(params),
         *                             onexit=funcao(params),
         *                             onchange=funcao(params),
         *                             onblur=funcao(params)    etc.
         *
         *          "setas" podem ser 0 = nenhuma,
         *                            1 = esquerda e direita
         *                            2 = esquerda, direita, acima, abaixo
         */

        public static string[] ConstroiBotoesFormulario(Tuple<string, string, string, string>[]? Botoes = null, int setas = 0)
        {
            string[] setasIn = new[]
            {
            "<div class='circulo'><div class='bloco_setas'>",
            "   <a href='#'           name='esquerda' class='seta-esquerda' id='esquerda' onclick=''></a>",
            "   <a href='#top'        name='acima'    class='seta-cima'     id='acima'    onclick=''></a>",
            "   <a href='#mypagedown' name='abaixo'   class='seta-baixo'    id='abaixo'   onclick=''></a>",
            "   <a href='#'           name='direita'  class='seta-direita'  id='direita'  onclick=''></a>",
            "</div></div>"
         };
            string[] buttons;
            if (Botoes is null)
            {
                /* se vier nulo, mandamos o padrão, sem setas */
                buttons = new[]
                {
                   "<input type='submit' name='b1' class='subbotao' value='Incluir' id='1' onclick='alert(this.value)' />",
                   "<input type='button' name='b2' class='subbotao' value='Excluir' id='2' onclick='alert(this.value)' />",
                   "<input type='button' name='b3' class='subbotao' value='Folhear' id='3' onclick='alert(this.value)' />"
               };
            }
            else
            {
                int x = 0;
                buttons = (setas == 1) ? new string[Botoes.Length + 4] :
                          (setas == 2) ? new string[Botoes.Length + 6] : new string[Botoes.Length];

                foreach (Tuple<string, string, string, string> parametro in Botoes)
                {
                    buttons[x] = string.Concat("<input name='b", x.ToString(),
                                               "' class='subbotao' type='", parametro.Item1,
                                               "' value='", parametro.Item2, "' ",
                                               "' id='", parametro.Item3, "' ",
                                               parametro.Item4, " />");
                    x++;
                }
                /* contruindo as setas */
                if (setas > 0) buttons[x] = setasIn[0];
                if (setas == 1 || setas == 2)
                {
                    x++; buttons[x] = setasIn[1];   // esquerda
                    x++; buttons[x] = setasIn[4];   // direita
                }
                if (setas == 2)
                {
                    x++; buttons[x] = setasIn[2];   // acima
                    x++; buttons[x] = setasIn[3];   // abaixo
                }
                x++;
                if (setas > 0) buttons[x] = setasIn[5];
            }
            return buttons;
        }

        public static DateTime FormataData(this string data, string patternData = "MM-dd-yyyy", bool modoData = true)
        {
            //a data pode vir em qualquer formato e ser convertida no formato do sistema
            int posDia = patternData.IndexOf("dd");
            int posMes = patternData.IndexOf("MM");
            int posAno = patternData.IndexOf("yyyy");
            data = data.Substring(posDia, 2) + "/" + data.Substring(posMes, 2) + "/" + data.Substring(posAno, 4);
            return Convert.ToDateTime(data);
        }

        /* Método para formatar as datas DYNAMIC das Views USO: FormatarData(ItemDataNaView) */

        public static dynamic FormataData(dynamic data, bool modoData = false, string patternData = "dd/MM/yyyy HH:mm:ss")
        {
            int posDia = patternData.IndexOf("dd");
            int posMes = patternData.IndexOf("MM");
            int posAno = patternData.IndexOf("yyyy");
            int posHor = patternData.IndexOf("HH");
            int posMin = patternData.IndexOf("mm");
            int posSec = patternData.IndexOf("ss");
            if (modoData == true)
                return data.ToString().Substring(posDia, 2) + "/" + data.ToString().Substring(posMes, 2) + "/" + data.ToString().Substring(posAno, 4) + " " +
                       data.ToString().Substring(posHor, 2) + ":" + data.ToString().Substring(posMin, 2) + ":" + data.ToString().Substring(posSec, 2);
            else
                return data.ToString().Substring(posDia, 2) + "/" + data.ToString().Substring(posMes, 2) + "/" + data.ToString().Substring(posAno, 4);
        }

        public static dynamic RetornaSN(this object? valor, bool compacto = true)
        {
            return valor switch
            {
                0 => compacto ? "N" : "Não",
                1 => compacto ? "S" : "Sim",
                "S" => 1,
                "N" => 0,
                null => compacto ? "N" : "Não", // Se for nulo
                _ => compacto ? "N" : "Não"    // Para código não mapeado
            };
        }

        /* Retorna um texto enviado como parâmetro, para qualquer valor numérico ou datetime que esteja vazio ou nulo,
         * caso contrário, retorna o próprio valor mas como tipo string.
         */

        public static string? RetornaTextoQuandoNullVazio(this object? valor, string texto = "")
        {
            string? ret = (valor == null) ? string.Empty : valor.ToString();

            if (string.IsNullOrEmpty(ret) && !string.IsNullOrEmpty(texto))
            {
                return texto;
            }
            return ret;
        }

        //Converte Stream em um array de Bytes[]
        public static byte[] GetByteArray(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static string LimitaString(this string value, int length)
        {
            if (value != null && value.Length > length)
                return value[..length];      //forma simplificada de value.Substring(0, lenght);
            return String.Empty;
        }

        /* Preenche a String com o caractere passado até completar o tamanho definido
         * <param name="value"> Valor em String principal a ser alterado</param>
         * <param name="length"> Tamanho da string final</param>
         * <param name="ch">Caracter para completar os espaços vazios</param>
         * <param name="left">Indica se irá completar a esquerda</param>
         * <returns>Retorna a String alterada</returns>
         */

        private static string CompleteString(this string value, int length, char ch, bool left = true)
        {
            if (value == null)
            {
                return CompleteString("", length, ch, left);
            }
            if (value.Length > length)
            {
                if (left)
                {
                    int len = value.Length;
                    return value.Substring(len - length, length);
                }
                else
                {
                    return value.Substring(0, length);
                }
            }
            else
            {
                if (value.Length < length)
                {
                    if (left)
                        return value.PadLeft(length, ch);
                    else
                        return value.PadRight(length, ch);
                }
            }
            return value;
        }

        public static string FillZero(this string value, int length, bool left = true)
        {
            return CompleteString(value, length, '0', left);
        }

        public static string FillZero(this long value, int length, bool left = true)
        {
            return CompleteString(value.ToString(), length, '0', left);
        }

        public static string FillZero(this int value, int length, bool left = true)
        {
            return CompleteString(value.ToString(), length, '0', left);
        }

        //Retorna a lista de mensagens do Exception e:
        public static List<string> GetFullErrorMessage(Exception e)
        {
            List<string> listError = [e.Message];
            Exception? inner = e.InnerException;
            while (inner != null)
            {
                listError.Add(inner.Message);
                inner = inner.InnerException;
            }
            return listError;
        }

        //Valida Telefone celular
        public static bool ValidarTelefoneCelular(string telefone)
        {
            static string AddDigito9Telefone(string telefone)
            {
                telefone = DesformataCamposNumericosComMascara(telefone);
                if (telefone.Length < 11)
                {
                    string auxTel = telefone;
                    auxTel = auxTel.Substring(0, 2) + "9" + auxTel.Substring(2);
                    return auxTel;
                }
                return telefone;
            }

            if (string.IsNullOrEmpty(telefone))
            {
                return false;
            }
            string phone = Regex.Replace(telefone, @"[^0-9]", "");
            phone = phone.Length > 12 ? Regex.Replace(phone, @"^55", "") : phone;

            if (phone.Length < 11)
            {
                phone = AddDigito9Telefone(phone);
            }

            // https://www.anatel.gov.br/setorregulado/plano-de-numeracao-brasileiro?id=330
            // Telefone Móvel YY 9XXXX XXXX

            //https://www.anatel.gov.br/setorregulado/plano-de-numeracao-brasileiro?id=334
            // SME  YY 7XXX XXXX
            return Regex.IsMatch(phone, @"(^[1-9]{2}9[0-9]{8}$)|(^[1-9]{2}7[0-9]{7}$)");
        }

        //Valida Email
        public static bool ValidaEmail(this string email)
        {
            //começa validando o e-mail com regular expression
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                return false;
            }
            return true;
        }

        public static string RetornaEmailValidado(this string email)
        {
            //começa validando o e-mail com regular expression
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                return "Email inválido";
            }
            return email;
        }

        public static bool ValidaPisPasep(string pisPasep)
        {
            int[] multiplicador = new int[10] { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma;
            int resto;

            if (pisPasep.Trim().Length == 0) return false;
            if (pisPasep.Trim() == "0") return false;

            pisPasep = pisPasep.Trim();
            pisPasep = pisPasep.Replace("-", "").Replace(".", "").PadLeft(11, '0');

            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(pisPasep[i].ToString()) * multiplicador[i];

            resto = soma % 11;

            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            return pisPasep.EndsWith(resto.ToString());
        }

        public static bool ValidarCEI(object value)
        {
            int[] peso = { 7, 4, 1, 8, 5, 2, 1, 6, 3, 7, 4 };
            string[] invalidos = new string[] { "000000000000", "111111111111", "222222222222", "333333333333",
                                                "444444444444", "555555555555", "666666666666", "777777777777",
                                                "888888888888", "999999999999" };

            string cei = (string)value;
            if (cei.Length != 12 || invalidos.Contains(cei) || cei.Substring(0, 10).Equals("0000000000")) { return false; }

            char[] algarismos = cei.ToCharArray();
            int soma = 0;

            for (int i = 0; i < 11; i++)
            {
                soma += peso[i] * Convert.ToInt32(algarismos[i]);
            }

            int unidadeSoma = Convert.ToInt32(soma.ToString().Substring(soma.ToString().Length - 1));
            int dezenaSoma = Convert.ToInt32(soma.ToString().Substring(soma.ToString().Length - 2));
            int soma2 = unidadeSoma + dezenaSoma;
            int unidadeSoma2 = Convert.ToInt32(soma.ToString().Substring(soma.ToString().Length - 1));
            int subtracao = 10 - unidadeSoma2;
            int unidadeSubtracao = Convert.ToInt32(soma.ToString().Substring(soma.ToString().Length - 1));
            if (unidadeSubtracao == Convert.ToInt32(algarismos[11]))
            {
                return true;
            }
            return false;
        }

        public static bool ValidarCNPJ(string cnpj)
        {
            int[] mt1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] mt2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma; int resto; string digito; string TempCNPJ;

            cnpj = cnpj.Trim();
            cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");

            if (cnpj.Length != 14)
                return false;

            if (cnpj == "00000000000000" || cnpj == "11111111111111" ||
                cnpj == "22222222222222" || cnpj == "33333333333333" ||
                cnpj == "44444444444444" || cnpj == "55555555555555" ||
                cnpj == "66666666666666" || cnpj == "77777777777777" ||
                cnpj == "88888888888888" || cnpj == "99999999999999")
                return false;

            TempCNPJ = cnpj.Substring(0, 12);
            soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(TempCNPJ[i].ToString()) * mt1[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = resto.ToString();

            TempCNPJ = TempCNPJ + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(TempCNPJ[i].ToString()) * mt2[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }

        private static bool ValidarCNS_DEFINITIVO(string vlrCNS)
        {
            int soma = 0;
            int resto = 0;
            int dv = 0;
            string pis = "";
            string resultado = "";

            pis = vlrCNS.Substring(0, 11);
            soma = (Convert.ToInt32(pis.Substring(0, 1)) * 15) +
                    (Convert.ToInt32(pis.Substring(1, 1)) * 14) +
                    (Convert.ToInt32(pis.Substring(2, 1)) * 13) +
                    (Convert.ToInt32(pis.Substring(3, 1)) * 12) +
                    (Convert.ToInt32(pis.Substring(4, 1)) * 11) +
                    (Convert.ToInt32(pis.Substring(5, 1)) * 10) +
                    (Convert.ToInt32(pis.Substring(6, 1)) * 9) +
                    (Convert.ToInt32(pis.Substring(7, 1)) * 8) +
                    (Convert.ToInt32(pis.Substring(8, 1)) * 7) +
                    (Convert.ToInt32(pis.Substring(9, 1)) * 6) +
                    (Convert.ToInt32(pis.Substring(10, 1)) * 5);
            resto = soma % 11;
            dv = 11 - resto;
            if (dv == 11) dv = 0;

            if (dv == 10)
            {
                soma = (Convert.ToInt32(pis.Substring(0, 1)) * 15) +
                        (Convert.ToInt32(pis.Substring(1, 1)) * 14) +
                        (Convert.ToInt32(pis.Substring(2, 1)) * 13) +
                        (Convert.ToInt32(pis.Substring(3, 1)) * 12) +
                        (Convert.ToInt32(pis.Substring(4, 1)) * 11) +
                        (Convert.ToInt32(pis.Substring(5, 1)) * 10) +
                        (Convert.ToInt32(pis.Substring(6, 1)) * 9) +
                        (Convert.ToInt32(pis.Substring(7, 1)) * 8) +
                        (Convert.ToInt32(pis.Substring(8, 1)) * 7) +
                        (Convert.ToInt32(pis.Substring(9, 1)) * 6) +
                        (Convert.ToInt32(pis.Substring(10, 1)) * 5) + 2;
                resto = soma % 11;
                dv = 11 - resto;
                resultado = pis + "001" + dv.ToString();
            }
            else
            {
                resultado = pis + "000" + dv.ToString();
            }
            if (vlrCNS != resultado)
                return false;
            else
                return true;
        }

        private static bool ValidarCNS_PROVISORIO(string vlrCNS)
        {
            string pis = "";
            int resto = 0;
            int soma = 0;

            pis = vlrCNS.Substring(0, 15);

            if (pis == "") return false;

            if ((vlrCNS.Substring(0, 1) != "7") && (vlrCNS.Substring(0, 1) != "8") && (vlrCNS.Substring(0, 1) != "9"))
            {
                return false;
            }

            soma = (Convert.ToInt32(pis.Substring(0, 1), 10) * 15)
                    + (Convert.ToInt32(pis.Substring(1, 1), 10) * 14)
                    + (Convert.ToInt32(pis.Substring(2, 1), 10) * 13)
                    + (Convert.ToInt32(pis.Substring(3, 1), 10) * 12)
                    + (Convert.ToInt32(pis.Substring(4, 1), 10) * 11)
                    + (Convert.ToInt32(pis.Substring(5, 1), 10) * 10)
                    + (Convert.ToInt32(pis.Substring(6, 1), 10) * 9)
                    + (Convert.ToInt32(pis.Substring(7, 1), 10) * 8)
                    + (Convert.ToInt32(pis.Substring(8, 1), 10) * 7)
                    + (Convert.ToInt32(pis.Substring(9, 1), 10) * 6)
                    + (Convert.ToInt32(pis.Substring(10, 1), 10) * 5)
                    + (Convert.ToInt32(pis.Substring(11, 1), 10) * 4)
                    + (Convert.ToInt32(pis.Substring(12, 1), 10) * 3)
                    + (Convert.ToInt32(pis.Substring(13, 1), 10) * 2)
                    + (Convert.ToInt32(pis.Substring(14, 1), 10) * 1);

            resto = soma % 11;

            if (resto == 0)
                return true;
            else
                return false;
        }

        public static bool ValidarCNS(string vlrCNS)
        {
            string justNumbers = new(vlrCNS.Where(char.IsDigit).ToArray());
            int tamCNS = justNumbers.Length;

            if (tamCNS != 15) return false;

            if (justNumbers == "000000000000000") return false;

            if (!ValidarCNS_PROVISORIO(justNumbers))
                return ValidarCNS_DEFINITIVO(justNumbers);

            return true;
        }

        public static bool ValidarCPF(long CPF)
        {
            int d1, d2;
            int soma = 0;
            string digitado = "";
            string calculado = "";
            // Pega a string passada no parametro
            //int tamanho = CPF.ToString().Length;
            string cpf = CPF.ToString().PadLeft(11, '0');
            // Pesos para calcular o primeiro digito
            int[] peso1 = new int[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            // Pesos para calcular o segundo digito
            int[] peso2 = new int[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] n = new int[11];
            if (cpf.Length != 11) return false;

            // Caso coloque todos os numeros iguais
            string[] invalidos = new string[] { "00000000000", "11111111111", "22222222222", "33333333333",
                                                "44444444444", "55555555555", "66666666666", "77777777777",
                                                "88888888888", "99999999999" };

            if (invalidos.Contains(cpf)) return false;

            try
            {
                // Quebra cada digito do CPF
                n[0] = Convert.ToInt32(cpf.Substring(0, 1));
                n[1] = Convert.ToInt32(cpf.Substring(1, 1));
                n[2] = Convert.ToInt32(cpf.Substring(2, 1));
                n[3] = Convert.ToInt32(cpf.Substring(3, 1));
                n[4] = Convert.ToInt32(cpf.Substring(4, 1));
                n[5] = Convert.ToInt32(cpf.Substring(5, 1));
                n[6] = Convert.ToInt32(cpf.Substring(6, 1));
                n[7] = Convert.ToInt32(cpf.Substring(7, 1));
                n[8] = Convert.ToInt32(cpf.Substring(8, 1));
                n[9] = Convert.ToInt32(cpf.Substring(9, 1));
                n[10] = Convert.ToInt32(cpf.Substring(10, 1));
            }
            catch
            {
                return false;
            }
            // Calcula cada digito com seu respectivo peso
            for (int i = 0; i <= peso1.GetUpperBound(0); i++)
            {
                soma += peso1[i] * Convert.ToInt32(n[i]);
            }
            // Pega o resto da divisao
            int resto = soma % 11;
            if (resto == 1 || resto == 0)
                d1 = 0;
            else
                d1 = 11 - resto;

            soma = 0;
            // Calcula cada digito com seu respectivo peso
            for (int i = 0; i <= peso2.GetUpperBound(0); i++)
            {
                soma += peso2[i] * Convert.ToInt32(n[i]);
            }
            // Pega o resto da divisao
            resto = soma % 11;
            if (resto == 1 || resto == 0)
                d2 = 0;
            else
                d2 = 11 - resto;

            calculado = d1.ToString() + d2.ToString();
            digitado = n[9].ToString() + n[10].ToString();

            // Se os últimos dois digitos calculados for igual aos dois últimos dígitos do cpf entao é válido
            if (calculado == digitado) return true;

            return false;
        }

        public static bool ValidarDeclaracaoNascidoVivo(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                char firstChar = value[0];
                return value.Any(c => c != firstChar);
            }
            return false;
        }

        public static bool ValidarCEP(string cep)
        {
            if (cep.Length == 8)
            {
                cep = cep.Substring(0, 5) + "-" + cep.Substring(5, 3);
            }
            return Regex.IsMatch(cep, "[0-9]{5}-[0-9]{3}");
        }

        public static string CPFSemFormatacao(this string CPF)
        {
            if (string.IsNullOrEmpty(CPF)) return string.Empty;
            else
                return Regex.Replace(CPF, @"[^\d]", "");
        }

        public static string CNPJSemFormatacao(this string CNPJ)
        {
            if (string.IsNullOrEmpty(CNPJ)) return string.Empty;
            else
                return Regex.Replace(CNPJ, @"[^\d]", "");
        }

        public static string DesformataCamposNumericosComMascara(string value)
        {
            if (!string.IsNullOrEmpty(value))
                return Regex.Replace(value, @"[^\d]", "");

            return value;
        }

        public static string RetornaValorParcelaBrasileira(this decimal value)
        {
            return string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:0.00}", value);
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

        public static string CortaNoTamanhoMaximo(this string valor, int tam = 1)
        {
            if (string.IsNullOrEmpty(valor)) return "";
            tam = (tam < 1) ? 1 : tam;
            return (tam > valor.Trim().Length) ? valor.Trim() : valor.Trim().Substring(0, tam);
        }

        public static bool ValidaData(this string data)
        {
            DateOnly dataOnly;
            if (DateTime.TryParse(data, out DateTime dataTemp))
            { }
            else
            {
                try
                {
                    dataOnly = DateOnly.FromDateTime(Convert.ToDateTime(data));
                }
                catch (Exception e)
                {
                    string error = e.Message;
                    return false;
                }
                finally { }
            }
            return true;
        }

        /* Retorna formatada a data lida da base de dados que está no formato yyyy/MM/dd HH:mm:ss */

        public static DateTime DateFromDataBase(DateTime date, DateTime hour)
        {
            return new DateTime(date.Year, date.Month, date.Day, hour.Hour, hour.Minute, hour.Second);
        }

        /* Retorna formatado o horário lido da base de dados que está no formato HH:mm:ss */

        public static string HourFromDataBase(this DateTime date)
        {
            string horario = date.Hour.ToString() + ":" + date.Minute.ToString() + ":" + date.Second.ToString();
            return horario;
        }

        public static DateTime RetornaDataMinimaDefault()
        {
            return SqlDateTime.MinValue.Value;
        }

        public static DateTime RetornaDataMaximaDefault()
        {
            return SqlDateTime.MaxValue.Value;
        }

        public static string DataNula(this string data)   /* Tratamento Data NULA para algumas versões de Entity */
        {
            /* Por que "01010001"?
             * RESP: parece que em algumas versões do Entity, uma data qualquer ao ser tratada com
             *       GetValueOrDefault().ToString("ddMMyyyy") e a mesma for NULL, então ela NÃO retornará null mas sim: "01010001".
             */
            if (data == null || data == string.Empty || data == "01010001")  // a data NULL pode vir = 01010001
                return string.Empty;
            else
                return data;
        }

        /// Deleta um arquivo já existente
        /// <param name="filename">Nome completo do arquivo (caminho + nome + extensão)</param>
        /// <param name="baseName">Nome do arquivo sem extensão</param>
        public static void DeleteFile(string filename, string baseName)
        {
            try
            {
                File.Delete(filename);
            }
            catch
            {
                //caso o arquivo esteja aberto é preciso forçar e fechá-lo para depois fazer a sua exclusão
                foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcesses())
                {
                    if (process.MainWindowTitle.Contains(baseName))
                    {
                        process.Kill();

                        bool stop = false;
                        System.Timers.Timer timer = new(1000);//milissegundos
                        timer.Elapsed += (sender, e) =>
                        {
                            stop = true;
                        };
                        timer.Start();

                        if (stop)
                            break;
                    }
                }
                File.Delete(filename);
            }
            finally
            { }
        }

        public static string Formatar(string texto, string mascara)
        {
            if (texto.Trim().Length == 0 || mascara.Length == 0)
                return texto;

            // Obter separadores.
            char[] Separadores = mascara.Replace("#", string.Empty).ToCharArray();
            // Quebra máscara por grupo.
            string[] Grupos = mascara.Split(Separadores);

            // Prepara formato.
            string Formato = "^";
            foreach (string grp in Grupos)
                Formato += string.Format(@"(\d{{{0}}})", grp.Length.ToString());

            // Cria objeto de expressão regular.
            Regex r = new(Formato);
            Match m = r.Match(texto);

            // Montar retorno.
            string Retorno = string.Empty;
            for (int i = 1; i <= m.Groups.Count; i++)
            {
                Retorno += m.Groups[i].Value;
                if (i < m.Groups.Count - 1)
                    Retorno += Separadores[(i - 1) < Separadores.Length ? (i - 1) : Separadores.Length - 1];
            }
            if (Retorno.Equals(string.Empty))
                return texto;
            return Retorno;
        }

        public static string FormatarCEP(this string cep)
        {
            ArgumentNullException.ThrowIfNull(cep);   //não aceita CEP nulo
            cep = cep.Trim();

            if (cep.Length == 0 || cep.All(c => c == '0'))
                throw new ArgumentException("O valor informado para o CEP é inválido.", nameof(cep));

            string temp = cep.Length < 8 ? cep.PadLeft(8, '0') : cep;
            return Formatar(temp, "#####-###");
        }

        public static string FormatarContaExame(this string? conta)
        {
            if (conta == null) return string.Empty;

            if (!conta.Equals(""))
            {
                string Temp = conta.Length < 11 ? conta.ToString().PadLeft(11, '0') : conta;
                return Formatar(Temp, "##.##.###.####");
            }
            return conta;
        }

        public static string FormatarContaExameSem11(this string? conta)
        {
            if (conta == null) return string.Empty;

            if (!conta.Equals(""))
            {
                string Temp = conta.Length < 11 ? conta.ToString().PadLeft(11, '0').Substring(2, 9) : conta.Substring(2, 9);
                return Formatar(Temp, "##.###.####");
            }
            return conta;
        }

        public static string? FormatarCPF(this string? cpfFormatar)
        {
            if (cpfFormatar == null)
                return string.Empty;

            if (!cpfFormatar.Equals(""))
            {
                string Temp = cpfFormatar.Length < 11 ? cpfFormatar.ToString().PadLeft(11, '0') : cpfFormatar;
                return Formatar(Temp, "###.###.###-##");
            }
            return cpfFormatar;
        }

        public static string FormatarCNPJNotNull(this string cnpjFormatar)
        {
            if (!cnpjFormatar.Equals(""))
            {
                string Temp = cnpjFormatar.Length < 14 ? cnpjFormatar.ToString().PadLeft(14, '0') : cnpjFormatar;
                return Formatar(Temp, "##.###.###/####-##");
            }
            return cnpjFormatar;
        }

        public static string? FormatarCNPJ(this string? cnpjFormatar)
        {
            if (cnpjFormatar == null)
                return string.Empty;

            if (!cnpjFormatar.Equals(""))
            {
                string Temp = cnpjFormatar.Length < 14 ? cnpjFormatar.ToString().PadLeft(14, '0') : cnpjFormatar;
                return Formatar(Temp, "##.###.###/####-##");
            }
            return cnpjFormatar;
        }

        /* Método para formatar CNPJ DYNAMIC das Views USO: FormatarData(ItemCNPJnaView) */

        public static dynamic FormatarCNPJ(dynamic cnpjFormatar)
        {
            if (!cnpjFormatar.Equals(""))
            {
                dynamic Temp = cnpjFormatar.ToString().Length < 14 ? cnpjFormatar.ToString().PadLeft(14, '0') : cnpjFormatar;
                return Formatar(Temp.ToString(), "##.###.###/####-##");
            }
            return cnpjFormatar;
        }

        /* Método para formatar CPF DYNAMIC das Views USO: FormatarData(ItemCPFnaView) */

        public static dynamic FormatarCPF(dynamic cpfFormatar)
        {
            if (!cpfFormatar.Equals(""))
            {
                dynamic Temp = cpfFormatar.ToString().Length < 11 ? cpfFormatar.ToString().PadLeft(11, '0') : cpfFormatar;
                return Formatar(Temp.ToString(), "###.###.###-##");
            }
            return cpfFormatar;
        }

        /* Formata Telefone de acordo com o tamanho do número nacional e internacional, e pode retornar vazio ou nulo */

        public static string? FormataTelefone(this string? tel)
        {
            if (string.IsNullOrEmpty(tel)) return "";
            tel = tel.TrimStart('0');  //retira zeros à esquerda
            tel = tel.TrimStart('+');  //retira + à esquerda

            long number = Convert.ToInt64(tel);
            int tam = tel.Length;
            string fmt = "";
            if (tam == 8) fmt = "0000-0000";
            if (tam == 10) fmt = "(00) 0000-0000";
            if (tam == 11) fmt = "(00) 0-0000-0000";
            if (tam == 12) fmt = "+0 (00) 0-0000-0000";      //internacional
            if (tam == 13) fmt = "+00 (00) 0-0000-0000";     //internacional
            if (tam == 14) fmt = "+00 (000) 0-0000-0000";    //internacional
            if (tam == 15) fmt = "+00 (0000) 0-0000-0000";   //internacional
            if (tam > 15) return tel;

            return number.ToString(fmt);
        }

        /* Formata Telefone de acordo com o tamanho do número nacional e internacional, não pode ser nulo */

        public static string FormataTelefoneNotNull(this string tel)
        {
            tel = tel.TrimStart('0');  //retira zeros à esquerda
            tel = tel.TrimStart('+');  //retira + à esquerda

            long number = Convert.ToInt64(tel);
            int tam = tel.Length;
            string fmt = "";
            if (tam == 8) fmt = "0000-0000";
            if (tam == 10) fmt = "(00) 0000-0000";
            if (tam == 11) fmt = "(00) 0-0000-0000";
            if (tam == 12) fmt = "+0 (00) 0-0000-0000";      //internacional
            if (tam == 13) fmt = "+00 (00) 0-0000-0000";     //internacional
            if (tam == 14) fmt = "+00 (000) 0-0000-0000";    //internacional
            if (tam == 15) fmt = "+00 (0000) 0-0000-0000";   //internacional
            if (tam > 15) return tel;

            return number.ToString(fmt);
        }

        /* Retorna apenas números de uma string qualquer que contenha números */

        public static string ApenasNumeros(this string? valor)
        {
            if (string.IsNullOrEmpty(valor))
                return string.Empty;

            // Remove tudo que não for número
            return new string(valor.Where(char.IsDigit).ToArray());
        }

        /* Converte uma data String no formato dd/mm/aaaa para o tipo DateTime.
         * <param name="dataString">Data string no formato dd/mm/aaaa</param>
         * <param name="start">Gerar data com horário inicial ou final</param>
         * <returns></returns>
         */

        public static DateTime GetDateFromString(string dateString, bool start = true)
        {
            short dia = Convert.ToInt16(dateString.Substring(0, 2));
            short mes = Convert.ToInt16(dateString.Substring(2, 2));
            int ano = Convert.ToInt32(dateString.Substring(4, 4));

            if (start)
                return new DateTime(ano, mes, dia, 0, 0, 0);
            else
                return new DateTime(ano, mes, dia, 23, 59, 59);
        }

        public static string GetEscapedString(string value)
        {
            string caracteresAcentuados = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇç";
            string caracteresSemAcentos = "AAAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUuuuuCc";
            string caracteresNaoPermitidos = "'!@#$%¨&*()+=§{}[]ªº/\\?°|´`~^\"";

            if (!string.IsNullOrEmpty(value))
            {
                StringBuilder str = new();
                foreach (char c in value.ToCharArray())
                {
                    if (!Char.IsControl(c))
                        str.Append(c);
                }
                for (int i = 0; i < caracteresAcentuados.Length; i++)
                {
                    str = str.Replace(caracteresAcentuados[i].ToString(), caracteresSemAcentos[i].ToString());
                }
                for (int i = 0; i < caracteresNaoPermitidos.Length; i++)
                {
                    str = str.Replace(caracteresNaoPermitidos[i].ToString(), "");
                }
                str.Replace("amp;", "");

                return str.ToString();
            }
            return value;
        }

        /* Converte Base64 para String */

        public static string Base64Decode(string base64EncodedData)
        {
            byte[] base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /* Converte String para a Base64 */

        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string CalcularIdade(DateTime DataDeNascimento)
        {
            int Anos = DateTime.Now.Year - DataDeNascimento.Year;
            int Meses = 0, Dias = 0;
            if (DataDeNascimento.Month > DateTime.Now.Month)
            {
                Anos--;
                Meses = 12 - DateTime.Now.Month;
            }
            else if (DateTime.Now.Month == DataDeNascimento.Month && DateTime.Now.Day < DataDeNascimento.Day)
            {
                Anos--;
            }
            else
            {
                Meses = DateTime.Now.Month - DataDeNascimento.Month;
            }
            if (DateTime.Now.Day < DataDeNascimento.Day)
                Dias = 30 - (DataDeNascimento.Day - DateTime.Now.Day);
            else
                Dias = DateTime.Now.Day - DataDeNascimento.Day;

            string Idade = Anos > 0 ? (Anos + " anos") : "";
            if (Meses > 0 && Dias > 0)
                Idade = Idade + "-" + Meses + (Meses > 1 ? " meses" : " mês") + "-" + Dias + (Dias > 1 ? " dias" : " dia");
            else if (Meses == 0 && Dias > 0)
                Idade = Idade + "-" + Dias + (Dias > 1 ? " dias" : " dia");
            else if (Meses > 0 && Dias == 0)
                Idade = Idade + "-" + Meses + (Meses > 1 ? " meses" : " mês");

            return Idade;
        }

        public static DateTime CalcularProximoDiaUtil(DateTime dataBase, List<FeriadoDetalhe> feriados, int diasUteisDesejados)
        {
            DateTime dataAtual = dataBase;
            int diasContados = 0;

            for (int i = 0; i <= 30; i++)
            {
                if (diasUteisDesejados > 0)
                    dataAtual = dataAtual.AddDays(1);

                bool ehFimDeSemana = dataAtual.DayOfWeek == DayOfWeek.Saturday || dataAtual.DayOfWeek == DayOfWeek.Sunday;
                bool ehFeriado = feriados.Any(f => f.Dia == dataAtual.Day && f.Mes == dataAtual.Month && f.Ano == dataAtual.Year);

                if (!ehFimDeSemana && !ehFeriado)
                {
                    if (diasUteisDesejados > 0)
                    {
                        diasContados++;
                        if (diasContados == diasUteisDesejados)
                            break;
                    }
                    else
                    {
                        break;
                    }
                }

                if (diasUteisDesejados <= 0)
                    dataAtual = dataAtual.AddDays(1);
            }

            return dataAtual;
        }

        public static Task<string> RetornaTextoDeArquivoHtml(this string pathToHtmlFile)
        {
            using FileStream fs = File.OpenRead(pathToHtmlFile);
            using StreamReader sr = new(fs);
            return sr.ReadToEndAsync();
        }

        public static async Task<string> RetornaTextoDeArquivoHtmlAsync(this string pathToHtmlFile)
        {
            using FileStream fs = File.OpenRead(pathToHtmlFile);
            using StreamReader sr = new(fs);
            return await sr.ReadToEndAsync();
        }

        /* Recuperamos neste método a lista de todos os campos que estão no formulário, e
         * podemos retornar o array de apenas uma das listas identificadas (exemplo: "dados")
         */

        private static ICollection<(string, string)> RetornaListaIFormCollection(IFormCollection fc, string parametroFormulario = "dados")
        {
            /*  Outros exemplos magníficos de IFormCollection:  https://csharp.hotexamples.com/examples/-/IFormCollection/-/php-iformcollection-class-examples.html */
            //EXEMPLO de parâmetro existente no formulário cshtml: @Html.Hidden("dados", new { Email=@ViewBag.Itens.Email, Nome=@ViewBag.Itens.NomeCompleto })
            //var fcDados = fc["dados"].ToList();  //recupera a lista (IFormCollection fc), mas eu faço na forma abaixo...
            //var fcDados = FormCollectionToJson(fc);  //recupera a lista (IFormCollection fc), mas eu faço na forma abaixo...
            //
            ICollection<(string, string)> colecao = [];
            //var dados = new List<object>();
            //Request.Form.TryGetValue(parametroFormulario, out lista);  //requisitando toda a lista de dados do último formulário executado em tela.
            fc.TryGetValue(parametroFormulario, out StringValues lista);
            string res = string.Empty;
            //Primeiro tratamento, vai limpar alguns caracteres indesejados.
            foreach (string? item in lista)
            {
                res = item != null ? item.Replace("{", "").Replace("}", "").TrimStart().TrimEnd() : "null";
                break;
            }
            //Segundo tratamento, vai arrumar o array e montar a coleção de retorno.
            string[] arr = res.Split(",");
            foreach (string item in arr)
            {
                string[] converte = item.Split("=");
                if (parametroFormulario != "dados")
                    colecao.Add((parametroFormulario, converte[0].Trim()));
                else
                    colecao.Add((converte[0].Trim(), converte[1].Trim()));
            }
            return colecao;
        }

        /* "IFormCollection": Retorna o valor de um campo, que está numa lista contendo itens, ou apenas num campo solitário do formulário.
        * Basta informar o nome do campo do formulário, ou informar a lista de dados de um Hidden qualquer que o valor será retornado!
        * Exemplos: string email = "Email".RetornaValorDoFormulario(fc);  retorna um email de uma lista tal e qual esta: "{ Email = rguilemond@gmail.com, Nome = Ricardo, Idade = 57 }"
        *           string nome = "Nome".RetornaValorDoFormulario(fc);  retorna o nome de um campo/box solitário/único citado no formulário.
        */

        public static string RetornaValorDoFormulario(this string campo, IFormCollection fc)
        {
            if (string.IsNullOrWhiteSpace(campo) || fc == null)
                return string.Empty;

            static string RetornaStringIFormCollection(IEnumerable<(string, string)> listaDados, string busca)
            {
                return listaDados.FirstOrDefault(item =>
                    string.Equals(item.Item1, busca, StringComparison.OrdinalIgnoreCase)).Item2 ?? string.Empty;
            }

            string? dados = fc["dados"].FirstOrDefault();
            bool contemNaLista = !string.IsNullOrEmpty(dados) &&
                                 (dados.Contains($"{campo} = ") || dados.Contains($"{campo}="));

            ICollection<(string, string)> lista = contemNaLista
                ? RetornaListaIFormCollection(fc)
                : RetornaListaIFormCollection(fc, campo);

            return RetornaStringIFormCollection(lista, campo);
        }

        public static string RetornaValorDoFormulario(this string campo, List<string> dados)
        {
            if (string.IsNullOrWhiteSpace(campo) || dados == null || dados.Count == 0)
                return string.Empty;

            foreach (string linha in dados)
            {
                if (string.IsNullOrWhiteSpace(linha))
                    continue;

                // Remove possíveis chaves externas e quebra em pares separados por vírgula
                string[] campos = linha.Replace("{", "")
                                  .Replace("}", "")
                                  .Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (string trecho in campos)
                {
                    string[] partes = trecho.Split(new[] { '=', ':' }, 2, StringSplitOptions.TrimEntries);

                    if (partes.Length == 2 &&
                        string.Equals(partes[0], campo, StringComparison.OrdinalIgnoreCase))
                    {
                        return partes[1];
                    }
                }
            }
            return string.Empty;
        }

        public static bool ConvertStringToBool(this string truefalse)
        {
            return Convert.ToBoolean(truefalse);
        }

        /*
         * Vai retornar a URL montada com o caminho da Action desejada em "novaUrl"
         * EXEMPLO DE USO (Olhar a classe UtilsBase):
         * string redirecionaUrl = "Senhas".MontaUrl(base.HttpContext.Request);
         * onde "Senhas" é a action que se deseja redirecionar!
         */

        public static string NovaUrl(this string urlOriginal, string novaRota)
        {
            if (!Uri.TryCreate(urlOriginal, UriKind.Absolute, out Uri? uriBase))
                return novaRota; // ou string.Empty, ou lançar exceção, dependendo do cenário

            return $"{uriBase.Scheme}://{uriBase.Host}/{novaRota.TrimStart('/')}";
        }

        /* Repete "n" espaços após o texto  */

        public static string TextoEspaco(this string texto, uint quant = 1)
        {
            string espaco = "&nbsp;";
            espaco = string.Concat(Enumerable.Repeat(espaco, (int)quant));
            return string.Concat(texto, espaco);
        }

        public static int ToInt32(this string valor)
        {
            //return Convert.ToInt32(valor);
            if (int.TryParse(valor, out int resultado))
            {
                return resultado;
            }
            return 0;
        }

        public static ulong ToULong(this string valor)
        {
            return (ulong)Int64.Parse(valor);
        }

        public static string[] EliminaItemVazioDoArray(this string[] array)
        {
            if (array.Length > 0)
            {
                ICollection<string> lista = array.ToList().Where(l => !string.IsNullOrEmpty(l.Trim())).ToList();
                array = lista.ToArray();
            }
            return array;
        }

        public static string RemoveAcentos(this string text)
        {
            StringBuilder sbReturn = new();
            char[] arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                    sbReturn.Append(letter);
            }
            return sbReturn.ToString();
        }

        public static string[] RetornaPartesNome(this string Nome)
        {
            static string LimpaCaracteresEspeciais(string texto)
            {
                try
                {
                    if (string.IsNullOrEmpty(texto)) return string.Empty;
                    texto = texto.RemoveAcentos();
                    string expressaoPermitida = "^0-9a-zA-Z ";
                    return Regex.Replace(texto, expressaoPermitida, string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1.5)).Trim();
                }
                catch (RegexMatchTimeoutException)  /* se excedeu um tempo "aceitável" máximo, então retorna o texto sem verificar */
                {
                    return texto.Trim();
                }
            }

            string[] monos = { "da", "do", "de", "dos", "das", "di" };
            try
            {
                string meioNome = string.Empty;
                string segundoNome = string.Empty;
                // Quebrando o nome em partes
                string[] partes = Nome.Trim().Split(' ').ToArray();
                partes = partes.EliminaItemVazioDoArray();
                string primeiroNome = partes[0];
                string sobreNome = partes[partes.Length - 1];
                if (primeiroNome == sobreNome) sobreNome = string.Empty;
                if (partes.Length > 2)
                {
                    meioNome = Nome.Replace(primeiroNome, "").Replace(sobreNome, "").Trim();
                }
                if (meioNome.Length > 1 && monos.Contains(partes[1].ToLower()))
                { }
                else if (meioNome.Length > 2)
                {
                    segundoNome = partes[1];
                    primeiroNome = primeiroNome + " " + segundoNome;
                    meioNome = meioNome.Replace(partes[1], "");
                }
                if (monos.Contains(meioNome.Trim().ToLower()) && meioNome.Length == 2)
                {
                    primeiroNome = primeiroNome + " " + meioNome;
                    meioNome = "";
                }
                return new string[] { LimpaCaracteresEspeciais(primeiroNome).Trim(),
                                      LimpaCaracteresEspeciais(meioNome).Trim(),
                                      LimpaCaracteresEspeciais(sobreNome).Trim()
                                    };
            }
            catch (Exception e)
            {
                string mens = e.Message.ToString();
                return new string[] { Nome, mens, "" };
            }
        }

        public static string LimpaCaracteresEspeciais(this string texto, bool retiraEspacoBranco = false, string expressaoPermitida = "[^0-9A-Za-z ]")
        {
            try
            {
                if (string.IsNullOrEmpty(texto)) return string.Empty;
                texto = texto.RemoveAcentos();
                string espaco = retiraEspacoBranco ? "" : " ";
                return Regex.Replace(texto, expressaoPermitida, espaco, RegexOptions.None, TimeSpan.FromSeconds(1.5)).Trim();
            }
            catch (RegexMatchTimeoutException)  /* se excedeu um tempo "aceitável" máximo, então retorna o texto sem verificar */
            {
                return texto.Trim();
            }
        }

        /* Retorna um campo que veio do formulário, e trata conversões  */

        public static dynamic RetornaValorFormulario(this dynamic[] campo, int array, int posicao, char tipo = 'S')
        {
            dynamic ret = campo[array].Split(',')[posicao];

            if (tipo == 'I')  //integer
                return Convert.ToInt32(ret);
            else
                return ret;
        }

        /* SOBRECARGA DO ORIGINAL MICROSOFT ::: Retorna a extensão pelo nome completo do caminho/arquivo.ext : método de sobrecarga do original Microsoft */

        public static string GetExtension(this string file)
        {
            string ret = GetExtension(file);
            if (string.IsNullOrEmpty(ret)) ret = string.Empty;
            return ret;
        }

        /* SOBRECARGA DO ORIGINAL MICROSOFT ::: Retorna invariável para decimal, independente de ponto ou vírgula que estejam utilizando */

        public static decimal ToDecimalInvariant(this string value)
        {
            return decimal.Parse(value, CultureInfo.InvariantCulture);
        }

        public static string GetCodigoDeErrosHttp(int codigo = 200)  //200=OK
        {
            return codigo switch
            {
                //1xx - Informacional = Indica que a solicitação foi recebida e o processo continua:
                100 => "100 Continue (Continuar)",
                101 => "101 Switching Protocols (Mudando Protocolos)",
                102 => "102 Processing (Processando - WebDAV)",
                103 => "103 Early Hints (Indícios Preliminares)",
                //2xx - Sucesso = Indica que a solicitação foi concluída com sucesso:
                200 => "200 OK (Tudo certo)",
                201 => "201 Created(Criado)",
                202 => "202 Accepted(Aceito)",
                203 => "203 Non - Authoritative Information(Informação Não - Autorizada)",
                204 => "204 No Content(Sem Conteúdo)",
                205 => "205 Reset Content(Conteúdo Reiniciado)",
                206 => "206 Partial Content(Conteúdo Parcial)",
                207 => "207 Multi - Status(Status Múltiplo - WebDAV)",
                208 => "208 Already Reported(Já Reportado -WebDAV)",
                226 => "226 IM Used(IM Utilizado -RFC 3229)",
                //3xx - Redirecionamento = Indica que é necessário mais ação para concluir a solicitação:
                300 => "300 Multiple Choices(Várias Opções)",
                301 => "301 Moved Permanently(Movido Permanentemente)",
                302 => "302 Found(Encontrado)",
                303 => "303 See Other(Veja Outro)",
                304 => "304 Not Modified(Não Modificado)",
                305 => "305 Use Proxy(Usar Proxy -descontinuado)",
                306 => "306 Switch Proxy(Trocar Proxy -não utilizado)",
                307 => "307 Temporary Redirect(Redirecionamento Temporário)",
                308 => "308 Permanent Redirect(Redirecionamento Permanente)",
                //4xx - Erro do Cliente = Indica que houve um problema no lado do cliente:
                400 => "400 Bad Request(Requisição Inválida)",
                401 => "401 Unauthorized(Não Autorizado)",
                402 => "402 Payment Required(Pagamento Necessário -reservado para uso futuro)",
                403 => "403 Forbidden(Proibido)",
                404 => "404 Not Found(Não Encontrado)",
                405 => "405 Method Not Allowed(Método Não Permitido)",
                406 => "406 Not Acceptable(Não Aceitável)",
                407 => "407 Proxy Authentication Required(Autenticação de Proxy Necessária)",
                408 => "408 Request Timeout(Tempo de Requisição Esgotado)",
                409 => "409 Conflict(Conflito)",
                410 => "410 Gone(Indisponível)",
                411 => "411 Length Required(Tamanho Necessário)",
                412 => "412 Precondition Failed(Pré-Condição Falhou)",
                413 => "413 Payload Too Large(Carga Útil Muito Grande)",
                414 => "414 URI Too Long(URI Muito Longo)",
                415 => "415 Unsupported Media Type(Tipo de Mídia Não Suportado)",
                416 => "416 Range Not Satisfiable(Intervalo Não Satisfatório)",
                417 => "417 Expectation Failed(Expectativa Falhou)",
                418 => "418 I'm a teapot (Eu Sou um Bule de Chá - piada do protocolo HTCPCP)",
                421 => "421 Misdirected Request(Requisição Mal Direcionada)",
                422 => "422 Unprocessable Entity(Entidade Não Processável -WebDAV)",
                423 => "423 Locked(Bloqueado - WebDAV)",
                424 => "424 Failed Dependency(Dependência Falhou -WebDAV)",
                425 => "425 Too Early(Muito Cedo)",
                426 => "426 Upgrade Required(Atualização Necessária)",
                428 => "428 Precondition Required(Pré-Condição Necessária)",
                429 => "429 Too Many Requests(Solicitações em Excesso)",
                431 => "431 Request Header Fields Too Large(Campos do Cabeçalho da Requisição Muito Grandes)",
                451 => "451 Unavailable For Legal Reasons(Indisponível por Razões Legais)",
                //5xx - Erro do Servidor = Indica que o servidor falhou ao cumprir uma solicitação válida:
                500 => "500 Internal Server Error(Erro Interno do Servidor)",
                501 => "501 Not Implemented(Não Implementado)",
                502 => "502 Bad Gateway(Gateway Inválido)",
                503 => "503 Service Unavailable(Serviço Indisponível)",
                504 => "504 Gateway Timeout(Tempo Esgotado do Gateway)",
                505 => "505 HTTP Version Not Supported(Versão HTTP Não Suportada)",
                506 => "506 Variant Also Negotiates(Variante Também Negocia)",
                507 => "507 Insufficient Storage(Armazenamento Insuficiente -WebDAV)",
                508 => "508 Loop Detected(Loop Detectado -WebDAV)",
                510 => "510 Not Extended(Não Estendido)",
                511 => "511 Network Authentication Required(Autenticação de Rede Necessária)",
                // Código não mapeado
                _ => $"Código HTTP {codigo} não mapeado"
            };
        }

        /* Valida os casos em que o path/caminho está vazio ou é um fakepath */

        public static bool NaoExistePath(string? campo, string texto = "fakepath")
        {
            return string.IsNullOrEmpty(campo) || campo.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /*
           USO:
                 MSSQL:
                 using var conexao = new NpgsqlConnection ("sua-connection-string-mssql");
                 var data = await ObterDataHoraServidorAsync(conexao, "mssql");

                 ORACLE:
                 using var conexao = new OracleConnection("sua-connection-string-oracle");
                 var data = await ObterDataHoraServidorAsync(conexao, "oracle");

                 MYSQL:
                 using var conexao = new MyNpgsqlConnection ("sua-connection-string-mysql");
                 var data = await ObterDataHoraServidorAsync(conexao, "mysql");

                 FIREBASE ou FIRESTORE:
                 // Usando biblioteca Google.Cloud.Firestore
                 var timestamp = Timestamp.GetCurrentTimestamp();
                 DateTime dataHoraServidor = timestamp.ToDateTime();

         */

        public static async Task<DateTime?> ObterDataHoraServidorAsync(DbConnection conexao, string tipoBanco)
        {
            string comandoSql = tipoBanco.ToLower() switch
            {
                "mssql" => "SELECT GETDATE()",
                "mysql" => "SELECT NOW()",
                "oracle" => "SELECT SYSDATE FROM DUAL",
                _ => throw new NotSupportedException($"Tipo de banco '{tipoBanco}' não suportado.")
            };

            try
            {
                await using DbCommand cmd = conexao.CreateCommand();
                cmd.CommandText = comandoSql;

                if (conexao.State != ConnectionState.Open)
                    await conexao.OpenAsync();

                object? resultado = await cmd.ExecuteScalarAsync();
                return Convert.ToDateTime(resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter data do banco: {ex.Message}");
                return null;
            }
        }

        /* Mascarador para esconder partes de textos sensíveis */

        public static string MascararTexto(string? textoOriginal, int visivelInicio = 3, int visivelFim = 3, char mascara = '*')
        {
            if (string.IsNullOrEmpty(textoOriginal))
                return string.Empty;

            int total = textoOriginal.Length;

            if (visivelInicio + visivelFim >= total)
                return new string(mascara, total); // Mascarar tudo se não houver espaço

            string inicio = textoOriginal.Substring(0, visivelInicio);
            string fim = textoOriginal.Substring(total - visivelFim, visivelFim);
            string meio = new string(mascara, total - visivelInicio - visivelFim);

            return $"{inicio}{meio}{fim}";
        }

        public static string MascararEmail(string? email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return MascararTexto(email);

            var partes = email.Split('@');
            var nome = MascararTexto(partes[0], 2, 1);
            return $"{nome}@{partes[1]}";
        }

        /* Se quiser salvar os campos que são NULL com vazio */

        public static string Safe(this string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value;
        }

        public static string SafeUpper(this string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.ToUpperInvariant();
        }

        public static string SafeLower(this string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.ToLowerInvariant();
        }

        //Função Helper para que o ToListAsync não quebre a aplicação 
        /*
          Exemplos de USO:  var dados = await query.SafeToListAsync(); // retorna lista vazia
                            ou
                            var dados = await query.SafeToListAsync(new List<PlanoExames> { new PlanoExames { Id = 0 } });  //retorna lista com 1 item
         */
        public static async Task<List<T>> SafeToListAsync<T>(this IQueryable<T> query, List<T>? fallback = null)
        {
            try
            {
                return await query.ToListAsync();
            }
            catch
            {
                return fallback ?? new List<T>();
            }
        }

        public static List<string> QuebrarTextoEmLinhas(string texto, int limite = 40)
        {
            var linhas = new List<string>();
            var palavras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var linhaAtual = "";

            foreach (var palavra in palavras)
            {
                // Verifica se adicionar a palavra ultrapassa o limite
                if ((linhaAtual.Length + palavra.Length + 1) <= limite)
                {
                    linhaAtual += (linhaAtual.Length == 0 ? "" : " ") + palavra;
                }
                else
                {
                    // Adiciona a linha atual à lista e começa uma nova
                    if (linhaAtual.Length > 0)
                        linhas.Add(linhaAtual);

                    linhaAtual = palavra;
                }
            }

            // Adiciona a última linha, se houver
            if (linhaAtual.Length > 0)
                linhas.Add(linhaAtual);

            return linhas;
        }

        public static void AppendTextoQuebrado(StringBuilder sb, string texto, int limite = 40)
        {
            if (texto.Trim() == "-")
            {
                sb.AppendLine(new string('-', limite));
                return;
            }

            var linhas = QuebrarTextoEmLinhas(texto, limite);
            foreach (var linha in linhas)
            {
                sb.AppendLine(linha);
            }
        }


    }//Fim
}