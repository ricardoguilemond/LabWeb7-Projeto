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
        private readonly Db _db;
        public ResumoUsuarioViewComponent(IConnectionService connectionService, IEventLogHelper eventLogHelper)
        {
            var options = new DbContextOptionsBuilder<Db>()
                .UseNpgsql(connectionService.GetConnectionString())
                .Options;

            _db = new Db(options, connectionService, eventLogHelper);
        }

        public Task<IViewComponentResult> InvokeAsync()
        {
            var modelo = new ResumoUsuarioViewModel
            {
                TotalReCaptcha = Utils.TotalReCaptcha(_db) ?? "N/A",
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