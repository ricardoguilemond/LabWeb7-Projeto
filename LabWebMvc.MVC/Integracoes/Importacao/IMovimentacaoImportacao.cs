namespace LabWebMvc.MVC.Integracoes.Importacao
{
    public interface IMovimentacaoImportacao
    {
        void ProcessaMovimentacao(MovimentacaoImportacaoParameter parameter);
    }

    public class MovimentacaoImportacaoParameter
    {
        //Tabelas que podem ter registros importados
        //public Assinaturas assinaturas { get; set; }
        //public ClasseExames classeExames { get; set; }
        //public Pacientes pacientes { get; set; }
        //public Empresa empresa { get; set; }
        //public ExamesRealizados examesRealizados { get; set; }
        //public ExamesRealizadosAM ExamesRealizadosAM { get; set; }
        //public Instituicao instituicao { get; set; }
        //public ItensExamesRealizados itensExamesRealizados { get; set; }
        //public ItensExamesRealizadosAM itensExamesRealizadosAM { get; set; }
        //public Logradouro logradouro { get; set; }
        //public Medicos medicos { get; set; }
        //public PlanoExames planoExames { get; set; }
        //public Requisitar Requisitar { get; set; }
        //public SituacaoExames situacaoExames { get; set; }
        //public TabelaExames tabelaExames { get; set; }
        //public TextosProntos textosProntos { get; set; }

        //Variáveis de importação
        public string? NomeTabela { get; set; }
    }
}