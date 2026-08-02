namespace LabWebMvc.MVC.ViewModel
{
    /// <summary>
    /// Resposta padrão esperada pelo DataTables em modo server-side processing.
    /// </summary>
    /// <typeparam name="T">Tipo dos registros retornados.</typeparam>
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<T> Data { get; set; } = new List<T>();
    }
}
