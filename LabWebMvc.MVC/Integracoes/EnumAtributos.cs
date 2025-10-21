using System.Reflection;
using static LabWebMvc.MVC.Integracoes.Integracoes;

namespace LabWebMvc.MVC.Integracoes
{
    public static class EnumAtributos
    {
        /// <summary>
        /// ATENÇÃO: CUIDADO AO MEXER NA CLASSE, POIS TERÁ IMPACTOS NOS SERVIÇOS DE INTEGRAÇÕES
        /// </summary>
        public static T? GetAttribute<T>(this LayoutExecucao enumValue) where T : Attribute
        {
            T? attribute;
            MemberInfo? memberInfo = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();

            if (memberInfo != null)
            {
                attribute = (T?)memberInfo.GetCustomAttributes(typeof(T), false).FirstOrDefault();
                return attribute;
            }
            return null;
        }
    }
}