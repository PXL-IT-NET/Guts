using System.Linq;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/testruns")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TestRunController : ControllerBase
    {
        private readonly ITestRunConverter _testRunConverter;
        private readonly ITestRunService _testRunService;
        private readonly IExerciseService _exerciseService;

        public TestRunController(ITestRunConverter testRunConverter, 
            ITestRunService testRunService, 
            IExerciseService exerciseService)
        {
            _testRunConverter = testRunConverter;
            _testRunService = testRunService;
            _exerciseService = exerciseService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetTestRun(int id)
        {
            var storedTestRun = await _testRunService.GetTestRunAsync(id);
            var model = _testRunConverter.ToTestRunModel(storedTestRun);
            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> PostTestRun([FromBody] CreateTestRunModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var exercise = await _exerciseService.GetOrCreateExerciseAsync(model.Exercise);
            var testNames = model.Results.Select(testResult => testResult.TestName);
            await _exerciseService.LoadOrCreateTestsForExerciseAsync(exercise, testNames);

            var testRun = _testRunConverter.From(model.Results, UserId, exercise);
            var savedTestRun = await _testRunService.RegisterRunAsync(testRun);

            var savedModel = _testRunConverter.ToTestRunModel(savedTestRun);

            return CreatedAtAction(nameof(GetTestRun), new {id = savedModel.Id}, savedModel);
        }
    }
}
