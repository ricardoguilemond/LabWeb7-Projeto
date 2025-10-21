using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

namespace LabWebMvc.MVC.Areas.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly IDbFactory _dbFactory;
        protected readonly IValidadorDeSessao _validador;
        protected readonly GeralController _geralController;
        protected readonly IEventLogHelper _eventLogHelper;
        protected readonly Imagem _imagem;

        protected Db _db;

        protected BaseController(IDbFactory dbFactory, 
                                 IValidadorDeSessao validador,
                                 GeralController geralController, 
                                 IEventLogHelper eventLogHelper,
                                 Imagem imagem)
        {
            _dbFactory = dbFactory;
            _validador = validador;
            _geralController = geralController;
            _eventLogHelper = eventLogHelper;
            _imagem = imagem;

            _db = _dbFactory.Create();
        }
    }
}