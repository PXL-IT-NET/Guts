using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.ProjectModels;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/project-assessments")]
public class ProjectAssessmentController(
    IProjectService projectService,
    IProjectAssessmentRepository projectAssessmentRepository,
    IMapper mapper)
    : ControllerBase
{
    [HttpGet("of-project/{projectId}")]
    [ProducesResponseType(typeof(IList<ProjectAssessmentModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectAssessments(int projectId)
    {
        IReadOnlyList<IProjectAssessment> assessments = await projectAssessmentRepository.FindByProjectIdAsync(projectId);
        var models = assessments.Select(a => mapper.MapToProjectAssessmentModel(a)).ToList();
        return Ok(models);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectAssessmentModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetProjectAssessment(int id)
    {
        IProjectAssessment assessment = await projectAssessmentRepository.GetByIdAsync(id);
        ProjectAssessmentModel model = mapper.MapToProjectAssessmentModel(assessment);
        return Ok(model);
    }

    [HttpPost("")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType(typeof(ProjectAssessmentModel), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> AddProjectAssessment([FromBody] CreateProjectAssessmentModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        IProjectAssessment assessment = await projectService.CreateProjectAssessmentAsync(model.ProjectId, model.Description,
            model.OpenOn.ToUniversalTime(), model.Deadline.ToUniversalTime());

        ProjectAssessmentModel outputModel = mapper.MapToProjectAssessmentModel(assessment);

        return CreatedAtAction(nameof(GetProjectAssessment), new { id = assessment.Id }, outputModel);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> UpdateProjectAssessment(int id, [FromBody] UpdateProjectAssessmentModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await projectService.UpdateProjectAssessmentAsync(id, model.Description,
            model.OpenOn.ToUniversalTime(), model.Deadline.ToUniversalTime());

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> DeleteProjectAssessment(int id)
    {
        if (id < 1)
        {
            return BadRequest();
        }

        IProjectAssessment assessmentToDelete = await projectAssessmentRepository.GetByIdAsync(id);
        await projectAssessmentRepository.DeleteAsync(assessmentToDelete);
        return Ok();
    }
}