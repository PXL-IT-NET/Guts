using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses/{courseId}/projects")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProjectController : ControllerBase
    {
        public const int CacheTimeInSeconds = 300;

        private readonly IProjectService _projectService;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IProjectConverter _projectConverter;
        private readonly ITopicConverter _topicConverter;
        private readonly ITeamConverter _teamConverter;
        private readonly IMemoryCache _memoryCache;

        public ProjectController(IProjectService projectService,
            IAssignmentRepository assignmentRepository,
            IProjectConverter projectConverter,
            ITopicConverter topicConverter,
            ITeamConverter teamConverter,
            IMemoryCache memoryCache)
        {
            _projectService = projectService;
            _assignmentRepository = assignmentRepository;
            _projectConverter = projectConverter;
            _topicConverter = topicConverter;
            _teamConverter = teamConverter;
            _memoryCache = memoryCache;
        }

        [HttpGet("{projectCode}")]
        [ProducesResponseType(typeof(ProjectDetailModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
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

        [HttpPost("{projectCode}/teams/generate")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GenerateProjectTeams(int courseId, string projectCode, [FromBody] TeamGenerationModel model)
        {
            //TODO: technical dept -> write unit tests
            if (courseId < 1 || string.IsNullOrEmpty(projectCode))
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!IsLector())
            {
                return Forbid();
            }

            await _projectService.GenerateTeamsForProject(courseId, projectCode, model.TeamBaseName, model.NumberOfTeams);
            
            return Ok();
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

        /// <summary>
        /// Retrieves an overview of the testresults for a project of a course (for the current period).
        /// The overview contains testresults for the team of the authorized user.
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="projectCode"></param>
        /// <param name="teamId">Identifier of the team for which the summary should be retrieved.</param>
        /// <param name="date">Optional date parameter. If provided the status of the summary on that date will be returned.</param>
        [HttpGet("{projectCode}/teams/{teamId}/summary")]
        [ProducesResponseType(typeof(TopicSummaryModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProjectSummary(int courseId, string projectCode, int teamId, [FromQuery] DateTime? date)
        {
            //TODO: write tests
            if (courseId < 1 || string.IsNullOrEmpty(projectCode) || teamId < 1)
            {
                return BadRequest();
            }
            try
            {
                var project = await _projectService.LoadProjectForUserAsync(courseId, projectCode, GetUserId());

                if (IsStudent())
                {
                    //students can only see the summary of own team
                    if (project.Teams.All(team => team.Id != teamId))
                    {
                        return Forbid();
                    }
                }
                else if (!IsLector())
                {
                    return Forbid();
                }

                var dateUtc = date?.ToUniversalTime();

                project.Assignments = await _assignmentRepository.GetByTopicWithTests(project.Id);

                var assignmentResults = await _projectService.GetResultsForTeamAsync(project, teamId, dateUtc);

                var model = _topicConverter.ToTopicSummaryModel(project, assignmentResults);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Retrieves an overview of the assignment statistics for a project of a course (for the current period).
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="projectCode">Short code of the project.</param>
        /// <param name="date">Optional date parameter. If provided the status of the summary on that date will be returned.</param>
        [HttpGet("{projectCode}/statistics")]
        [ProducesResponseType(typeof(TopicStatisticsModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProjectStatistics(int courseId, string projectCode, [FromQuery] DateTime? date)
        {
            //TODO: write tests
            if (courseId < 1 || string.IsNullOrEmpty(projectCode))
            {
                return BadRequest();
            }

            var dateUtc = date?.ToUniversalTime();
            bool useCache = !(dateUtc.HasValue && DateTime.UtcNow.Subtract(dateUtc.Value).TotalSeconds > CacheTimeInSeconds);

            var cacheKey = $"GetProjectStatistics-{courseId}-{projectCode}";
            if (!useCache || !_memoryCache.TryGetValue(cacheKey, out TopicStatisticsModel model))
            {
                try
                {
                    var project = await _projectService.LoadProjectAsync(courseId, projectCode);
                    var projectStatistics = await _projectService.GetProjectStatisticsAsync(project, dateUtc);
                    model = _topicConverter.ToTopicStatisticsModel(project, projectStatistics, "Teams");

                    if (useCache)
                    {
                        _memoryCache.Set(cacheKey, model, DateTime.Now.AddSeconds(CacheTimeInSeconds));
                    }
                }
                catch (DataNotFoundException)
                {
                    return NotFound();
                }
            }

            return Ok(model);
        }
    }
}