using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabWebMvc.MVC.Views.Shared.Components.MenuDinamico
{
    public class MenuDinamicoViewComponent : ViewComponent
    {
        private readonly Db _db;
        public MenuDinamicoViewComponent(IConnectionService connectionService, IEventLogHelper eventLogHelper)
        {
            var options = new DbContextOptionsBuilder<Db>().UseNpgsql(connectionService.GetConnectionString()).Options;
            _db = new Db(options, connectionService, eventLogHelper);
        }

        public IViewComponentResult Invoke()
        {
            try
            {
                List<ControleDePerfilMenu> menu = _db.ControleDePerfilMenu.OrderBy(o => o.Coluna).ThenBy(o => o.Nivel).ToList();
                return View(menu);
            }
            catch (Exception ex)
            {
                var eventLog2 = new ExtensionsMethods.EventViewerHelper.EventLogHelper();
                eventLog2.LogEventViewer("'Layout': Erro na montagem do menu: " + ex.Message, "wError");
                return View(new List<ControleDePerfilMenu>());
            }
        }
    }
}