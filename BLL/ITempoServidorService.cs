namespace BLL
{
    public interface ITempoServidorService
    {
        string ObterDataHoraServidor(string? formato = null);    //síncrono

        Task<DateTime?> ObterDataHoraServidorAsync();

        Task<string> ObterDataHoraServidorFormatadoAsync(string? formato = null);
    }

    /*
        USAR ASSIM NO CONTROLLER: (tem outro exemplo no GeralController)
        public class RelatorioController : Controller
        {
            private readonly ITempoServidorService _tempoService;

            public RelatorioController(ITempoServidorService tempoService)
            {
                _tempoService = tempoService;
            }

            public async Task<IActionResult> Index()
            {
                var dataHoraServidor = await _tempoService.ObterDataHoraServidorAsync();
                ViewBag.DataHora = dataHoraServidor?.ToString("dd/MM/yyyy HH:mm:ss");

                return View();
            }
        }

     */
}