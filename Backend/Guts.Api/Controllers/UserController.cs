using System.Net;
using Guts.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/users")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerBase
    {
        [HttpGet("current/profile")]
        [ProducesResponseType(typeof(UserProfileModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult GetCurrentUserProfile()
        {
            //TODO: use this service to have a user profile at client side.
            var model = new UserProfileModel
            {
                Id = GetUserId(),
                Roles = GetUserRoles()
            };
            return Ok(model);
        }
    }
}