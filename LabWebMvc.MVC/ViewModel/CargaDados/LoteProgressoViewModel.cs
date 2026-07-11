namespace LabWebMvc.MVC.ViewModel.CargaDados
{
    public class LoteProgressoViewModel
    {
        public int PorcentagemTotal { get; set; }
        public string TabelaAtual { get; set; } = string.Empty;
        public long RegistrosProcessados { get; set; }
        public long TotalRegistros { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Mensagem { get; set; }
        public bool EmExecucao { get; set; }
        public bool Erro { get; set; }
        public bool RequerDecisao { get; set; }
        public string? ErroDetalhe { get; set; }
    }
}
