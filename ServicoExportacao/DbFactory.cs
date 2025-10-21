using ExtensionsMethods.EventViewerHelper;
using LabWebMvc.MVC.Areas.ServicosDatabase;
using LabWebMvc.MVC.Models;
using Microsoft.EntityFrameworkCore;

public class DbFactory : IDbFactory
{
    private readonly IConnectionService _connectionService;
    private readonly IEventLogHelper _eventLogHelper;

    public DbFactory(IConnectionService connectionService, IEventLogHelper eventLogHelper)
    {
        _connectionService = connectionService;
        _eventLogHelper = eventLogHelper;
    }

    public Db Create()
    {
        var optionsBuilder = new DbContextOptionsBuilder<Db>()
            .UseNpgsql(_connectionService.GetConnectionString());

        return new Db(optionsBuilder.Options, _connectionService, _eventLogHelper);
    }
}