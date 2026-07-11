using System.ComponentModel.DataAnnotations;

namespace LabWebMvc.MVC.ViewModel.CargaDados
{
    public class TabelaSelecaoViewModel
    {
        public string NomeFirebird { get; set; } = string.Empty;
        public string NomePostgreSQL { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public bool Selecionada { get; set; }
        public bool Habilitada { get; set; } = true;
        public string? MotivoDesabilitada { get; set; }
    }

    public class SelecaoTabelasViewModel
    {
        public List<TabelaSelecaoViewModel> Tabelas { get; set; } = new();
        public string StringConexaoFirebird { get; set; } = string.Empty;
        public int TamanhoLote { get; set; } = 1000;
        public bool ModoSimulacao { get; set; }
    }
}
