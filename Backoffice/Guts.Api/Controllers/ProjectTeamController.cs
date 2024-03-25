using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Api.Models.ProjectModels;
using Guts.Business;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/courses/{courseId}/projects/{projectCode}/teams")]
public class ProjectTeamController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectTeamRepository _projectTeamRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ITopicConverter _topicConverter;
    private readonly ITeamConverter _teamConverter;

    public ProjectTeamController(IProjectService projectService,
        IProjectTeamRepository projectTeamRepository,
        IAssignmentRepository assignmentRepository,
        ITopicConverter topicConverter,
        ITeamConverter teamConverter)
    {
        _projectService = projectService;
        _projectTeamRepository = projectTeamRepository;
        _assignmentRepository = assignmentRepository;
        _topicConverter = topicConverter;
        _teamConverter = teamConverter;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IList<TeamDetailsModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectTeams(int courseId, string projectCode)
    {
        var teams = await _projectService.LoadTeamsOfProjectAsync(courseId, projectCode);

        var models = teams.Select(team => _teamConverter.ToTeamDetailsModel(team)).ToList();

        return Ok(models);
    }

    [HttpGet("{teamId}")]
    [ProducesResponseType(typeof(TeamDetailsModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectTeam(int courseId, string projectCode, int teamId)
    {
        IProjectTeam team = await _projectTeamRepository.LoadByIdAsync(teamId);

        Contracts.Require(team.Project.CourseId == courseId, $"Team with id '{teamId}' is not linked to course with id '{courseId}'");
        Contracts.Require(team.Project.Code == projectCode, $"Team with id '{teamId}' is not linked to project with code '{projectCode}'");

        TeamDetailsModel model = _teamConverter.ToTeamDetailsModel(team);

        return Ok(model);
    }

    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    public async Task<IActionResult> AddProjectTeam(int courseId, string projectCode, [FromBody] TeamEditModel model)
    {
        IProjectTeam createdTeam = await _projectService.AddProjectTeamAsync(courseId, projectCode, model.Name);
        TeamDetailsModel createdTeamModel = _teamConverter.ToTeamDetailsModel(createdTeam);

        return CreatedAtAction(nameof(GetProjectTeam),
            new { courseId = courseId, projectCode = projectCode, teamId = createdTeamModel.Id }, createdTeamModel);
    }

    [HttpPut("{teamId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    public async Task<IActionResult> UpdateProjectTeam(int courseId, string projectCode, int teamId, [FromBody] TeamEditModel model)
    {
        await _projectService.UpdateProjectTeamAsync(courseId, projectCode, teamId, model.Name);

        return Ok();
    }

    [HttpPost("generate")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    public async Task<IActionResult> GenerateProjectTeams(int courseId, string projectCode, [FromBody] TeamGenerationModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _projectService.GenerateTeamsForProject(courseId, projectCode, model.TeamBaseName, model.TeamNumberFrom, model.TeamNumberTo);

        return Ok();
    }

    [HttpPost("{teamId}/join")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    public async Task<IActionResult> JoinProjectTeam(int courseId, string projectCode, int teamId)
    {
        await _projectService.AddUserToProjectTeamAsync(courseId, projectCode, teamId, GetUserId());
        return Ok();
    }

    [HttpPost("{teamId}/leave")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    public async Task<IActionResult> LeaveProjectTeam(int courseId, string projectCode, int teamId)
    {
        await _projectService.RemoveUserFromProjectTeamAsync(courseId, projectCode, teamId, GetUserId());
        return Ok();
    }

    [HttpPost("{teamId}/remove")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    public async Task<IActionResult> RemoveFromProjectTeam(int courseId, string projectCode, int teamId, [FromBody] int userId)
    {
        await _projectService.RemoveUserFromProjectTeamAsync(courseId, projectCode, teamId, userId);
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
    [HttpGet("{teamId}/summary")]
    [ProducesResponseType(typeof(TopicSummaryModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectSummary(int courseId, string projectCode, int teamId, [FromQuery] DateTime? date)
    {
        IProject project = await _projectService.LoadProjectForUserAsync(courseId, projectCode, GetUserId());

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

        IReadOnlyList<AssignmentResultDto> assignmentResults = await _projectService.GetResultsForTeamAsync(project, teamId, dateUtc);

        var model = _topicConverter.ToTopicSummaryModel(project, assignmentResults);
        return Ok(model);
    }

    [HttpDelete("{teamId}")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> DeleteProjectTeam(int courseId, string projectCode, int teamId)
    {
        await _projectService.DeleteProjectTeamAsync(courseId, projectCode, teamId);
        return Ok();
    }
}