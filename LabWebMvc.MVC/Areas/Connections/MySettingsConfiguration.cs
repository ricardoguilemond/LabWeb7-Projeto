namespace LabWebMvc.MVC.Areas.Connections
{
    public sealed class MySettingsConfiguration
    {
        public bool Log { get; set; }
        public string? ConnectionStringId { get; set; }
        public Parameters? Parameters { get; set; }
    }

    public sealed class Parameters
    {
        public bool IsProduction { get; set; }
    }
}