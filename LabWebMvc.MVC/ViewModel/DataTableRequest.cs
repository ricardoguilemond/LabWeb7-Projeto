namespace LabWebMvc.MVC.ViewModel
{
    /// <summary>
    /// Requisição padrão enviada pelo DataTables em modo server-side processing.
    /// </summary>
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch Search { get; set; } = new DataTableSearch();
        public List<DataTableOrder> Order { get; set; } = new List<DataTableOrder>();
        public List<DataTableColumn> Columns { get; set; } = new List<DataTableColumn>();
    }

    public class DataTableSearch
    {
        public string Value { get; set; } = string.Empty;
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "desc";
    }

    public class DataTableColumn
    {
        public string Data { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch Search { get; set; } = new DataTableSearch();
    }
}
