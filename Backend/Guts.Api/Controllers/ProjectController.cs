using System;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses/{courseId}/projects")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectController : ControllerBase
    {
        [HttpGet("{code}")]
        [ProducesResponseType(typeof(ProjectDetailModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProjectDetails(int courseId, string code)
        {
            //TODO: write tests (get components, get team(s))
            throw new NotImplementedException();
        }
    }
}