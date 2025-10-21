using LabWebMvc.MVC.Areas.Connections;
using Microsoft.AspNetCore.Mvc;

namespace LabWebMvc.MVC.Areas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController : ControllerBase
    {
        private readonly MySettingsConfiguration? _configuration;

        public ConnectionController(MySettingsConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public bool Get()
        {
            return _configuration != null && _configuration.Parameters != null ? _configuration.Parameters.IsProduction : false;
        }
    }
}