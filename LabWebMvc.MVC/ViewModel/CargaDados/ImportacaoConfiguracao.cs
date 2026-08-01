namespace LabWebMvc.MVC.ViewModel.CargaDados
{
    public class ImportacaoConfiguracao
    {
        public string StringConexaoFirebird { get; set; } = string.Empty;
        public List<string> TabelasSelecionadas { get; set; } = new();
        public int TamanhoLote { get; set; } = 1000;
        public bool ModoSimulacao { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public bool IgnorarErros { get; set; } = true;
    }
}
