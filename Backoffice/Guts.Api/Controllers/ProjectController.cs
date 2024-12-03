using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.Extensions.Caching.Memory;
using Guts.Api.Models.ProjectModels;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses/{courseId}/projects")]
    public class ProjectController : ControllerBase
    {
        public const int CacheTimeInSeconds = 60;

        private readonly IProjectService _projectService;
        //private readonly IProjectTeamRepository _projectTeamRepository;
        //private readonly IAssignmentRepository _assignmentRepository;
        private readonly IProjectConverter _projectConverter;
        private readonly ITopicConverter _topicConverter;
        //private readonly ITeamConverter _teamConverter;
        private readonly ISolutionFileService _solutionFileService;
        private readonly IMemoryCache _memoryCache;

        public ProjectController(IProjectService projectService,
            IProjectConverter projectConverter,
            ITopicConverter topicConverter,
            ISolutionFileService solutionFileService,
            IMemoryCache memoryCache)
        {
            _projectService = projectService;
            _projectConverter = projectConverter;
            _topicConverter = topicConverter;
            _solutionFileService = solutionFileService;
            _memoryCache = memoryCache;
        }

        [HttpGet("{projectCode}")]
        [ProducesResponseType(typeof(ProjectDetailModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProjectDetails(int courseId, string projectCode, [FromQuery] int? periodId = null)
        {
            if (courseId < 1 || string.IsNullOrEmpty(projectCode))
            {
                return BadRequest();
            }

            IProject project;
            if (IsLector())
            {
                project = await _projectService.LoadProjectAsync(courseId, projectCode, periodId);
            }
            else
            {
                project = await _projectService.LoadProjectForUserAsync(courseId, projectCode, GetUserId(), periodId);
            }

            var model = _projectConverter.ToProjectDetailModel(project);

            return Ok(model);
        }

        [HttpPost("")]
        [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
        [ProducesResponseType(typeof(ProjectDetailModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddProject(int courseId, [FromBody] TopicAddModel model)
        {
            if (courseId < 1)
            {
                return BadRequest();
            }

            IProject project = await _projectService.CreateProjectAsync(courseId, model.Code, model.Description);

            ProjectDetailModel outputModel = _projectConverter.ToProjectDetailModel(project);

            return CreatedAtAction(nameof(GetProjectDetails), new{ courseId = courseId, projectCode = outputModel.Code }, outputModel);
        }

        [HttpPut("{projectCode}")]
        [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateProject(int courseId, string projectCode, [FromBody]TopicUpdateModel model)
        {
            if (courseId < 1 || string.IsNullOrEmpty(projectCode))
            {
                return BadRequest();
            }

            await _projectService.UpdateProjectAsync(courseId, projectCode, model.Description);

            return Ok();
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

        [HttpGet("{projectCode}/getsourcecodezip")]
        [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
        public async Task<IActionResult> DownloadSourceCodesAsZip(int courseId, string projectCode, [FromQuery] DateTime? date)
        {
            IProject project = await _projectService.LoadProjectAsync(courseId, projectCode);
            IReadOnlyList<SolutionDto> solutions = await _projectService.GetAllSolutions(project, date);

            byte[] bytes = await _solutionFileService.CreateZipFromFiles(solutions);
            var result = new FileContentResult(bytes, "application/zip")
            {
                FileDownloadName = $"Project_{projectCode}_sources.zip"
            };
            return result;
        }
    }
}