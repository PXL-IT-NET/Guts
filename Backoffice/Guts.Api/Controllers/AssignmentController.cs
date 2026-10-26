using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/assignments")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AssignmentController(
    IAssignmentService assignmentService,
    IAssignmentRepository assignmentRepository,
    IAssignmentConverter assignmentConverter,
    IProjectTeamRepository projectTeamRepository,
    ITopicService topicService,
    ISolutionFileRepository solutionFileRepository,
    ISolutionFileService solutionFileService,
    IMapper mapper)
    : ControllerBase
{
    [HttpGet("{assignmentId}")]
    public async Task<IActionResult> GetAssignmentResults(int assignmentId, [FromQuery] DateTime? date)
    {
        return await GetAssignmentResultsForUser(assignmentId, GetUserId(), date);
    }

    [HttpGet("ofcourse/{courseId}")]
    public async Task<IActionResult> GetAssignmentsOfCourse(int courseId)
    {
        var topics = await topicService.GetTopicsByCourseWithAssignmentsAndTestsAsync(courseId);
        var models = topics.SelectMany(t => t.Assignments).Select(a => mapper.MapToTopicAssignmentModel(a));
        return Ok(models);
    }

    [HttpGet("{assignmentId}/foruser/{userId}")]
    public async Task<IActionResult> GetAssignmentResultsForUser(int assignmentId, int userId, [FromQuery] DateTime? date)
    {
        if (IsStudent())
        {
            //students can only see their own assignment results
            if (!IsOwnUserId(userId)) return Forbid();
        }
        else if (!IsLector())
        {
            return Forbid();
        }

        var assignment = await assignmentRepository.GetSingleWithTestsAndCourseAsync(assignmentId);

        var dateUtc = date?.ToUniversalTime();

        var testRunInfo = await assignmentService.GetUserTestRunInfoForAssignment(assignmentId, userId, dateUtc);

        var results = await assignmentService.GetResultsForUserAsync(assignmentId, userId, dateUtc);

        var solutionFiles =
            await solutionFileRepository.GetAllLatestOfAssignmentForUserAsync(assignmentId, userId, dateUtc);

        var model = assignmentConverter.ToAssignmentDetailModel(assignment, testRunInfo, results.TestResults, solutionFiles);

        return Ok(model);
    }

    [HttpGet("{assignmentId}/forteam/{teamId}")]
    public async Task<IActionResult> GetAssignmentResultsForTeam(int assignmentId, int teamId, [FromQuery] DateTime? date)
    {
        var team = await projectTeamRepository.LoadByIdAsync(teamId);
        if (IsStudent())
        {
            if (team.TeamUsers.All(tu => tu.UserId != GetUserId()))
            {
                return Forbid();
            }
        }
        else if (!IsLector())
        {
            return Forbid();
        }

        var assignment = await assignmentRepository.GetSingleWithTestsAndCourseAsync(assignmentId);
        var dateUtc = date?.ToUniversalTime();

        var testRunInfo = await assignmentService.GetTeamTestRunInfoForAssignment(assignmentId, teamId, dateUtc);

        var results = await assignmentService.GetResultsForTeamAsync(team.ProjectId, assignmentId, teamId, dateUtc);

        var solutionFiles =
            await solutionFileRepository.GetAllLatestOfAssignmentForTeamAsync(assignmentId, teamId, dateUtc);

        var model = assignmentConverter.ToAssignmentDetailModel(assignment, testRunInfo, results.TestResults, solutionFiles);

        return Ok(model);
    }

    [HttpGet("{assignmentId}/getsourcecodezip")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    public async Task<IActionResult> DownloadSourceCodesAsZip(int assignmentId)
    {
        IReadOnlyList<SolutionDto> solutions = await assignmentService.GetAllSolutions(assignmentId);
        byte[] bytes = await solutionFileService.CreateZipFromFiles(solutions);
        var result = new FileContentResult(bytes, "application/zip")
        {
            FileDownloadName = $"Assignment_{assignmentId}_sources.zip"
        };
        return result;
    }

    [HttpDelete("{assignmentId}")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Delete(int assignmentId)
    {
        Assignment assignmentToDelete = await assignmentRepository.GetByIdAsync(assignmentId);
        if (assignmentToDelete is null)
        {
            return NotFound();
        }

        await assignmentRepository.DeleteAsync(assignmentToDelete);
        return Ok();
    }
}