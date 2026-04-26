namespace LabWebMvc.MVC.Areas.ControlGeral
{
    public interface IGeralService
    {
        bool SessaoValida();

        string? ObterSessionUF(HttpContext httpContext);

        string[] GerarBotoes();

        Task<string> ObterDataHoraFormatadoAsync();

        string ObterDataHora(bool iso = false);

        object[] GerarTextoMenu(string titulo);
    }
}