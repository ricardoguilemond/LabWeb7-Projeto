namespace ExtensionsMethods.Genericos
{
    public static class Enumeradores
    {
        public enum TipoContaBloqueado : int
        {
            Nao = 0,
            Sim = 1
        }

        public enum TipoSituacaoLogin : int
        {
            ComRestricao = 0,      //restrição
            SemRestricao = 1,      //tudo ok, verificado, liberado
            SemVerificacao = 2     //em fase de verificação/validação
        }

        public enum TipoEmailConfirmado : int
        {
            Nao = 0,         //quando está recém criado ou na fase de verificação/validação
            Sim = 1,         //email validado
            Inexistente = 2  //quando o e-mail for inexistente
        }

        /* Conta SUS é padrão no Plano de Exames para todos as demais Instituições do Plano */

        public enum IdPadrao : int
        {
            SUS = 1
        }

        /* Tipos de Conta Exame  */

        public enum TipoContaExame : int
        {
            Principal = 0,
            Item = 1
        }

        /* Acompanha ListaDocumento() e tabelas */

        public enum TipoDocumento
        {
            CPF = 0,
            RG = 1,
            CNH = 2,
            Funcional = 3,
            SUS = 4,
            CTPS = 5,
            Nascimento = 6,
            Casamento = 7,
            Outros = 8,

            // Alias interno para SUS
            CNS = SUS
        }

        public enum TipoGenero : int
        {
            M = 0,   //Masculino
            F = 1    //Feminino
        }
    }
}