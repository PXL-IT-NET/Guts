using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Guts.Api.Models.ProjectModels;
using Guts.Business.Dtos;
using Guts.Business.Services.Assessment;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/project-team-assessments")]
public class ProjectTeamAssessmentController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IProjectTeamAssessmentService _projectTeamAssessmentService;

    public ProjectTeamAssessmentController(
        IProjectTeamAssessmentService projectTeamAssessmentService,
        IMapper mapper)
    {
        _projectTeamAssessmentService = projectTeamAssessmentService;
        _mapper = mapper;
    }

    [HttpGet("of-project-assessment/{projectAssessmentId}/of-team/{teamId}/status")]
    [ProducesResponseType(typeof(ProjectTeamAssessmentStatusDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectTeamAssessmentStatus(int projectAssessmentId, int teamId)
    {
        ProjectTeamAssessmentStatusDto status = await _projectTeamAssessmentService.GetStatusAsync(projectAssessmentId, teamId);
        return Ok(status);
    }

    [HttpGet("of-project-assessment/{projectAssessmentId}/of-team/{teamId}/peer-assessments")]
    [ProducesResponseType(typeof(IList<PeerAssessmentModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetPeerAssessmentsOfUser(int projectAssessmentId, int teamId)
    {
        int userId = GetUserId();
        IReadOnlyList<IPeerAssessment> userAssessments = await _projectTeamAssessmentService.GetPeerAssessmentsOfUserAsync(projectAssessmentId, teamId, userId);

        IList<PeerAssessmentModel> models = userAssessments.Select(a => _mapper.Map<PeerAssessmentModel>(a)).ToList();
        return Ok(models);
    }

    [HttpPost("of-project-assessment/{projectAssessmentId}/of-team/{teamId}/peer-assessments")]
    [ProducesResponseType(typeof(IList<PeerAssessmentModel>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> SavePeerAssessment(int projectAssessmentId, int teamId, [FromBody] PeerAssessmentModel[] models)
    {
        int userId = GetUserId();
        
        List<PeerAssessmentDto> dtos = models.Select(model => _mapper.Map<PeerAssessmentDto>(model)).ToList();

        await _projectTeamAssessmentService.SavePeerAssessmentsOfUserAsync(projectAssessmentId, teamId, userId, dtos);

        return Ok();
    }

    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [HttpGet("of-project-assessment/{projectAssessmentId}/of-team/{teamId}/detailed-results")]
    [ProducesResponseType(typeof(IList<AssessmentResultModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectTeamAssessmentResults(int projectAssessmentId, int teamId)
    {
        IReadOnlyList<IAssessmentResult> results = await _projectTeamAssessmentService.GetResultsForLectorAsync(projectAssessmentId, teamId);
        IList<AssessmentResultModel> models = results.Select(result => _mapper.Map<AssessmentResultModel>(result)).ToList();
        return Ok(models);
    }

    [HttpGet("of-project-assessment/{projectAssessmentId}/of-team/{teamId}/my-result")]
    [ProducesResponseType(typeof(IList<PeerAssessmentModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectTeamAssessmentResultForUser(int projectAssessmentId, int teamId)
    {
        int userId = GetUserId();
        
        IAssessmentResult result = await _projectTeamAssessmentService.GetResultForStudent(projectAssessmentId, teamId, userId);

        AssessmentResultModel model = _mapper.Map<AssessmentResultModel>(result);
        return Ok(model);
    }
}