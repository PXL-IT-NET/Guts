using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        [Route("")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectPermanent("~/swagger");
        }
    }
}