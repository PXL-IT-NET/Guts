using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Route("")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        [Route("")]
        public IActionResult Index()
        {
            return RedirectPermanent("~/swagger");
        }
    }
}