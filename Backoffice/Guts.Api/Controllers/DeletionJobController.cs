using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Application;
using Guts.Application.Dtos;
using Guts.Application.Services.Deletion;
using Guts.Domain.DeletionJobAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/deletion-jobs")]
[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
public class DeletionJobController(
    IDeletionJobService deletionJobService,
    IMapper mapper)
    : ControllerBase
{
    /// <summary>
    /// Retrieves active (Pending or InProgress) background deletion jobs.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IList<DeletionJobStatusModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetActiveJobs([FromQuery] string targetType = null)
    {
        DeletionTargetType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            if (!Enum.TryParse<DeletionTargetType>(targetType, ignoreCase: true, out var value) ||
                !Enum.IsDefined(value))
            {
                return BadRequest(ErrorModel.FromString($"Invalid target type '{targetType}'."));
            }

            parsedType = value;
        }

        IReadOnlyList<DeletionJobDto> activeJobs = await deletionJobService.GetActiveJobsAsync(parsedType);
        IList<DeletionJobStatusModel> models = activeJobs.Select(mapper.MapToDeletionJobStatusModel).ToList();
        return Ok(models);
    }

    /// <summary>
    /// Retrieves the status of a specific deletion job.
    /// </summary>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(DeletionJobStatusModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> GetJobStatus(Guid jobId)
    {
        if (jobId == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            DeletionJobDto job = await deletionJobService.GetJobStatusAsync(jobId);
            return Ok(mapper.MapToDeletionJobStatusModel(job));
        }
        catch (DataNotFoundException)
        {
            return NotFound();
        }
    }
}
