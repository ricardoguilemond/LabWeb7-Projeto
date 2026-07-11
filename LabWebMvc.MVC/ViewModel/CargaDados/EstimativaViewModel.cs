namespace LabWebMvc.MVC.ViewModel.CargaDados
{
    public class EstimativaTabelaViewModel
    {
        public string NomeFirebird { get; set; } = string.Empty;
        public string NomePostgreSQL { get; set; } = string.Empty;
        public long TotalRegistros { get; set; }
        public long RegistrosExistentes { get; set; }
        public long RegistrosNovos { get; set; }
        public TimeSpan TempoEstimado { get; set; }
        public int Ordem { get; set; }
        public List<string> Incompatibilidades { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
    }

    public class EstimativaViewModel
    {
        public List<EstimativaTabelaViewModel> Tabelas { get; set; } = new();
        public TimeSpan TempoTotalEstimado { get; set; }
        public long TotalRegistros { get; set; }
        public long TotalNovos { get; set; }
        public bool PodeProsseguir { get; set; }
        public List<string> ErrosBloqueantes { get; set; } = new();
        public string StringConexaoFirebird { get; set; } = string.Empty;
        public int TamanhoLote { get; set; } = 1000;
        public bool ModoSimulacao { get; set; }
    }
}
