using System.Net;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TestAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/tests")]
public class TestController : ControllerBase
{
    private readonly ITestRepository _testRepository;


    public TestController(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Delete(int id)
    {
        Test testToDelete = await _testRepository.GetByIdAsync(id);
        if (testToDelete is null)
        {
            return NotFound();
        }

        await _testRepository.DeleteAsync(testToDelete);
        return Ok();
    }
}