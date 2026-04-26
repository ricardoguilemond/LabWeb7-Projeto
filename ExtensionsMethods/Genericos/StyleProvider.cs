namespace ExtensionsMethods.Genericos
{
    public class StyleProvider
    {
        /* Nesta classe estão os estilos obrigatórios, que por razões específicas, queremos manter sem o risco de modificações externas */

        public string GetStyle()
        {
            /*
               .has-error ::: Importante: NÃO RETIRAR OU MODIFICAR
                              É usado para definir o tamanho das fontes em mensagens de erro nas Views através de has-error em @Html.ValidationMessageFor dos ErrorMessage das VMs

               USO: basta colocar as linhas abaixo @{...} no início das páginas razor cshtml

                 @{
                     var styleProvider = new StyleProvider();
                     var style = styleProvider.GetStyle();
                  }
                  @Html.Raw(style)
            */

            return @"<style>
                    .has-error {
                        float: left;
                        margin-left: 2px;
                        display: inline-block;
                        font-family: tahoma, calibri, arial, sans-serif;
                        font-size: 12.5px !important;
                        font-weight: 400;
                        color: red;
                        width: 100%;
                    }
                    </style>";
        }

        //public void AlterarMensagemDeErro<T>(string campo, string novaMensagem) where T : class
        //{
        //    campo = campo.Split(".").LastOrDefault();
        //    PropertyInfo propertyInfo = typeof(T).GetProperty(campo, typeof(string));
        //    if (propertyInfo != null)
        //    {
        //        RequiredAttribute? attribute = propertyInfo.GetCustomAttribute<RequiredAttribute>();
        //        if (attribute != null)
        //        {
        //            attribute.ErrorMessage = novaMensagem;
        //        }
        //    }
        //}
    }//Fim
}