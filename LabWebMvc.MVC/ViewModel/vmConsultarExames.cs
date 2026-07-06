namespace LabWebMvc.MVC.ViewModel
{
    public class vmConsultarExames
    {
        /* Propriedade para grid Index (migrada de ViewBag.ListaDados) */
        public ICollection<dynamic> ListaDados { get; set; } = [];
    }
}
