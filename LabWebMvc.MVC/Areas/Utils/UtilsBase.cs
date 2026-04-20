using BLL;
using LabWebMvc.MVC.Interfaces.Criptografias;
using LabWebMvc.MVC.Mensagens;
using LabWebMvc.MVC.Models;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using static ExtensionsMethods.Genericos.Enumeradores;

namespace LabWebMvc.MVC.Areas.Utils
{
    public static class Utils
    {
        private static IHttpContextAccessor? _accessor;

        public static void SetHttpContextAccessor(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public static string? LoginCNPJEmpresaLogado()
        {
            return _accessor?.HttpContext?.Session.GetString("SessionCNPJEmpresa");
        }

        public static string? LoginNomeEmpresaLogado()
        {
            return _accessor?.HttpContext?.Session.GetString("SessionNomeEmpresa");
        }

        //Para o Manu com Bootstrap 5, o ID do menu deve ser único e seguro, evitando caracteres especiais e espaços.
        public static string GeraMenuIdSeguro(string nomeMenu, int id)
        {
            if (string.IsNullOrWhiteSpace(nomeMenu))
                nomeMenu = "menu";

            // Remove acentos e caracteres especiais
            string normalizado = nomeMenu.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder builder = new();

            foreach (char ch in normalizado)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (char.IsLetterOrDigit(ch))
                        builder.Append(ch);
                    else if (char.IsWhiteSpace(ch))
                        builder.Append('_');
                }
            }

            return $"collapse_{builder}_{id}";
        }

        #region ImagemOlho

        public static string ImagemOlho { get; set; } = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAABDUlEQVQ4jd2SvW3DMBBGbwQVKlyo4BGC4FKFS4+TATKCNxAggkeoSpHSRQbwAB7AA7hQoUKFLH6E2qQQHfgHdpo0yQHX8T3exyPR/ytlQ8kOhgV7FvSx9+xglA3lM3DBgh0LPn/onbJhcQ0bv2SHlgVgQa/suFHVkCg7bm5gzB2OyvjlDFdDcoa19etZMN8Qp7oUDPEM2KFV1ZAQO2zPMBERO7Ra4JQNpRa4K4FDS0R0IdneCbQLb4/zh/c7QdH4NL40tPXrovFpjHQr6PJ6yr5hQV80PiUiIm1OKxZ0LICS8TWvpyyOf2DBQQtcXk8Zi3+JcKfNafVsjZ0WfGgJlZZQxZjdwzX+ykf6u/UF0Fwo5Apfcq8AAAAASUVORK5CYII=";

        #endregion ImagemOlho

        #region Constantes e públicas genéricas

        public const int ExtendedTimeout = 60000;   //Tempo máximo aguardando execução de script na base de dados via SQL Command
        public static string PathExecutavel = AppDomain.CurrentDomain.BaseDirectory;

        #endregion Constantes e públicas genéricas

        /*
         * De forma mais simples, chama o "DynamicSqlQuery" em tempo de execução, apenas com os parâmetros da query, mantendo o tempo padrão de 30 segundos!
         */

        #region DynamicSqlQuery

        public static IEnumerable DynamicSqlQuery(Db database, string sql, params object[] parameters)
        {
            return DynamicSqlQuery(database, sql, 30, parameters);
        }

        #endregion DynamicSqlQuery

        #region DynamicSqlQuery - Método Principal

        public static IEnumerable DynamicSqlQuery(Db database, string sql, int timeOut = 30, params object[] parameters)
        {
            // Criando tipo dinâmico para os resultados
            TypeBuilder builder = CreateTypeBuilder("MyDynamicAssembly", "MyDynamicModule", "MyDynamicType");

            using DbCommand command = database.Database.GetDbConnection().CreateCommand();
            command.CommandTimeout = timeOut;
            command.CommandText = sql;

            bool connectionWasOpen = database.Database.GetDbConnection().State == ConnectionState.Open;

            try
            {
                if (!connectionWasOpen)
                {
                    database.Database.OpenConnection();
                }

                // Adicionando parâmetros
                foreach (object param in parameters)
                {
                    DbParameter dbParam = command.CreateParameter();
                    dbParam.Value = param ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }

                using DbDataReader reader = command.ExecuteReader();
                DataTable? schema = reader.GetSchemaTable();

                if (schema != null)
                {
                    foreach (DataRow row in schema.Rows)
                    {
                        string name = (string)row["ColumnName"];
                        Type type = (Type)row["DataType"];
                        object? allowNull = row["AllowDBNull"];

                        if (type != typeof(string) && allowNull != null && (bool)allowNull)
                            type = typeof(Nullable<>).MakeGenericType(type);

                        CreateAutoImplementedProperty(builder, name, type);
                    }
                }
            }
            finally
            {
                if (!connectionWasOpen)
                {
                    database.Database.CloseConnection();
                }
                command.Parameters.Clear();
            }

            Type resultType = builder.CreateType()!;
            database.Database.SetCommandTimeout(timeOut);

            // Executa SQL e retorna resultados
            return database.Database.SqlQueryRaw<object>(sql, parameters);
        }

        #endregion DynamicSqlQuery - Método Principal

        /* VERSÃO ASSÍNCRONA DOS MÉTODOS
         *      SqlQueryRawAsync
         *      DynamicSqlQueryAsync
         */

        public static async Task<List<T>> SqlQueryRawAsync<T>(DbContext dbContext, string sql, params object[] parameters) where T : class
        {
            return await dbContext.Set<T>().FromSqlRaw(sql, 30, parameters).ToListAsync();
        }

        /*
         * Cria query SQL dinâmica em tempo de execução, passando o tempo máximo de execução e parâmetros da query
         * EXEMPLO DE USO:
                           public async Task<IEnumerable> ObterUsuarios()
                           {
                              string sql = "SELECT Id, Nome, DataCriacao FROM Usuarios";
                              return await Utils.DynamicSqlQueryAsync(_db, sql);
                           }
         */

        #region DynamicSqlQueryAsync

        public static async Task<IEnumerable> DynamicSqlQueryAsync(Db database, string sql, int timeOut = 30, params object[] parameters)
        {
            bool connectionWasOpen = database.Database.GetDbConnection().State == System.Data.ConnectionState.Open;

            TypeBuilder builder = CreateTypeBuilder("MyDynamicAssembly", "MyDynamicModule", "MyDynamicType");

            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandTimeout = timeOut;
            command.CommandText = sql;

            try
            {
                if (!connectionWasOpen)
                {
                    await database.Database.OpenConnectionAsync();
                }

                foreach (object param in parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.Value = param ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }

                await using var reader = await command.ExecuteReaderAsync();
                var schema = reader.GetSchemaTable();

                if (schema != null)
                {
                    foreach (System.Data.DataRow row in schema.Rows)
                    {
                        string name = (string)row["ColumnName"];
                        Type type = (Type)row["DataType"];
                        object? allowNull = row["AllowDBNull"];

                        if (type != typeof(string) && allowNull != null && (bool)allowNull)
                            type = typeof(Nullable<>).MakeGenericType(type);

                        CreateAutoImplementedProperty(builder, name, type);
                    }
                }
            }
            finally
            {
                if (!connectionWasOpen)
                    await database.Database.CloseConnectionAsync();

                command.Parameters.Clear();
            }

            Type resultType = builder.CreateType()!;
            database.Database.SetCommandTimeout(timeOut);

            return await SqlQueryRawAsync<object>(database, sql, parameters);
        }

        #endregion DynamicSqlQueryAsync

        /*
           EXEMPLO DE USO:
           List<dynamic> results = DynamicListFromSql(myDb,"select * from table where a=@a and b=@b", new Dictionary<string, object> { { "a", true }, { "b", false } }).ToList();
         */

        #region DynamicListFromSql

        public static IEnumerable<dynamic> DynamicListFromSql(this DbContext db, string Sql, Dictionary<string, object> Params)
        {
            using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = Sql;

                if (db.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
                {
                    db.Database.OpenConnection();
                }

                foreach (KeyValuePair<string, object> p in Params)
                {
                    DbParameter dbParameter = cmd.CreateParameter();
                    dbParameter.ParameterName = p.Key;
                    dbParameter.Value = p.Value;
                    cmd.Parameters.Add(dbParameter);
                }

                using (DbDataReader dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        IDictionary<string, object> row = new Dictionary<string, object>(); // Substituição de ExpandoObject
                        for (int fieldCount = 0; fieldCount < dataReader.FieldCount; fieldCount++)
                        {
                            row.Add(dataReader.GetName(fieldCount), dataReader[fieldCount]);
                        }
                        yield return row;
                    }
                }
            }
        }

        #endregion DynamicListFromSql

        /* Retorna valores das configurações do arquivo de strings de conexão */

        #region GetPathAppSettingsJson

        public static string GetPathAppSettingsJson()
        {
            string pathExe = PathExecutavel;  //No Debug é: F:\Projetos dotNet\Web-Project\LabWeb7-Project\LabWebMvc.MVC\bin\Debug\net8.0\

            if (File.Exists(string.Format("{0}{1}", pathExe, "appsettings.Development.json")) || File.Exists(string.Format("{0}{1}", pathExe, "appsettings.json")))
            {
                return pathExe;
            }
            else
            {
                //segunda tentiva de pegar o arquivo json de configuração no ambiente da aplicação
                string? envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrEmpty(envName))
                {
                    /*
                    * lista dos serviços que vão utilizar o AppSettings.Json ou AppSettings.Development.Json
                    */
                    string[] svr = { "ServicoExportacao", "ServicoImportacao" };
                    string path = Path.Combine(Directory.GetCurrentDirectory());

                    if (svr.Contains(path))
                    {
                        foreach (string item in svr)
                        {
                            path = path.Replace(string.Format("{0}{1}", "\\", item), "");
                        }
                        path = string.Format("{0}{1}", path, "\\LabWebMvc.Mvc");
                    }
                    return path;
                }
            }
            return pathExe;
        }

        #endregion GetPathAppSettingsJson

        #region EnviarEmail

        public static string EnviarEmail(string Assunto, string Mensagem, string Email, bool html)
        {
            string log = "";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            System.Net.Mail.SmtpClient smtp = new();

            if ((NetworkCredential?)smtp.Credentials != null)
            {
                log += "Credential UserName: " + ((NetworkCredential)smtp.Credentials).UserName + "\n";
            }
            else
                throw new ApplicationException("Credenciais de Email estão nulas");

            System.Net.Mail.MailMessage mail = new(((NetworkCredential)smtp.Credentials).UserName, Email)
            {
                From = new System.Net.Mail.MailAddress(((NetworkCredential)smtp.Credentials).UserName),
                Subject = Assunto,
                Body = Mensagem,
                IsBodyHtml = html
            };
            //Envia
            smtp.Send(mail);

            return log;
        }

        #endregion EnviarEmail

        #region FirstOrDefault for dynamic

        public static dynamic? FirstOrDefault(IEnumerable items)
        {
            IEnumerator iter = items.GetEnumerator();

            if (!iter.MoveNext())
                return null;

            return iter.Current;
        }

        #endregion FirstOrDefault for dynamic

        #region MensagemStartUp

        public static dynamic MensagemStartUp(this string nome, bool modoMenuFlutuante = false)
        {
            string statusUp = string.Format("{0} ::: {1}", nome, Mensagens_pt_BR.StatusUp);
            dynamic[] colecao = { statusUp, modoMenuFlutuante }; //false = não abre quadro flutuante popup de subopções do menu à direita da tela
            return colecao;
        }

        #endregion MensagemStartUp

        /* Retira TODOS os espaços em branco de um texto */

        public static string Alltrim(this string texto)
        {
            return texto.Replace(" ", "");
        }

        public static string? LoginNomeLogado()
        {
            IHttpContextAccessor HttpContextAccessor = new HttpContextAccessor();
            return HttpContextAccessor.HttpContext?.Session.GetString("SessionNome");
        }

        public static string? LoginEmailLogado()
        {
            IHttpContextAccessor HttpContextAccessor = new HttpContextAccessor();
            return HttpContextAccessor.HttpContext?.Session.GetString("SessionEmail");
        }

        public static string? TotalReCaptcha(Db db)
        {
            try
            {
                DateTime agora = DateTime.Now;
                ReCaptchaMonitoramento? totalReCaptcha = db.ReCaptchaMonitoramento.Where(r => r.MesReferencia == agora.Month && r.AnoReferencia == agora.Year).FirstOrDefault();

                if (totalReCaptcha != null)
                {
                    return totalReCaptcha.QuantidadeSolicitacoes.ToString("N0") + @"/10.000 mês";
                }
            }
            catch
            {
                //Log opcional
            }

            return "0/0";
        }

        public static string? LoginTokenLogado()
        {
            IHttpContextAccessor HttpContextAccessor = new HttpContextAccessor();
            return HttpContextAccessor.HttpContext?.Session.GetString("SessionToken");
        }

        /*
         * Retorna nome de e-mail validado
         */

        public static string RetornaEmailValidado(this string email)
        {
            //começa validando o e-mail com regular expression
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                return "e-mail inválido";
            }
            return email;
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

        public static string MoveFileForFinalDestination(string tempfile = "C:\\Temp")
        {
            string? uploadTemp = System.Configuration.ConfigurationManager.AppSettings["UploadFoldERTemporario"];
            string? uploadFinal = System.Configuration.ConfigurationManager.AppSettings["UploadFolderFinal"];

            string tempPath = !string.IsNullOrEmpty(uploadTemp) ? uploadTemp : tempfile;
            string? finalPath = !string.IsNullOrEmpty(uploadFinal) ? uploadFinal : tempfile;

            if (!Directory.Exists(Path.GetDirectoryName(finalPath)))
            {
                finalPath = Path.GetDirectoryName(finalPath);
                if (finalPath != null) Directory.CreateDirectory(finalPath);
            }
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            if (finalPath != null) File.Copy(tempPath, finalPath);

            return finalPath != null ? finalPath : tempfile;
        }

        /*
         * Cria uma cópia do arquivo de um path qualquer para outro
         */

        public static void FileCopy(string pastaOrigem, string nomeArquivoOrigem, string pastaDestino, string nomeArquivoDestino, bool sobrescreve = true, bool geraCripto = false)
        {
            if (geraCripto)
            {
                nomeArquivoDestino = CriptoDecripto.ArquivoToken(nomeArquivoDestino);
            }
            string origem = Path.Combine(pastaOrigem, nomeArquivoOrigem);
            string destino = Path.Combine(pastaDestino, nomeArquivoDestino);
            File.Copy(origem, destino, sobrescreve);
        }

        /*
         * Cria uma cópia do arquivo de um path qualquer para o "Temp" do Root do Sistema (wwwRoot\Temp)
         */

        public static string FileCopyTemp(string pastaOrigem, string nomeArquivoOrigem, string nomeArquivoDestino, bool sobrescreve = true, bool geraCripto = false)
        {
            if (geraCripto)
            {
                nomeArquivoDestino = CriptoDecripto.ArquivoToken(nomeArquivoDestino);
            }
            string pastaDestino = Path.GetFullPath("wwwroot\\temp\\");         // "\wwwroot\temp\"
            string origem = Path.Combine(pastaOrigem, nomeArquivoOrigem);
            string destino = Path.Combine(pastaDestino, nomeArquivoDestino);
            File.Copy(origem, destino, sobrescreve);
            return nomeArquivoDestino;  //retorna o nome pois pode estar criptografado/token.
        }

        /*
         * Cria uma cópia do arquivo para o path LOCAL do Windows Temp (%temp%)
         */

        #region FileCopyWinTemp

        public static string FileCopyWinTemp(string pastaOrigem, string nomeArquivoOrigem, string nomeArquivoDestino, bool sobrescreve = true, bool geraCripto = true)
        {
            string pastaDestino = GetLocalPathTempDoWindows(); //%temp%
            string origem = Path.Combine(pastaOrigem, nomeArquivoOrigem);
            string destino = Path.Combine(pastaDestino, nomeArquivoDestino);
            File.Copy(origem, destino, sobrescreve);
            return nomeArquivoDestino;  //retorna o nome pois pode estar criptografado/token.
        }

        #endregion FileCopyWinTemp

        /*
         * Retorna/Pega o Valor de Um Parâmetro de Configuração do Setup dos Serviços
         */

        #region GetValorSetupDoServico

        public static string? GetValorSetupDoServico(string Sessao = "LoginPadraoSistema", string Parametro = "UsuarioLogin")
        {
            IConfigurationRoot configuration = null!;

            configuration = Validations.Utils.Ambiente();

            if (Sessao != null && Parametro != null && configuration.GetSection(Sessao).GetSection(Parametro).Exists())
                return configuration.GetSection(Sessao).GetSection(Parametro).Value;

            return string.Empty;
        }

        #endregion GetValorSetupDoServico

        /*
         * Retorna a pasta local de imagens do usuário
         * A pasta local fica definida no appsettings ou não teremos como pegar a pasta local
         */

        #region GetLocalPathImagens

        public static string GetLocalPathImagens()
        {
            string? pathSetup = GetValorSetupDoServico("Paths", "Imagens");
            string pathImages = string.Empty;
            if (pathSetup != null)
            {
                pathImages = Environment.ExpandEnvironmentVariables(pathSetup);
                pathImages = string.Format("{0}{1}", pathImages.Replace("/", "##").Replace("#", "/"), "\\");
            }
            return pathImages;
        }

        #endregion GetLocalPathImagens

        /*
         * Retorna a pasta local de documentos do usuário
         * A pasta local fica definida no appsettings ou não teremos como pegar a pasta local
         */

        #region GetLocalPathDocumentos

        public static string GetLocalPathDocumentos()
        {
            string? pathSetup = GetValorSetupDoServico("Paths", "Documentos");
            string pathImages = string.Empty;
            if (pathSetup != null)
            {
                pathImages = Environment.ExpandEnvironmentVariables(pathSetup);
            }
            return pathImages;
        }

        #endregion GetLocalPathDocumentos

        /*
         * Retorna a pasta local de Temp do usuário
         */

        #region GetLocalPathTemp

        public static string GetLocalPathTemp()
        {
            string? pathSetup = GetValorSetupDoServico("Paths", "Temporario");
            string pathImages = string.Empty;
            if (pathSetup != null)
            {
                pathImages = Environment.ExpandEnvironmentVariables(pathSetup);
            }
            return pathImages;
        }

        #endregion GetLocalPathTemp

        /*
         * Retorna a pasta TEMP do wwwroot (wwwroot\temp)
         */

        public static string GetLocalwwwRootTemp()
        {
            return @"/temp/";
        }

        /*
         * Retorna a pasta local de Temp do usuário Windows
         */

        #region GetLocalPathTempDoWindows

        public static string GetLocalPathTempDoWindows()
        {
            string? pathSetup = GetValorSetupDoServico("Paths", "WinTemp");
            string pathImages = string.Empty;
            if (pathSetup != null)
            {
                pathImages = Environment.ExpandEnvironmentVariables(pathSetup);
            }
            return pathImages;
        }

        #endregion GetLocalPathTempDoWindows

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

        private static void CreateAutoImplementedProperty(TypeBuilder builder, string propertyName, Type propertyType)
        {
            const string PrivateFieldPrefix = "m_";
            const string GetterPrefix = "get_";
            const string SetterPrefix = "set_";

            // Generate the field.
            FieldBuilder fieldBuilder = builder.DefineField(string.Concat(PrivateFieldPrefix, propertyName),
                                        propertyType, FieldAttributes.Private);

            // Generate the property
            PropertyBuilder propertyBuilder = builder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

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

        /*
         *   CreateTypeBuilder: Define e cria novas instâncias de classes em tempo de execução.
         */

        #region CreateTypeBuilder

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
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);

            //cria o tipo Builder e define os atributos
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                  typeName,
                  TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass |
                  TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                  typeof(BaseClass));  //Base está declarado no topo desta Classe (mas ainda precisa ser testado)
                                       //se for definir o método como privado
                                       //typeBuilder.DefineDefaultConstructor(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                                       //se for definir o método como público
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            return typeBuilder;
        }

        #endregion CreateTypeBuilder

        /* EXEMPLO DE USO: string redirecionaUrl = "Senhas".MontaUrl(base.HttpContext.Request);
         * onde "Senhas" é a action que se deseja redirecionar!
         */
        public static string MontaUrl(this string action, HttpRequest request)
        {
            try
            {
                var scheme = request.Scheme; // http ou https
                var host = request.Host.Value; // inclui porta se existir
                var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;

                return $"{scheme}://{host}{pathBase}/{action}";
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();
                eventLog2.LogEventViewer("'Action: " + action + "', Erro de montagem de URL: " + msg, "wError");
            }
            return action;
        }


        //Retorna uma data convertida para DateTime
        public static DateTime ToFormataData(this string data)
        {
            return Convert.ToDateTime(data);
        }

        //Capitalize considerando variável vazia ou nula, retornando vazia.
        public static string? ToCapitalize(this string? Texto)
        {
            if (Texto == null)
                return Texto;
            return string.Join(" ", Texto.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Substring(0, 1).ToUpper() + c.Substring(1).ToLower()));
        }

        //Capitalize considerando variável nunca vazia ou nula.
        public static string ToCapitalizeNotNull(this string Texto)
        {
            return string.Join(" ", Texto.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Substring(0, 1).ToUpper() + c.Substring(1).ToLower()));
        }

        /* Método sobrescrito de Upper para evitar marcações verdes em textos nulos */

        public static string? Upper(this string? Texto)
        {
            if (Texto == null)
                return Texto;
            return Texto.ToUpper();
        }

        /* Método Genérico para ler itens das LISTAS específicas em combo box de listas suspensas para:
         * TipoDocumento, Emissor, Genero, EstadoCivil, UF, TempoGestacao, ListaSN, ListaTipoMapa
         * Lê uma coleção e retorna o elemento do array na posição "pos" informada
         * Exemplos de uso no HTML:
         * usar no HTML: @using LabWebMvc.MVC.ExtensionsMethods.Genericos
         * Utils.RetornaItem(item.Emissor)
         * Utils.RetornaItem(item.Emissor, 1)
         * Utils.RetornaItem(item.Qualquer)
         * Utils.RetornaItem(Qualquer, 0)
         */

        public static string RetornaItem(dynamic codigo, string tipo, int pos = 1)
        {
            ICollection<object> lista = [];
            if (tipo == "TipoDocumento") lista = ListaDocumento();
            if (tipo == "Emissor") lista = ListaOrgaoEmissor();
            if (tipo == "Genero" || tipo == "Sexo") lista = ListaGenero();
            if (tipo == "EstadoCivil") lista = ListaEstadoCivil();
            if (tipo == "UF") lista = ListaUF();
            if (tipo == "TempoGetacao") lista = ListaTempoGestacao();
            if (tipo == "ListaSN") lista = ListaSN();
            if (tipo == "ListaTipoMapa") lista = ListaTipoMapa();

            /* Palavras dos índices para serem retiradas dos arrays */
            string[] arrLimpeza = new string[] { " ", "{", "}", "=", "Index", "Nome", "Sexo", "UF", "Check", "Descricao", "Sigla" };
            string res = string.Empty;

            for (int i = 0; i < lista.Count; i++)
            {
                string[]? arr = lista.ToArray()[i].ToString()?.Split(" ");
                res = arr == null ? "" : string.Join(" ", arr);
                foreach (string item in arrLimpeza)
                {
                    res = res.Replace(item, "");
                }
                int indice = Convert.ToInt32(res.Split(",").ToArray()[0]);
                if (indice == (int)codigo)
                {
                    return string.IsNullOrEmpty(res) ? "0" : res.Split(",")[pos];
                }
            }
            return string.Empty;
        }

        /* Lista dos Tipos de Documentos */

        public static ICollection<object> ListaDocumento()
        {
            return [ new { Index = 0, Nome = "CPF", Check = true },
                     new { Index = 1, Nome = "RG/Identidade", Check = false },
                     new { Index = 2, Nome = "CNH", Check = false },
                     new { Index = 3, Nome = "Funcional", Check = false },
                     new { Index = 4, Nome = "CNS/SUS", Check = false },
                     new { Index = 5, Nome = "CTPS", Check = false },
                     new { Index = 6, Nome = "Certidão Nascimento", Check = false },
                     new { Index = 7, Nome = "Certidão Casamento", Check = false },
                     new { Index = 8, Nome = "Outros", Check = false }
            ];
        }

        /* Lista de Orgãos Emissores dos Documentos */

        public static ICollection<object> ListaOrgaoEmissor()
        {
            return [ new { Index = 0,   Nome = "CPF/Receita Federal/MF-SRF" },
                     new { Index = 1,   Nome = "Detran" },
                     new { Index = 2,   Nome = "IFP (Instituo Felix Pacheco)" },
                     new { Index = 3,   Nome = "IPF (Instituto Pereira Faustino)" },
                     new { Index = 4,   Nome = "Funcional/Nacional" },
                     new { Index = 5,   Nome = "Exército" },
                     new { Index = 6,   Nome = "Marinha" },
                     new { Index = 7,   Nome = "Aeronáutica" },
                     new { Index = 8,   Nome = "Passaporte" },
                     new { Index = 9,   Nome = "CNS/SUS" },
                     new { Index = 10,  Nome = "Outros" }
            ];
        }

        /* Lista Gênero/Sexo */

        public static ICollection<object> ListaGenero()
        {
            return [ new { Index = 0, Sexo = "M", Nome = "Masculino", Check = true },
                                         new { Index = 1, Sexo = "F", Nome = "Feminino", Check = false }
            ];
        }

        public static ICollection<object> ListaEstadoCivil()
        {
            return [ new { Index = 0, Nome = "Outros", Check = false },
                     new { Index = 1, Nome = "Solteiro(a)", Check = true },
                     new { Index = 2, Nome = "Casado(a)", Check = false },
                     new { Index = 3, Nome = "Viúvo(a)", Check = false },
                     new { Index = 4, Nome = "Divorciado(a)", Check = false }
            ];
        }

        public static ICollection<object> ListaUF()
        {
            return [ new { Index =  0, UF = "XX", Nome = "Estrangeiro" },
                     new { Index =  1, UF = "AC", Nome = "Acre" },
                     new { Index =  2, UF = "AL", Nome = "Alagoas" },
                     new { Index =  3, UF = "AM", Nome = "Amazonas" },
                     new { Index =  4, UF = "AP", Nome = "Amapá" },
                     new { Index =  5, UF = "BA", Nome = "Bahia" },
                     new { Index =  6, UF = "CE", Nome = "Ceará" },
                     new { Index =  7, UF = "DF", Nome = "Distrito Federal" },
                     new { Index =  8, UF = "ES", Nome = "Espírito Santo" },
                     new { Index =  9, UF = "GO", Nome = "Goiás" },
                     new { Index = 10, UF = "MA", Nome = "Maranhão" },
                     new { Index = 11, UF = "MG", Nome = "Minas Gerais" },
                     new { Index = 12, UF = "MS", Nome = "Mato Grosso do Sul" },
                     new { Index = 13, UF = "MT", Nome = "Mato Grosso" },
                     new { Index = 14, UF = "PA", Nome = "Pará" },
                     new { Index = 15, UF = "PB", Nome = "Paraíba" },
                     new { Index = 16, UF = "PE", Nome = "Pernambuco" },
                     new { Index = 17, UF = "PI", Nome = "Piauí" },
                     new { Index = 18, UF = "PR", Nome = "Paraná" },
                     new { Index = 19, UF = "RJ", Nome = "Rio de Janeiro" },
                     new { Index = 20, UF = "RN", Nome = "Rio Grande do Norte" },
                     new { Index = 21, UF = "RO", Nome = "Rondônia" },
                     new { Index = 22, UF = "RR", Nome = "Roraima" },
                     new { Index = 23, UF = "RS", Nome = "Rio Grande do Sul" },
                     new { Index = 24, UF = "SC", Nome = "Santa Catarina" },
                     new { Index = 25, UF = "SE", Nome = "Sergipe" },
                     new { Index = 26, UF = "SP", Nome = "São Paulo" },
                     new { Index = 27, UF = "TO", Nome = "Tocantins" }
            ];
        }

        /* Lista meses do Tempo de Gestação */

        public static ICollection<object> ListaTempoGestacao()
        {
            return [ new { Index = 0,   Nome = "<1 mês" },
                                         new { Index = 1,   Nome = "1 mês" },
                                         new { Index = 2,   Nome = "2 meses" },
                                         new { Index = 3,   Nome = "3 meses" },
                                         new { Index = 4,   Nome = "4 meses" },
                                         new { Index = 5,   Nome = "5 meses" },
                                         new { Index = 6,   Nome = "6 meses" },
                                         new { Index = 7,   Nome = "7 meses" },
                                         new { Index = 8,   Nome = "8 meses" },
                                         new { Index = 9,   Nome = "9 meses" },
                                         new { Index = 10,  Nome = "Nenhum" }
            ];
        }

        /* Lista S/N (Sim ou Não) */

        public static ICollection<object> ListaSN()
        {
            return [ new { Index = 0, Sigla = "N", Descricao = "Não", Check = true },
                                         new { Index = 1, Sigla = "S", Descricao = "Sim", Check = false }
            ];
        }

        /* Lista 0/1 Tipo de Mapa de Trabalho (se Eletrônico-Máquina ou Convencional-Digitação) */

        public static ICollection<object> ListaTipoMapa()
        {
            return [ new { Index = 0, Sigla = "F", Descricao = "Ficha/Convencional", Check = true },
                                         new { Index = 1, Sigla = "E", Descricao = "Eletrônico/Computador", Check = false }
            ];
        }

        /* Formata Telefone em consultas  */

        public static string FormataTelefone(dynamic tel)
        {
            string? telefone = tel;

            if (tel.Length == 10)
            {
                return "(" + tel.Substring(0, 2) + ") " + tel.Substring(2, 4) + "-" + tel.Substring(6, 4);
            }
            else if (tel.Length == 11)
            {
                return "(" + tel.Substring(0, 2) + ") " + tel.Substring(2, 5) + "-" + tel.Substring(7, 4);
            }
            return telefone;
        }

        /* Formata conta do Plano de Exames */

        public static string FormataConta(dynamic conta)
        {
            if (conta == null)
            {
                throw new ArgumentNullException("Conta não pode ser nula ou vazia.");
            }
            string Temp = conta.Length < 11 ? conta.ToString().PadLeft(11, '0') : conta;
            return UtilBLL.Formatar(Temp, "##.##.###.####");
        }

        /* Formata Valor do Plano de Exames e TRUE retorna DECIMAL  ?????????????????????? TESTAR  */

        public static dynamic FormataValor(dynamic valor, bool dec = true)
        {
            if (valor == null)
            {
                valor = "0.00";
            }
            decimal value = Convert.ToDecimal(valor);
            return dec ? value.ToString("N2").ToDecimalInvariant() : value.ToString("N2");
            //return UtilBLL.Formatar(valor, "##.###,##");
        }

        /* Formata conta do Plano de Exames para as Views */

        public static string FormataContaView(dynamic conta)
        {
            string Temp = conta.Length < 11 ? conta.ToString().PadLeft(11, '0') : conta;
            Temp = Temp.Substring(2, 9);
            return UtilBLL.Formatar(Temp, "##.###.####");
        }

        /* Retorna automaticamente a sigla e/ou nome do país */

        public static string RetornaPais(int modo = 0)
        {
            RegionInfo regionInfo = RegionInfo.CurrentRegion;
            string name = regionInfo.Name;                       //sigla do país: BR, US, FR, GE etc.
            string englishName = regionInfo.EnglishName;         //nome do país em inglês: Brazil, Germany etc.
            string displayName = regionInfo.DisplayName;         //ou tentar: regionInfo.NativeName;
            string moedaSimbolo = regionInfo.ISOCurrencySymbol;  //símbolo da moeda

            if (modo == 0) return name;          //retorna a sigla do país: BR, US, FR, GE etc.
            if (modo == 1) return englishName;   //retorna o nome do país em inglês: Brazil
            if (modo == 2) return displayName;   //retorna o nome original do país: Brasil
            if (modo == 3) return moedaSimbolo;  //retorna o símbolo da moeda utilizada no país
            return name;
        }

        /* Reordena uma lista string de ICollection */

        public static ICollection<string> OrdenaICollectionList(ICollection<string> lista)
        {
            List<string> reordena = [];
            foreach (string item in lista)
                reordena.Add(item.ToString().PadLeft(10000, '0'));    /* PadLeft aqui evita, por exemplo, 3 ser confundido com 30 na order */

            reordena.Sort();
            lista = [];
            foreach (string item in reordena)
            {
                lista.Add(item.Trim());
            }
            return lista;
        }

        /* Gerador do Código de Conta Folha padrão / Retorna o Código de Folha de Exame */

        public static string RetornaCodigoFolhaExame(Db db, int contaFolha, int tipoConta = 11)
        {
            return tipoConta.ToString() + contaFolha.ToString().PadLeft(2, '0') + string.Empty.PadLeft(7, '0');
        }

        /* Retorna oparte CODIGO da Conta Principal, e, a DESCRIÇÃO da Conta Folha e da Conta Principal somente (para inserir numa conta item) */

        public static string[] RetornaDescricaoConta(Db db, int contaFolha, int contaPrincipal, int tipoConta = 11)
        {
            string strConta = tipoConta.ToString() + contaFolha.ToString().PadLeft(2, '0') + contaPrincipal.ToString().PadLeft(3, '0') + string.Empty.PadLeft(4, '0');
            string[] ret = new string[] { };
            PlanoExames? Descricao = db.PlanoExames.Where(l => l.ContaExame == strConta).FirstOrDefault();
            if (Descricao != null)
            {
                ret[0] = Descricao.ContaExame.Substring(0, 7);
                ret[1] = Descricao.Descricao.ToUpper();
                return ret;
            }
            return new string[] { "0", "ERRO" };
        }

        /* Gerador/Sequenciador do Código de CONTA PRINCIPAL (Sequencial) */

        public static string[] SequenciadorContaPrincipal(Db db, int contaFolha, int tipoConta = 11)
        {
            string conta = tipoConta.ToString() + contaFolha.ToString().PadLeft(2, '0');

            PlanoExames? sequencia = db.PlanoExames.Where(l => l.ContaExame.StartsWith(conta) && l.ContaExame.EndsWith("0000") && l.TabelaExamesId == (int)IdPadrao.SUS).OrderByDescending(o => o.ContaExame).FirstOrDefault();
            if (sequencia != null)
            {
                //11.01.000 -> 1101001..1101999
                int seq = sequencia.ContaExame.Substring(0, 7).ToInt32() + 1;
                string[] ret = { seq.ToString() + string.Empty.PadLeft(4, '0'),
                                     sequencia.RefExame.ToUpper(),
                                     sequencia.RefItem.ToUpper()
                    };

                return ret;
            } // se não conseguiu criar a sequência acima que inicia de 001 e vai até 999, então a conta folha nem existe.
            return ["ERRO"];
        }

        /* Gerador/Sequenciador do Código de CONTA ITEM (Sequencial) */

        public static string[] SequenciadorContaItem(Db db, int contaFolha, ulong contaPrincipal, int tipoConta = 11)
        {
            string conta = contaPrincipal.ToString().Substring(0, 7);  /// tipoConta.ToString() + contaFolha.ToString().PadLeft(2, '0') + contaPrincipal.ToString().PadLeft(3, '0');

            PlanoExames? sequencia = db.PlanoExames.Where(l => l.ContaExame.StartsWith(conta) && l.TabelaExamesId == (int)IdPadrao.SUS).OrderByDescending(o => o.ContaExame).FirstOrDefault();
            if (sequencia != null)
            {
                //11.01.001.0000 -> 11.01.001.0001..11.01.999.9999
                ulong seq = sequencia.ContaExame.Substring(0, 11).ToULong() + 1;
                string[] ret = { seq.ToString(),
                                     sequencia.RefExame.ToUpper(),
                                     sequencia.RefItem.ToUpper()
                    };
                return ret;
            } // se não conseguiu criar a sequência do item acima que inicia de 001 então a conta principal ou folha nem existe.
            return ["ERRO"];
        }

        /* Retorna uma mensagem de aviso contendo título e texto em HTML */

        public static string AvisoHtml(string titulo, string texto)
        {
            return "<div style='font: normal 16px calibri, arial, sans-serif !important;'>" +
                   "<div style='font-weight: 800; color: red;'>" + titulo + ":</div>" +
                   "<div style='font-weight: 400; color: red;'>" + texto + "</div>" +
                   "</div>";
        }

        /* Retorna a temperatura formatada em graus Celsius */

        public static string? TemperaturaCelsius(this decimal? temp)
        {
            if (string.IsNullOrEmpty(temp.ToString()))
                return "?ºC";

            return temp.ToString() + "°C";
        }

        /* Formata telefone de acordo com a quantidade de dígitos enviados */
        //public static string? FormataTelefone(this string? tel)
        //{
        //    if (string.IsNullOrEmpty(tel)) return "";
        //    long number = Convert.ToUInt32(tel);
        //    int tam = tel.Length;
        //    string fmt = "";
        //    if (tam == 8) fmt = "0000-0000";
        //    if (tam == 10) fmt = "(00) 0000-0000";
        //    if (tam == 11) fmt = "(00) 0-0000-0000";
        //    if (tam == 12) fmt = "(000) 0-0000-0000";
        //    if (tam > 12) return tel;

        //    return number.ToString(fmt);
        //}

        /* Para ler e gravar arquivos imagens EM BLOCOS */
        /* Tem que criar o método e testar

         string sPath = @"C:\Learning\Articles\csharp-example.PNG";
         using (FileStream fStream = new FileStream(sPath, FileMode.Open))
         {
           byte[] byteArray = new byte[1024];
           int bytesRead;
           string dPath = @"C:\Learning\tutlane1.png";
           using (FileStream fileStream2 = new FileStream(dPath, FileMode.Create))
           {
              while ((bytesRead = fStream.Read(byteArray, 0, byteArray.Length)) > 0)
              {
                 fileStream2.Write(byteArray, 0, bytesRead);
              }
           }
         }

         */

        /* ReformaTexto: Substitui termos no texto pesquisado e devolve o texto alterado */

        public static string ReformaTexto(this string texto, string pesquisa, string substituicao)
        {
            return texto.Replace(pesquisa, substituicao);
        }
    }
}