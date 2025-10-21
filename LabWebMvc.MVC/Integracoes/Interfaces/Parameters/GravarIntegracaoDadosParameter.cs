namespace LabWebMvc.MVC.Integracoes.Interfaces.Parameters
{
    public class GravarIntegracaoDadosParameter
    {
        #region Properties

        public virtual int TipoServico { get; set; }

        public virtual string? NomeArquivo { get; set; }

        public virtual string? Resumo { get; set; }

        public virtual string? Header { get; set; }

        public virtual string? Summary { get; set; }

        public virtual bool Sucesso { get; set; }

        public virtual int DiaInicio { get; set; }

        public virtual int MesInicio { get; set; }

        public virtual int AnoInicio { get; set; }

        public virtual int HoraInicio { get; set; }

        public virtual int MinutoInicio { get; set; }

        #endregion Properties
    }
}