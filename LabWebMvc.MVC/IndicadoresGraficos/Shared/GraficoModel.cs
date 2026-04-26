namespace LabWebMvc.MVC.IndicadoresGraficos.Shared
{
    public class GraficoModel
    {
        public string CanvasId { get; set; } = "grafico_" + Guid.NewGuid().ToString("N");    //fallback, criado mas pode ser sobrescrito    

        // Cabeçalho
        public string Titulo { get; set; } = "";
        public string Subtitulo { get; set; } = "";

        // Dados do gráfico
        public List<string> Labels { get; set; } = new();
        public List<int> Valores { get; set; } = new();
        public string TipoGrafico { get; set; } = "line";   // ou "bar", "pie", etc.

        // Rodapé
        public string RodapeTextoPrincipal { get; set; } = "";
        public string RodapeSubtexto { get; set; } = "";
        public string RodapeIconeCss { get; set; } = "";
        public string RodapeLinkTexto { get; set; } = "";
        public string RodapeLinkUrl { get; set; } = "";
        public bool RodapeMostrarAlerta { get; set; } = false;
    }
}