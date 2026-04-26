namespace LabWebMvc.MVC.Integracoes
{
    public partial class Enum
    {
        /// <summary>
        /// ATENÇÃO: CUIDADO AO MEXER NA CLASSE, POIS TERÁ IMPACTOS NOS SERVIÇOS DE INTEGRAÇÕES
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public class AttributeEnumType : Attribute
        {
            public Type type { get; set; }

            public AttributeEnumType(Type type)
            {
                this.type = type;
            }
        }

        public enum TipoArquivoIntegracoes
        {
            ExportacaoCadastroPacientes = 1,
            ExportacaoMedicos = 2,
            ExportacaoInstitucoes = 3,
            ExportacaoExames = 4
        }

        public enum IntegracaoExecucaoArquivoStatus
        {
            Sucesso = 1,
            Erro = 2
        }

        public enum TipoPeriodoExtracao
        {
            /* Este Enum controla a geração de períodos para a geração de arquivos de integração */
            Diario = 1,      /* gera arquivos do dia anterior */
            Semanal = 2,     /* gera arquivos somente da semana anterior */
            Mensal = 3,      /* gera arquivos somente do mês anterior */
            Retroativo = 4   /* gera arquivos retroativos de qualquer período definido pelo usuário via tabela! */
        }

        public enum DiaDaSemana
        {
            /* padrão na documentação Microsoft */
            Domingo = 0,
            Segunda = 1,
            Terca = 2,
            Quarta = 3,
            Quinta = 4,
            Sexta = 5,
            Sabado = 6
        }

        public enum TipoCampo
        {
            Char,
            Dec,
            Num,
            Date
        }
    }
}