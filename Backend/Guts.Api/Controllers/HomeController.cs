using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        [Route("")]
        public IActionResult Index()
        {
            return RedirectPermanent("~/docs");
        }
    }
}