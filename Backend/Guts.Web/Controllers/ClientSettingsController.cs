using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Guts.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/clientsettings")]
    public class ClientSettingsController : Controller
    {
        private readonly IOptions<ClientSettings> _apiSettings;

        public ClientSettingsController(IOptions<ClientSettings> apiSettings)
        {
            _apiSettings = apiSettings;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_apiSettings.Value);
        }
    }
}