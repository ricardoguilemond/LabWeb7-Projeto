using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;

public class BaseServico
{
    protected readonly IDbFactory _dbFactory;
    protected Db _db;

    public BaseServico(IDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
        _db = _dbFactory.Create();
    }
}