namespace ExtensionsMethods.EventViewerHelper
{
    public interface IEventLogHelper
    {
        Func<string?>? ObterCNPJ { get; set; }
        Func<string?>? ObterNomeEmpresa { get; set; }

        void LogEventViewer(string mensagem, string tipoEvento = "wInfo", string origem = "LabWebMvc");

        Task LogEventViewerAsync(string mensagem, string tipoEvento = "wInfo", string origem = "LabWebMvc");
    }
}