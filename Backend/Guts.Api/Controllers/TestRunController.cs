using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    /// <summary>
    /// Manage test runs.
    /// </summary>
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

        /// <summary>
        /// Retrieves a testrun.
        /// </summary>
        /// <param name="id">Identifier of the testrun in the database</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SavedTestRunModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<ActionResult> GetTestRun(int id)
        {
            var storedTestRun = await _testRunService.GetTestRunAsync(id);
            var model = _testRunConverter.ToTestRunModel(storedTestRun);
            return Ok(model);
        }

        /// <summary>
        /// Saves a testrun for an exercise. The testrun may contain results for one, multiple or all tests.
        /// If the exercise (or its chapter) does not exists yet (for the current period) a new exercise / chapter is created for the current period. 
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SavedTestRunModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PostTestRun([FromBody] CreateTestRunModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var exercise = await _exerciseService.GetOrCreateExerciseAsync(model.Exercise);
            var testNames = model.Results.Select(testResult => testResult.TestName);
            await _exerciseService.LoadOrCreateTestsForExerciseAsync(exercise, testNames);

            var testRun = _testRunConverter.From(model.Results, model.SourceCode, GetUserId(), exercise);
            var savedTestRun = await _testRunService.RegisterRunAsync(testRun);

            var savedModel = _testRunConverter.ToTestRunModel(savedTestRun);

            return CreatedAtAction(nameof(GetTestRun), new {id = savedModel.Id}, savedModel);
        }
    }
}
