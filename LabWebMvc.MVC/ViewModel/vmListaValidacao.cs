namespace LabWebMvc.MVC.ViewModel
{
    public class vmListaValidacao<T>
    {
        public string RetornoDeRota { get; init; } = "";
        public string Titulo { get; init; } = "";
        public int TotalRegistros { get; init; }
        public int TotalTabela { get; init; }
        public string? PartialView { get; init; }
        public List<T> ListaDados { get; set; } = new List<T>();
        public vmPlanoExames PlanoExames { get; set; } = new vmPlanoExames();

    }
}