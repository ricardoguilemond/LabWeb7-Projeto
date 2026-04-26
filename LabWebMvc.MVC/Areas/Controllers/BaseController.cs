using ExtensionsMethods.EventViewerHelper;
using ExtensionsMethods.ValidadorDeSessao;
using LabWebMvc.MVC.Areas.Concorrencias;
using LabWebMvc.MVC.Areas.ControleDeImagens;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
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
        protected readonly ExclusaoService _exclusaoService;
        protected readonly IConnectionService _connectionServiceBase;

        protected Db _db;

        protected BaseController(IDbFactory dbFactory, 
                                 IValidadorDeSessao validador,
                                 GeralController geralController, 
                                 IEventLogHelper eventLogHelper,
                                 Imagem imagem,
                                 ExclusaoService exclusaoService,
                                 IConnectionService connectionService)
        {
            _dbFactory = dbFactory;
            _validador = validador;
            _geralController = geralController;
            _eventLogHelper = eventLogHelper;
            _imagem = imagem;
            _exclusaoService = exclusaoService;
            _connectionServiceBase = connectionService;

            _db = _dbFactory.Create();
        }

        /// <summary>
        /// Restaura a conexão correta do tenant (empresa) a partir da sessão em cada requisição.
        /// Garante que o banco de dados correto seja usado após o login multi-tenant.
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string? sessionConn = HttpContext.Session.GetString("SessionStringConexao");
            if (!string.IsNullOrEmpty(sessionConn))
            {
                _connectionServiceBase.SetConnectionString(sessionConn);
                var optionsBuilder = new DbContextOptionsBuilder<Db>().UseNpgsql(sessionConn);
                _db = new Db(optionsBuilder.Options, _connectionServiceBase, _eventLogHelper);
            }
            base.OnActionExecuting(context);
        }
    }
}