namespace LabWebMvc.MVC.ViewModel
{
    public class vmGeral
    {
        /*
         * Estrutura dos botões de Input
         */
        public string? BotaoTipo { get; set; }
        public string? BotaoNome { get; set; }
        public string? BotaoId { get; set; }
        public string? BotaoValor { get; set; }
        public string? BotaoClasse { get; set; }

        /*
         * Todos as declarações abaixo possuem métodos em GeralController.cs
         */

        //Gênero/Sexo
        public string? TipoGenero { get; set; }

        //Tipo de Documento de Identificação
        public int TipoDocumento { get; set; }

        //Órgão emissor do documento de identificação
        public int TipoOrgaoEmissor { get; set; }

        //Estado Civil
        public int TipoEstadoCivil { get; set; }

        //UF
        public string? TipoUF { get; set; }

        //Tempo de Gestação
        public int TipoTempoGestacao { get; set; }

        /*
           Avisos ou mensagens gerais aplicadas nas Views.
         */
        public string? Aviso { get; set; }
    }
}