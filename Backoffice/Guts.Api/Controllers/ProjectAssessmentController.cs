using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Guts.Api.Models.ProjectModels;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/project-assessments")]
public class ProjectAssessmentController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectAssessmentRepository _projectAssessmentRepository;
    private readonly IMapper _mapper;

    public ProjectAssessmentController(IProjectService projectService,
        IProjectAssessmentRepository projectAssessmentRepository,
        IMapper mapper)
    {
        _projectService = projectService;
        _projectAssessmentRepository = projectAssessmentRepository;
        _mapper = mapper;
    }

    [HttpGet("ofproject/{projectId}")]
    [ProducesResponseType(typeof(IList<ProjectAssessmentModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectAssessments(int projectId)
    {
        IReadOnlyList<IProjectAssessment> assessments = await _projectAssessmentRepository.FindByProjectIdAsync(projectId);
        var models = assessments.Select(a => _mapper.Map<ProjectAssessmentModel>(a)).ToList();
        return Ok(models);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectAssessmentModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectAssessment(int id)
    {
        IProjectAssessment assessment = await _projectAssessmentRepository.GetByIdAsync(id);
        ProjectAssessmentModel model = _mapper.Map<ProjectAssessmentModel>(assessment);
        return Ok(model);
    }

    [HttpPost("")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType(typeof(ProjectAssessmentModel), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> AddProjectAssessment([FromBody] CreateProjectAssessmentModel model)
    {
        IProjectAssessment assessment = await _projectService.CreateProjectAssessmentAsync(model.ProjectId, model.Description,
            model.OpenOn.ToUniversalTime(), model.Deadline.ToUniversalTime());

        ProjectAssessmentModel outputModel = _mapper.Map<ProjectAssessmentModel>(assessment);

        return CreatedAtAction(nameof(GetProjectAssessment), new { id = outputModel.Id }, outputModel);
    }
}