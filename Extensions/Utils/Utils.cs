using LabWebMvc.MVC.Mensagens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;


namespace Extensions
{
    public abstract class Base { }
    public class FeriadoDetalhe
    {
        public int ID { get; set; }
        public int Dia { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string? Descricao { get; set; }
    }

    public static class Utils
    {
        public static string ImagemOlho { get; set; } = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAABDUlEQVQ4jd2SvW3DMBBGbwQVKlyo4BGC4FKFS4+TATKCNxAggkeoSpHSRQbwAB7AA7hQoUKFLH6E2qQQHfgHdpo0yQHX8T3exyPR/ytlQ8kOhgV7FvSx9+xglA3lM3DBgh0LPn/onbJhcQ0bv2SHlgVgQa/suFHVkCg7bm5gzB2OyvjlDFdDcoa19etZMN8Qp7oUDPEM2KFV1ZAQO2zPMBERO7Ra4JQNpRa4K4FDS0R0IdneCbQLb4/zh/c7QdH4NL40tPXrovFpjHQr6PJ6yr5hQV80PiUiIm1OKxZ0LICS8TWvpyyOf2DBQQtcXk8Zi3+JcKfNafVsjZ0WfGgJlZZQxZjdwzX+ykf6u/UF0Fwo5Apfcq8AAAAASUVORK5CYII=";

        public static dynamic MensagemStartUp(this string nome, bool modoMenuFlutuante = false)
        {
            var statusUp = string.Format("{0} ::: {1}", nome, Mensagens_pt_BR.StatusUp);
            dynamic[] colecao = { statusUp, modoMenuFlutuante }; //false = não abre quadro flutuante popup de subopções do menu à direita da tela
            return colecao;
        }

        /* Preenche a String com o caractere passado até completar o tamanho definido
         * <param name="value"> Valor em String principal a ser alterado</param>
         * <param name="length"> Tamanho da string final</param>
         * <param name="ch">Caracter para completar os espaços vazios</param>
         * <param name="left">Indica se irá completar a esquerda</param>
         * <returns>Retorna a String alterada</returns>
         */
        public static string CompleteString(this string value, int length, char ch, bool left = true)
        {
            if (value == null)
            {
                return CompleteString("", length, ch, left);
            }
            if (value.Length > length)
            {
                if (left)
                {
                    var len = value.Length;
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

        public static string LimitaString(this string value, int length)
        {
            if (value != null && value.Length > length)
                return value.Substring(0, length);

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

        public static string RemoveAcentuacao(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : Encoding.ASCII.GetString(Encoding.GetEncoding("Cyrillic").GetBytes(value));
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

        public static string MoveFileForFinalDestination(string tempfile)
        {
            string uploadTemp = ConfigurationManager.AppSettings["UploadFolderTemporario"];
            string uploadFinal = ConfigurationManager.AppSettings["UploadFolderFinal"];

            string tempPath = Path.Combine(uploadTemp, tempfile);
            string finalPath = Path.Combine(uploadFinal, tempfile);

            if (!Directory.Exists(Path.GetDirectoryName(finalPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(finalPath));
            }
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Copy(tempPath, finalPath);

            return finalPath;
        }

        public static string CPFSemFormatacao(this string CPF)
        {
            return Regex.Replace(CPF, @"[^\d]", "");
        }

        public static string DesformataCamposNumericosComMascara(string value)
        {
            if (value != null)
                return Regex.Replace(value, @"[^\d]", "");

            return null;
        }

        public static DateTime DateFromDataBase(DateTime date, DateTime hour)
        {
            return new DateTime(date.Year, date.Month, date.Day, hour.Hour, hour.Minute, hour.Second);
        }

        public static string HourFromDataBase(this DateTime date)
        {
            string horario = date.Hour.ToString()+":"+date.Minute.ToString()+":"+date.Second.ToString();
            return horario;
        }

        public static IEnumerable DynamicSqlQuery(System.Data.Entity.Database database, string sql, params object[] parameters)
        {
            return DynamicSqlQuery(database, sql, 30, parameters);
        }

        public static IEnumerable DynamicSqlQuery(System.Data.Entity.Database database, string sql, int timeOut, params object[] parameters)
        {
            bool connectioWasOpen = false;

            TypeBuilder builder = Utils.CreateTypeBuilder("MyDynamicAssembly", "MyDynamicModule", "MyDynamicType");
            System.Data.IDbCommand command = database.Connection.CreateCommand();
            command.CommandTimeout = timeOut;

            try
            {
                if (database.Connection.State == System.Data.ConnectionState.Open)
                    connectioWasOpen = true;
                else
                    database.Connection.Open();

                command.CommandText = sql;
                // command.CommandTimeout = command.Connection.ConnectionTimeout;

                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                System.Data.IDataReader reader = command.ExecuteReader();

                var schema = reader.GetSchemaTable();

                foreach (System.Data.DataRow row in schema.Rows)
                {
                    string name = (string)row["ColumnName"];
                    Type type = (Type)row["DataType"];

                    if (type != typeof(string) && (bool)row.ItemArray[schema.Columns.IndexOf("AllowDbNull")])
                        type = typeof(Nullable<>).MakeGenericType(type);

                    Utils.CreateAutoImplementedProperty(builder, name, type);
                }
                reader.Close();
                reader.Dispose();
            }
            finally
            {
                command.Dispose();

                if (!connectioWasOpen)
                    database.Connection.Close();

                command.Parameters.Clear();
            }

            Type resultType = builder.CreateType();
            database.CommandTimeout = timeOut;
            return database.SqlQuery(resultType, sql, parameters);
        }

        public static dynamic FirstOrDefault(IEnumerable items)
        {
            IEnumerator iter = items.GetEnumerator();

            if (!iter.MoveNext())
                return null;

            return iter.Current;
        }

        private static TypeBuilder CreateTypeBuilder(string assemblyName, string moduleName, string typeName)
        {
            //Versão antiga para Framework antes de 4
            //TypeBuilder typeBuilder = AppDomain
            //    .CurrentDomain
            //    .DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run)
            //    .DefineDynamicModule(moduleName)
            //    .DefineType(typeName, TypeAttributes.Public);
            //typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            //versão nova para atender Core e Framework 4 e acima
            //define dinamicamente o assembly
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
            
            //cria o tipo Builder e define os atributos
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                  typeName,
                  TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | 
                  TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, 
                  typeof(Base));  //Base está declarado no topo desta Classe (mas ainda precisa ser testado)
            //se for definir o método como privado
            //typeBuilder.DefineDefaultConstructor(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            //se for definir o método como público
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            return typeBuilder;
        }

        private static void CreateAutoImplementedProperty(TypeBuilder builder, string propertyName, Type propertyType)
        {
            const string PrivateFieldPrefix = "m_";
            const string GetterPrefix = "get_";
            const string SetterPrefix = "set_";

            // Generate the field.
            FieldBuilder fieldBuilder = builder.DefineField(string.Concat(PrivateFieldPrefix, propertyName),
                                        propertyType, FieldAttributes.Private);

            // Generate the property
            PropertyBuilder propertyBuilder = builder.DefineProperty(propertyName, System.Reflection.PropertyAttributes.HasDefault, propertyType, null);

            // Property getter and setter attributes.
            MethodAttributes propertyMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the getter method.
            MethodBuilder getterMethod = builder.DefineMethod(string.Concat(GetterPrefix, propertyName), propertyMethodAttributes, propertyType, Type.EmptyTypes);

            // Emit the IL code.
            // ldarg.0
            // ldfld,_field
            // ret
            ILGenerator getterILCode = getterMethod.GetILGenerator();
            getterILCode.Emit(OpCodes.Ldarg_0);
            getterILCode.Emit(OpCodes.Ldfld, fieldBuilder);
            getterILCode.Emit(OpCodes.Ret);

            // Define the setter method.
            MethodBuilder setterMethod = builder.DefineMethod(string.Concat(SetterPrefix, propertyName), propertyMethodAttributes, null, new Type[] { propertyType });

            // Emit the IL code.
            // ldarg.0
            // ldarg.1
            // stfld,_field
            // ret
            ILGenerator setterILCode = setterMethod.GetILGenerator();
            setterILCode.Emit(OpCodes.Ldarg_0);
            setterILCode.Emit(OpCodes.Ldarg_1);
            setterILCode.Emit(OpCodes.Stfld, fieldBuilder);
            setterILCode.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getterMethod);
            propertyBuilder.SetSetMethod(setterMethod);
        }

        //public static CAS.Data.ObterDadosDoUsuarioResponse ObterDadosUsuarioFromTokenID(Guid? tokenID)
        //{
        //    if (tokenID == null)
        //    {
        //        throw new InvalidOperationException("Token não reconhecido. Favor passar o valor correto");
        //    }
        //    var cas = new CAS.Business.AutorizacaoBD();
        //    var response = cas.ObterDadosDoUsuario(new CAS.Data.ObterDadosDoUsuarioParameter() { TokenID = tokenID });
        //    if (response.HasErrors)
        //    {
        //        if (response.Errors.First().Description == "Unauthorized.")
        //            response.Errors.First().Description = "A sessão do usuário expirou. Favor fazer o login novamente.";
        //        throw new InvalidOperationException(response.Errors.First().Description);
        //    }
        //    return response;
        //}

        //public static long ObterUsuarioIdFromTokenID(Guid? tokenID)
        //{
        //    var response = Utils.ObterDadosUsuarioFromTokenID(tokenID);
        //    if (response.ID == 0)
        //    {
        //        throw new InvalidOperationException("Não foi localizado um usuário para esse UserID");
        //    }

        //    return response.ID;
        //}

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

            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = resto.ToString();

            TempCNPJ = TempCNPJ + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(TempCNPJ[i].ToString()) * mt2[i];

            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
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

        public static bool ValidarCEP(string cep)
        {
            if (cep.Length == 8)
            {
                cep = cep.Substring(0, 5) + "-" + cep.Substring(5, 3);
            }
            return Regex.IsMatch(cep, ("[0-9]{5}-[0-9]{3}"));
        }

        public static string FormatarCPF(this string cpfFormatar)
        {
            if (cpfFormatar == null)
            {
                throw new ArgumentNullException("CPF não pode ser nulo.");
            }
            if (!cpfFormatar.Equals(""))
            {
                string Temp = (cpfFormatar.Length < 11 ? cpfFormatar.ToString().PadLeft(11, '0') : cpfFormatar);
                return Formatar(Temp, "###.###.###-##");
            }
            return cpfFormatar;
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
            Regex r = new Regex(Formato);
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

        public static string CalcularIdade(DateTime DataDeNascimento)
        {
            var Anos = DateTime.Now.Year - DataDeNascimento.Year;
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

            var Idade = Anos > 0 ? (Anos + " anos") : "";
            if (Meses > 0 && Dias > 0)
                Idade = Idade + "-" + Meses + (Meses > 1 ? " meses" : " mês") + "-" + Dias + (Dias > 1 ? " dias" : " dia");
            else if (Meses == 0 && Dias > 0)
                Idade = Idade + "-" + Dias + (Dias > 1 ? " dias" : " dia");
            else if (Meses > 0 && Dias == 0)
                Idade = Idade + "-" + Meses + (Meses > 1 ? " meses" : " mês");

            return Idade;
        }

        public static DateTime CalcularProximoDiaUtil(DateTime pDataDeValidade, List<FeriadoDetalhe> DtFeriado, int pDiasRetorno)
        {
            int i, dias = 0;
            DateTime dataDiaUtil = pDataDeValidade;

            if (pDiasRetorno > 0)
            {
                for (i = 0; i <= 30; i++)
                {
                    dataDiaUtil = dataDiaUtil.AddDays(1);
                    if ((dataDiaUtil.DayOfWeek == DayOfWeek.Sunday || dataDiaUtil.DayOfWeek == DayOfWeek.Saturday || DtFeriado.Where(a => a.Dia == dataDiaUtil.Day && a.Mes == dataDiaUtil.Month && a.Ano == dataDiaUtil.Year).Count() > 0) == false)
                    {
                        dias++;
                    }
                    if (dias == pDiasRetorno)
                        break;
                }
            }
            else
            {
                for (i = 0; i <= 30; i++)
                {
                    if ((dataDiaUtil.DayOfWeek == DayOfWeek.Sunday || dataDiaUtil.DayOfWeek == DayOfWeek.Saturday || DtFeriado.Where(a => a.Dia == dataDiaUtil.Day && a.Mes == dataDiaUtil.Month && a.Ano == dataDiaUtil.Year).Count() > 0) == false)
                    {
                        break;
                    }
                    dataDiaUtil = dataDiaUtil.AddDays(1);
                }
            }
            return dataDiaUtil;
        }

        /// Deleta um arquivo já existente
        /// <param name="filename"> Nome completo do arquivo (caminho + nome + extensão)</param>
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
                foreach (var process in System.Diagnostics.Process.GetProcesses())
                {
                    if (process.MainWindowTitle.Contains(baseName))
                    {
                        process.Kill();

                        var stop = false;
                        System.Timers.Timer timer = new System.Timers.Timer(1000);//milissegundos
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
                soma += (peso1[i] * Convert.ToInt32(n[i]));
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
                soma += (peso2[i] * Convert.ToInt32(n[i]));
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

        public static string FormatarTelefone(string tel)
        {
            var telefone = "";

            if (tel.Length == 10)
            {
                telefone = "(" + tel.Substring(0, 2) + ") " + tel.Substring(2, 4) + "-" + tel.Substring(6, 4);
            }
            else if (tel.Length == 11)
            {
                telefone = "(" + tel.Substring(0, 2) + ") " + tel.Substring(2, 5) + "-" + tel.Substring(7, 4);
            }
            else if (tel.Length == 14 || tel.Length == 15)
            {
                return tel;
            }
            return telefone;
        }

        public static string FormatarCEP(string cep)
        {
            if (cep == null)
            {
                throw new ArgumentNullException("CEP não pode ser nulo.");
            }
            string Temp = (cep.Length < 8 ? cep.ToString().PadLeft(8, '0') : cep);
            return Formatar(Temp, "#####-###");
        }

        public static bool ValidarDeclaracaoNascidoVivo(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var firstChar = value[0];
                return value.Any(c => c != firstChar);
            }
            return false;
        }

        private static bool ValidarCNS_PROVISORIO(string vlrCNS)
        {
            var pis = "";
            var resto = 0;
            var soma = 0;

            pis = vlrCNS.Substring(0, 15);

            if (pis == "") return false;

            if ((vlrCNS.Substring(0, 1) != "7") && (vlrCNS.Substring(0, 1) != "8") && (vlrCNS.Substring(0, 1) != "9"))
            {
                return false;
            }

            soma = ((Convert.ToInt32(pis.Substring(0, 1), 10)) * 15)
                    + ((Convert.ToInt32(pis.Substring(1, 1), 10)) * 14)
                    + ((Convert.ToInt32(pis.Substring(2, 1), 10)) * 13)
                    + ((Convert.ToInt32(pis.Substring(3, 1), 10)) * 12)
                    + ((Convert.ToInt32(pis.Substring(4, 1), 10)) * 11)
                    + ((Convert.ToInt32(pis.Substring(5, 1), 10)) * 10)
                    + ((Convert.ToInt32(pis.Substring(6, 1), 10)) * 9)
                    + ((Convert.ToInt32(pis.Substring(7, 1), 10)) * 8)
                    + ((Convert.ToInt32(pis.Substring(8, 1), 10)) * 7)
                    + ((Convert.ToInt32(pis.Substring(9, 1), 10)) * 6)
                    + ((Convert.ToInt32(pis.Substring(10, 1), 10)) * 5)
                    + ((Convert.ToInt32(pis.Substring(11, 1), 10)) * 4)
                    + ((Convert.ToInt32(pis.Substring(12, 1), 10)) * 3)
                    + ((Convert.ToInt32(pis.Substring(13, 1), 10)) * 2)
                    + ((Convert.ToInt32(pis.Substring(14, 1), 10)) * 1);

            resto = soma % 11;

            if (resto == 0)
                return true;
            else
                return false;
        }


        private static bool ValidarCNS_DEFINITIVO(string vlrCNS)
        {
            var soma = 0;
            var resto = 0;
            var dv = 0;
            var pis = "";
            var resultado = "";

            pis = vlrCNS.Substring(0, 11);
            soma = (((Convert.ToInt32(pis.Substring(0, 1))) * 15) +
                    ((Convert.ToInt32(pis.Substring(1, 1))) * 14) +
                    ((Convert.ToInt32(pis.Substring(2, 1))) * 13) +
                    ((Convert.ToInt32(pis.Substring(3, 1))) * 12) +
                    ((Convert.ToInt32(pis.Substring(4, 1))) * 11) +
                    ((Convert.ToInt32(pis.Substring(5, 1))) * 10) +
                    ((Convert.ToInt32(pis.Substring(6, 1))) * 9) +
                    ((Convert.ToInt32(pis.Substring(7, 1))) * 8) +
                    ((Convert.ToInt32(pis.Substring(8, 1))) * 7) +
                    ((Convert.ToInt32(pis.Substring(9, 1))) * 6) +
                    ((Convert.ToInt32(pis.Substring(10, 1))) * 5));
            resto = soma % 11;
            dv = 11 - resto;
            if (dv == 11) dv = 0;
        
            if (dv == 10)
            {
                soma = (((Convert.ToInt32(pis.Substring(0, 1))) * 15) +
                        ((Convert.ToInt32(pis.Substring(1, 1))) * 14) +
                        ((Convert.ToInt32(pis.Substring(2, 1))) * 13) +
                        ((Convert.ToInt32(pis.Substring(3, 1))) * 12) +
                        ((Convert.ToInt32(pis.Substring(4, 1))) * 11) +
                        ((Convert.ToInt32(pis.Substring(5, 1))) * 10) +
                        ((Convert.ToInt32(pis.Substring(6, 1))) * 9) +
                        ((Convert.ToInt32(pis.Substring(7, 1))) * 8) +
                        ((Convert.ToInt32(pis.Substring(8, 1))) * 7) +
                        ((Convert.ToInt32(pis.Substring(9, 1))) * 6) +
                        ((Convert.ToInt32(pis.Substring(10, 1))) * 5) + 2);
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

        public static bool ValidarCNS(string vlrCNS)
        {
            string justNumbers = new string(vlrCNS.Where(char.IsDigit).ToArray());
            var tamCNS = justNumbers.Length;

            if (tamCNS != 15) return false;

            if (justNumbers == "000000000000000") return false;

            if (!ValidarCNS_PROVISORIO(justNumbers))
                return ValidarCNS_DEFINITIVO(justNumbers);

            return true;
        }

        public static string GetEscapedString(string value)
        {
            var caracteresAcentuados = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇç";
            var caracteresSemAcentos = "AAAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUuuuuCc";
            var caracteresNaoPermitidos = "'!@#$%¨&*()+=§{}[]ªº/\\?°|´`~^\"";

            if (value != null)
            {
                StringBuilder str = new StringBuilder();
                foreach (var c in value.ToCharArray())
                {
                    if (!Char.IsControl(c))
                        str.Append(c);
                }
                for (var i = 0; i < caracteresAcentuados.Length; i++)
                {
                    str = str.Replace(caracteresAcentuados[i].ToString(), caracteresSemAcentos[i].ToString());
                }
                for (var i = 0; i < caracteresNaoPermitidos.Length; i++)
                {
                    str = str.Replace(caracteresNaoPermitidos[i].ToString(), "");
                }
                str.Replace("amp;", "");

                return str.ToString();
            }
            return value;
        }

        public static List<string> GetFullErrorMessage(Exception e)
        {
            var log = new List<string>();
            log.Add(e.Message);
            var inner = e.InnerException;
            while (inner != null)
            {
                log.Add(inner.Message);
                inner = inner.InnerException;
            }
            return log;
        }

        public static string EnviarEmail(string Assunto, string Mensagem, string Email, bool html)
        {
            var log = "";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();

            if (((System.Net.NetworkCredential)smtp.Credentials) != null)
            {
                log += "Credential UserName: " + ((System.Net.NetworkCredential)smtp.Credentials).UserName + "\n";
            }
            else
                throw new ApplicationException("Credenciais de Email estão nulas");

            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage(((System.Net.NetworkCredential)smtp.Credentials).UserName, Email);
            mail.From = new System.Net.Mail.MailAddress(((System.Net.NetworkCredential)smtp.Credentials).UserName);
            mail.Subject = Assunto;
            mail.Body = Mensagem;
            mail.IsBodyHtml = html;
            //Envia
            smtp.Send(mail);

            return log;
        }

        /* Converte String para a Base64 */
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /* Converte Base64 para String */
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /* Retorna somente Números de uma string qualquer (Remove letras e símbolos) */
        public static string RetornaNumeros(string str)
        {
            var apenasDigitos = new Regex(@"[^\d]");
            return apenasDigitos.Replace(str, "");
        }

        public static string ClearString(string strcomAcentos)
        {
            var strsemAcentos = strcomAcentos;

            strsemAcentos = Regex.Replace(strsemAcentos, "[ÁÀÂÃ]", "A");
            strsemAcentos = Regex.Replace(strsemAcentos, "[áàâã]", "a");
            strsemAcentos = Regex.Replace(strsemAcentos, "[ÉÈÊ]", "E");
            strsemAcentos = Regex.Replace(strsemAcentos, "[éèê]", "e");
            strsemAcentos = Regex.Replace(strsemAcentos, "[ÍÌÎ]", "I");
            strsemAcentos = Regex.Replace(strsemAcentos, "[íìî]", "i");
            strsemAcentos = Regex.Replace(strsemAcentos, "[ÓÒÔÕ]", "O");
            strsemAcentos = Regex.Replace(strsemAcentos, "[óòôõ]", "o");
            strsemAcentos = Regex.Replace(strsemAcentos, "[ÚÙÛÜ]", "U");
            strsemAcentos = Regex.Replace(strsemAcentos, "[úùûü]", "u");
            strsemAcentos = Regex.Replace(strsemAcentos, "[Ç]", "C");
            strsemAcentos = Regex.Replace(strsemAcentos, "[ç]", "c");

            strsemAcentos =
                strsemAcentos.Replace("~", string.Empty)
                    .Replace("´", string.Empty)
                    .Replace("`", string.Empty)
                    .Replace("^", string.Empty)
                    .Replace("'", " ")
                    .Replace("!", string.Empty)
                    .Replace("#", string.Empty)
                    .Replace("%", string.Empty)
                    .Replace("¨", string.Empty)
                    .Replace("&", string.Empty)
                    .Replace("*", string.Empty)
                    .Replace("+", string.Empty)
                    .Replace("=", string.Empty)
                    .Replace("?", string.Empty)
                    .Replace(";", string.Empty)
                    .Replace("[", string.Empty)
                    .Replace("{", string.Empty)
                    .Replace("]", string.Empty)
                    .Replace("}", string.Empty)
                    .Replace("\\", string.Empty)
                    .Replace(".", string.Empty)
                    .Replace(",", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("¬", string.Empty)
                    .Replace("¢", string.Empty)
                    .Replace("§", string.Empty)
                    .Replace("°", string.Empty)
                    .Replace("º", string.Empty)
                    .Replace("ª", string.Empty)
                    .Replace("¹", string.Empty)
                    .Replace("²", string.Empty)
                    .Replace("³", string.Empty)
                    .Replace("£", string.Empty)
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty);

            return strsemAcentos;
        }

        /* Converte uma data String no formato dd/mm/aaaa para o tipo DateTime.
           <param name="dataString">Data string no formato dd/mm/aaaa</param>
           <param name="start">Gerar data com horário inicial ou final</param>
           <returns></returns>
        */
        public static DateTime GetDateFromString(string dateString, bool start = true)
        {
            var dia = Convert.ToInt16(dateString.Substring(0, 2));
            var mes = Convert.ToInt16(dateString.Substring(2, 2));
            var ano = Convert.ToInt32(dateString.Substring(4, 4));

            if (start)
                return new DateTime(ano, mes, dia, 0, 0, 0);
            else
                return new DateTime(ano, mes, dia, 23, 59, 59);
        }
    }
}
