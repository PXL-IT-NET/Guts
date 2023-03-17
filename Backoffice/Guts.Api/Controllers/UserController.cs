using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Business.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IProjectTeamRepository _projectTeamRepository;

        public UserController(IProjectTeamRepository projectTeamRepository)
        {
            _projectTeamRepository = projectTeamRepository;
        }

        [HttpGet("current/profile")]
        [ProducesResponseType(typeof(UserProfileModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var model = new UserProfileModel
            {
                Id = GetUserId(),
                Roles = GetUserRoles(),
                Teams = (await _projectTeamRepository.GetByUserAsync(GetUserId())).Select(team => team.Id).ToList()
            };
            return Ok(model);
        }
    }
}