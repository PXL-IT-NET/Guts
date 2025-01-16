using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Guts.Api.Models;
using Guts.Api.Models.PeriodModels;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Business.Services.Exam;
using Guts.Business.Services.Period;
using Guts.Common;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/periods")]
public class PeriodController : ControllerBase
{
    private readonly IPeriodRepository _periodRepository;
    private readonly IPeriodService _periodService;
    private readonly IMapper _mapper;

    public PeriodController(IPeriodRepository periodRepository,
        IPeriodService periodService,
        IMapper mapper)
    {
        _periodRepository = periodRepository;
        _periodService = periodService;
        _mapper = mapper;
    }

    /// <summary>
    /// Retrieves all the periods 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IList<PeriodOutputModel>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        IReadOnlyList<IPeriod> periods = await _periodRepository.GetAllAsync();
        IList<PeriodOutputModel> models = periods.Select(p => _mapper.Map<PeriodOutputModel>(p)).ToList();
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
            IPeriod period = await _periodRepository.GetPeriodAsync(id);
            PeriodOutputModel model = _mapper.Map<PeriodOutputModel>(period);
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
    [ProducesResponseType(typeof(PeriodOutputModel), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreatePeriod([FromBody] PeriodCreationModel model)
    {
        try
        {
            IPeriod period = await _periodService.CreatePeriodAsync(model.Description, model.From, model.Until);
            PeriodOutputModel outputModel = _mapper.Map<PeriodOutputModel>(period);
            return CreatedAtAction(nameof(GetById), new { id = outputModel.Id }, outputModel);
        }
        catch (ContractException ex)
        {
            return BadRequest(ErrorModel.FromException(ex));
        }
    }
}