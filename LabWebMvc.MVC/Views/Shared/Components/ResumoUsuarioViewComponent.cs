using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Areas.Utils;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Views.Shared.Components
{
    public class ResumoUsuarioViewComponent : ViewComponent
    {
        private readonly IConnectionService _connectionService;
        private readonly IEventLogHelper _eventLogHelper;

        public ResumoUsuarioViewComponent(IConnectionService connectionService, IEventLogHelper eventLogHelper)
        {
            _connectionService = connectionService;
            _eventLogHelper = eventLogHelper;
        }

        public Task<IViewComponentResult> InvokeAsync()
        {
            string? sessionConn = HttpContext.Session.GetString("SessionStringConexao");
            string connStr = !string.IsNullOrEmpty(sessionConn)
                ? sessionConn
                : _connectionService.GetConnectionString();

            var options = new DbContextOptionsBuilder<Db>()
                .UseNpgsql(connStr)
                .Options;

            using Db db = new(options, _connectionService, _eventLogHelper);

            var modelo = new ResumoUsuarioViewModel
            {
                TotalReCaptcha = Utils.TotalReCaptcha(db) ?? "N/A",
                CNPJ = Utils.LoginCNPJEmpresaLogado() ?? "N/A",
                Nome = Utils.LoginNomeLogado() ?? "Usuário não identificado"
            };

            return Task.FromResult<IViewComponentResult>(View(modelo));
        }
    }

    public class ResumoUsuarioViewModel
    {
        public string? TotalReCaptcha { get; set; }
        public string? CNPJ { get; set; }
        public string? Nome { get; set; }
    }
}