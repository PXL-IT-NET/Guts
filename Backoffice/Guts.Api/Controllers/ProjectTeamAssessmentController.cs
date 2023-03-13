using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Guts.Business.Dtos;
using Guts.Business.Services.Assessment;
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

    [HttpGet("of-project-assessment/{projectAssessmentId}/of-team/{teamId}")]
    [ProducesResponseType(typeof(ProjectTeamAssessmentStatusDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectTeamAssessmentStatus(int projectAssessmentId, int teamId)
    {
        ProjectTeamAssessmentStatusDto status = await _projectTeamAssessmentService.GetStatusAsync(projectAssessmentId, teamId);
        return Ok(status);
    }
}