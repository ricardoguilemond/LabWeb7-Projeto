using LabWebMvc.MVC.Models;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace LabWebMvc.MVC.Areas.Validations
{
    public abstract class Base
    { }

    public class FeriadoDetalhe
    {
        public int ID { get; set; }
        public int Dia { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string? Descricao { get; set; }
    }

    public class ConfiguracoesDeServicos
    {
        public string? UploadFoldERTemporario { get; set; }
        public string? UploadFolderFinal { get; set; }
    }

    public static class Utils
    {
        private static IConfigurationRoot configuration = null!;

        //public static dynamic MensagemStartUp(this string nome, bool modoMenuFlutuante = false)
        //{
        //    var statusUp = string.Format("{0} ::: {1}", nome, Mensagens.Mensagens_pt_BR.StatusUp);
        //    dynamic[] colecao = { statusUp, modoMenuFlutuante }; //false = não abre quadro flutuante popup de subopções do menu à direita da tela
        //    return colecao;
        //}

        public static IConfigurationRoot Ambiente()
        {
            string? envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string pathRaizJson = Areas.Utils.Utils.GetPathAppSettingsJson() + $"\\appsettings.{envName}.json";
            string strJsonSettings = "appsettings.json";   //usado em Produção

            if (!string.IsNullOrEmpty(envName) && string.Equals(envName, "DEVELOPMENT", StringComparison.OrdinalIgnoreCase))
                if (File.Exists(pathRaizJson))
                    strJsonSettings = $"appsettings.{envName}.json";

            // em "configuration" retornamos todas as configurações existentes no "appsettings.json" do Core!
            configuration = new ConfigurationBuilder()
                                .SetBasePath(Areas.Utils.Utils.GetPathAppSettingsJson())
                                .AddJsonFile(strJsonSettings, optional: false)
                                .Build();
            return configuration;
        }

        public static int[] VersaoMySQL()
        {
            configuration = Ambiente();

            int[] versao = new int[] { 0, 0, 0 };

            if (configuration.GetSection("ConexaoMySQL").GetSection("versaoMajor").Exists())
                versao[0] = Convert.ToInt32(configuration.GetSection("ConexaoMySQL").GetSection("versaoMajor").Value);
            if (configuration.GetSection("ConexaoMySQL").GetSection("versaoMinor").Exists())
                versao[1] = Convert.ToInt32(configuration.GetSection("ConexaoMySQL").GetSection("versaoMinor").Value);
            if (configuration.GetSection("ConexaoMySQL").GetSection("versaoBuild").Exists())
                versao[2] = Convert.ToInt32(configuration.GetSection("ConexaoMySQL").GetSection("versaoBuild").Value);

            return versao;
        }

        public static string VariavelAppJsonSettings(string nomeVariavel, string sessao = "ConfiguracoesDeServicos")
        {
            string? valor = string.Empty;
            string? envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string? pathRaizJson = Areas.Utils.Utils.GetPathAppSettingsJson() + $"\\appsettings.{envName}.json";
            string strJsonSettings = "appsettings.json";   //usado em Produção

            if (!string.IsNullOrEmpty(envName) && string.Equals(envName, "DEVELOPMENT", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(pathRaizJson))
                    strJsonSettings = $"appsettings.{envName}.json";
            }
            IConfigurationRoot configuration = new ConfigurationBuilder()
                                                  .SetBasePath(Areas.Utils.Utils.GetPathAppSettingsJson())
                                                  .AddJsonFile(strJsonSettings, optional: false)
                                                  .Build();

            if (configuration.GetSection(sessao).Exists())
            {
                //Pego a sessão de configuração do Email (AppSettings) e capturo as variáveis que estão lá...
                IConfigurationSection sectionServicos = configuration.GetSection(sessao);
                valor = sectionServicos.GetSection(nomeVariavel)?.Value;
            }
            if (valor == null) valor = "";
            return valor;
        }

        public static IEnumerable DynamicSqlQuery(Db database, string sql, params object[] parameters)
        {
            return DynamicSqlQuery(database, sql, 30, parameters);
        }

        public static IEnumerable DynamicSqlQueryCore(Db database, string sql, params object[] parameters)
        {
            return DynamicSqlQueryCore(database, sql, 30, parameters);
        }

        //TODO// //public static IEnumerable DynamicSqlQueryCore(Db db, string Sql, Dictionary<string, object> Params)
        //{
        //    using (var cmd = db.Database.GetDbConnection().CreateCommand())
        //    {
        //        cmd.CommandText = Sql;
        //        if (cmd.Connection?.State != System.Data.ConnectionState.Open) { cmd.Connection?.Open(); }

        //        foreach (KeyValuePair<string, object> p in Params)
        //        {
        //            DbParameter dbParameter = cmd.CreateParameter();
        //            dbParameter.ParameterName = p.Key;
        //            dbParameter.Value = p.Value;
        //            cmd.Parameters.Add(dbParameter);
        //        }

        //        using (var dataReader = cmd.ExecuteReader())
        //        {
        //            while (dataReader.Read())
        //            {
        //                var row = new ExpandoObject() as IDictionary<string, object>;
        //                for (var fieldCount = 0; fieldCount < dataReader.FieldCount; fieldCount++)
        //                {
        //                    row.Add(dataReader.GetName(fieldCount), dataReader[fieldCount]);
        //                }
        //                yield return row;
        //            }
        //        }
        //    }
        //}

        //Eu usava esta antes do Core! Então precisa resolver apenas o "return"
        //public static IEnumerable DynamicSqlQuery(Db database, string sql, params object[] parameters)
        //{
        //    TypeBuilder builder = Utils.CreateTypeBuilder("MyDynamicAssembly", "MyDynamicModule", "MyDynamicType");

        //    using (System.Data.IDbCommand command = database.Database.GetDbConnection().CreateCommand())
        //    {
        //        try
        //        {
        //            if (command.Connection?.State != System.Data.ConnectionState.Open)
        //                command.Connection?.Open();

        //            command.CommandText = sql;
        //            command.CommandTimeout = command.Connection.ConnectionTimeout;
        //            foreach (var param in parameters)
        //            {
        //                command.Parameters.Add(param);
        //            }

        //            using (System.Data.IDataReader reader = command.ExecuteReader())
        //            {
        //                var schema = reader.GetSchemaTable();

        //                foreach (System.Data.DataRow row in schema.Rows)
        //                {
        //                    string name = (string)row["ColumnName"];
        //                    //var a=row.ItemArray.Select(d=>d.)
        //                    Type type = (Type)row["DataType"];
        //                    if (type != typeof(string) && (bool)row.ItemArray[schema.Columns.IndexOf("AllowDbNull")])
        //                    {
        //                        type = typeof(Nullable<>).MakeGenericType(type);
        //                    }
        //                    Utils.CreateAutoImplementedProperty(builder, name, type);
        //                }
        //            }
        //        }
        //        finally
        //        {
        //            command.Connection = null;
        //            database.Database.CloseConnection();
        //            command.Parameters.Clear();
        //        }
        //    }

        //    Type resultType = builder.CreateType();

        //    return database.SqlQuery(resultType, sql, parameters);   //database.SqlQuery(resultType, sql, parameters);
        //}

        public static dynamic? FirstOrDefault(IEnumerable items)
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
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);

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

        public static string MoveFileForFinalDestination(string tempfile)
        {
            string uploadTemp = VariavelAppJsonSettings("UploadFoldERTemporario");
            string uploadFinal = VariavelAppJsonSettings("UploadFolderFinal");

            string tempPath = Path.Combine(uploadTemp, tempfile);
            string finalPath = Path.Combine(uploadFinal, tempfile);
            string? dir = Path.GetDirectoryName(finalPath);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Copy(tempPath, finalPath);

            return finalPath;
        }
    }
}