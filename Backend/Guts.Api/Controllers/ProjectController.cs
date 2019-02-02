using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Guts.Domain;
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
        private readonly IProjectService _projectService;
        private readonly IProjectConverter _projectConverter;
        private readonly ITeamConverter _teamConverter;

        public ProjectController(IProjectService projectService, 
            IProjectConverter projectConverter,
            ITeamConverter teamConverter)
        {
            _projectService = projectService;
            _projectConverter = projectConverter;
            _teamConverter = teamConverter;
        }

        [HttpGet("{projectCode}")]
        [ProducesResponseType(typeof(ProjectDetailModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProjectDetails(int courseId, string projectCode)
        {
            if (courseId < 1 || string.IsNullOrEmpty(projectCode))
            {
                return BadRequest();
            }

            Project project;
            if (IsLector())
            {
                project = await _projectService.LoadProjectAsync(courseId, projectCode);
            }
            else
            {
                project = await _projectService.LoadProjectForUserAsync(courseId, projectCode, GetUserId());
            }
 
            var model = _projectConverter.ToProjectDetailModel(project);

            return Ok(model);
        }

        [HttpGet("{projectCode}/teams")]
        [ProducesResponseType(typeof(IList<TeamDetailsModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProjectTeams(int courseId, string projectCode)
        {
            if (courseId < 1 || string.IsNullOrEmpty(projectCode))
            {
                return BadRequest();
            }

            var teams = await _projectService.LoadTeamsOfProjectAsync(courseId, projectCode);

            var models = teams.Select(team => _teamConverter.ToTeamDetailsModel(team)).ToList();

            return Ok(models);
        }

        [HttpPost("{projectCode}/teams/{teamId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)] 
        public async Task<IActionResult> JoinProjectTeam(int courseId, string projectCode, int teamId)
        {
            //TODO: technical dept -> write unit tests
            if (courseId < 1 || string.IsNullOrEmpty(projectCode) || teamId < 1)
            {
                return BadRequest();
            }

            var allTeams = await _projectService.LoadTeamsOfProjectAsync(courseId, projectCode);
            var currentTeam = allTeams.FirstOrDefault(t => t.TeamUsers.Any(tu => tu.UserId == GetUserId()));
            if (currentTeam != null)
            {
                ModelState.AddModelError("OtherTeam", $"You are already a member of '{currentTeam.Name}'. It is not allowed to be in multiple teams.");
                return Conflict(ModelState);
            }

            var targetTeam = allTeams.FirstOrDefault(t => t.Id == teamId);
            if (targetTeam == null)
            {
                ModelState.AddModelError("InvalidTeam", $"The team you want to join is not a team of project '{projectCode}'.");
                return BadRequest(ModelState);
            }

            await _projectService.AddUserToProjectTeam(teamId, GetUserId());
            return Ok();
        }
    }
}