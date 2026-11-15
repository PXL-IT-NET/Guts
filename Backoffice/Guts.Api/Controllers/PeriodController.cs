using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.PeriodModels;
using Guts.Application;
using Guts.Application.Repositories;
using Guts.Application.Services.Period;
using Guts.Common;
using Guts.Domain.PeriodAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/periods")]
public class PeriodController(
    IPeriodRepository periodRepository,
    IPeriodService periodService,
    IMapper mapper)
    : ControllerBase
{
    /// <summary>
    /// Retrieves all the periods 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IList<PeriodOutputModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        IReadOnlyList<IPeriod> periods = await periodRepository.GetAllAsync();
        IList<PeriodOutputModel> models = periods.Select(p => mapper.MapToPeriodOutputModel(p)).ToList();
        return Ok(models);
    }

    /// <summary>
    /// Retrieves a period.
    /// </summary>
    /// <param name="id">Identifier of the period</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PeriodOutputModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> GetById(int id)
    {
        try
        {
            IPeriod period = await periodRepository.GetPeriodAsync(id);
            PeriodOutputModel model = mapper.MapToPeriodOutputModel(period);
            return Ok(model);
        }
        catch (DataNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Creates a new period
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
    [ProducesResponseType(typeof(PeriodOutputModel), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreatePeriod([FromBody] PeriodCreationModel model)
    {
        if (model == null)
        {
            return BadRequest(ErrorModel.FromString("Period data is required."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!model.From.HasValue || !model.Until.HasValue)
        {
            return BadRequest(ErrorModel.FromString("From and Until dates are required."));
        }

        try
        {
            IPeriod period = await periodService.CreatePeriodAsync(model.Description, model.From.Value, model.Until.Value);
            PeriodOutputModel outputModel = mapper.MapToPeriodOutputModel(period);
            return CreatedAtAction(nameof(GetById), new { id = outputModel.Id }, outputModel);
        }
        catch (ContractException ex)
        {
            return BadRequest(ErrorModel.FromException(ex));
        }
    }

    /// <summary>
    /// Updates an existing period.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdatePeriod(int id, [FromBody] PeriodCreationModel model)
    {
        if (model == null)
        {
            return BadRequest(ErrorModel.FromString("Period data is required."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!model.From.HasValue || !model.Until.HasValue)
        {
            return BadRequest(ErrorModel.FromString("From and Until dates are required."));
        }

        try
        {
            await periodService.UpdatePeriodAsync(id, model.Description, model.From.Value, model.Until.Value);
            return Ok();
        }
        catch (DataNotFoundException)
        {
            return NotFound();
        }
        catch (ContractException ex)
        {
            return BadRequest(ErrorModel.FromException(ex));
        }
    }

    /// <summary>
    /// Deletes a non-active period and its associated data.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeletePeriod(int id)
    {
        try
        {
            await periodService.DeletePeriodAsync(id);
            return NoContent();
        }
        catch (DataNotFoundException)
        {
            return NotFound();
        }
        catch (ContractException ex)
        {
            return BadRequest(ErrorModel.FromException(ex));
        }
    }
}