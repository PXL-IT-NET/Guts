using System.Net;
using System.Threading.Tasks;
using Guts.Api.Filters;
using Guts.Api.Models;
using Guts.Api.Models.ExamModels;
using Guts.Application.Services.Exam;
using Guts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/exam-sessions")]
public class ExamSessionController(
    IExamService examService,
    IMapper mapper)
    : ControllerBase
{
    [HttpPost("{examId}/enter")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExamEntryResponseModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Enter(int examId, [FromBody] EnterExamModel model)
    {
        if (examId < 1) return BadRequest();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var entry = await examService.EnterExamAsync(examId, model.AccessCode);
            return Ok(mapper.MapToExamEntryResponseModel(entry));
        }
        catch (ContractException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ErrorModel.FromException(ex));
        }
    }

    [HttpGet("{examId}/workspace")]
    [AllowExamGuest]
    [ProducesResponseType(typeof(ExamWorkspaceModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetWorkspace(int examId)
    {
        if (examId < 1) return BadRequest();

        var workspace = await examService.GetExamWorkspaceAsync(examId, allowUnopenedPreview: false);
        return Ok(mapper.MapToExamWorkspaceModel(workspace));
    }
}
