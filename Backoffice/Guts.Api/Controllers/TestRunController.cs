using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Services;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Guts.Api.Tests")]

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
        private readonly IAssignmentService _assignmentService;
        private readonly IChapterService _chapterService;
        private readonly IProjectService _projectService;
        private readonly ILogger<TestRunController> _logger;

        internal const string InvalidTestCodeHashErrorKey = "InvalidTestCodeHash";

        public TestRunController(ITestRunConverter testRunConverter, 
            ITestRunService testRunService, 
            IAssignmentService assignmentService,
            IChapterService chapterService, 
            IProjectService projectService,
            ILogger<TestRunController> logger)
        {
            _testRunConverter = testRunConverter;
            _testRunService = testRunService;
            _assignmentService = assignmentService;
            _chapterService = chapterService;
            _projectService = projectService;
            _logger = logger;
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
        [HttpPost] //keep for backwards compatibility
        [HttpPost("forexercise")]
        [ProducesResponseType(typeof(SavedTestRunModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PostExerciseTestRun([FromBody] CreateAssignmentTestRunModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation(
                $"Received testrun for exercise (assignmentCode: {model.Assignment.AssignmentCode}, chapterCode: {model.Assignment.TopicCode}) from user {GetUserId()}");

            Assignment assignment;
            if (IsLector())
            {
                var chapter = await _chapterService.GetOrCreateChapterAsync(model.Assignment.CourseCode, model.Assignment.TopicCode);
                assignment = await _assignmentService.GetOrCreateAssignmentAsync(chapter.Id, model.Assignment.AssignmentCode);
            }
            else
            {
                try
                {
                    assignment = await _assignmentService.GetAssignmentAsync(model.Assignment);
                }
                catch (DataNotFoundException)
                {
                    ModelState.AddModelError("InvalidAssignment",
                        "The exercise is not created yet. A lector first must create the exercise.");
                    return BadRequest(ModelState);
                }
            }

            if (!await _assignmentService.ValidateTestCodeHashAsync(model.TestCodeHash, assignment, IsLector()))
            {
                ModelState.AddModelError(InvalidTestCodeHashErrorKey,
                    "The hash of the test code does not match any of the hashes associated with the assignment.");
                return BadRequest(ModelState);
            }

            var savedModel = await SaveTestRunForAssignment(model, assignment);
            
            return CreatedAtAction(nameof(GetTestRun), new {id = savedModel?.Id}, savedModel);
        }

        /// <summary>
        /// Saves a testrun for a component of a project. The testrun may contain results for one, multiple or all tests of that component.
        /// If the component (or its project) does noet exists yet (for the current period) a new component / project is created for the current period.
        /// </summary>
        [HttpPost("forproject")]
        [ProducesResponseType(typeof(SavedTestRunModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PostProjectTestRun([FromBody] CreateAssignmentTestRunModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation(
                $"Received testrun for project (assignmentCode: {model.Assignment.AssignmentCode}, projectCode: {model.Assignment.TopicCode}) from user {GetUserId()}");

            Assignment component;
            if (IsLector())
            {
                var project = await _projectService.GetOrCreateProjectAsync(model.Assignment.CourseCode, model.Assignment.TopicCode);
                component = await _assignmentService.GetOrCreateAssignmentAsync(project.Id, model.Assignment.AssignmentCode);
            }
            else
            {
                try
                {
                    component = await _assignmentService.GetAssignmentAsync(model.Assignment);
                }
                catch (DataNotFoundException)
                {
                    ModelState.AddModelError("InvalidAssignment", 
                        "The project component is not created yet. A lector first must first create the component.");
                    return BadRequest(ModelState);
                }
            }

            if (! await _assignmentService.ValidateTestCodeHashAsync(model.TestCodeHash, component, IsLector()))
            {
                ModelState.AddModelError("InvalidTestCodeHash",
                    "The hash of the test code does not match any of the hashes associated with the assignment.");
                return BadRequest(ModelState);
            }

            var savedModel = await SaveTestRunForAssignment(model, component);
            return CreatedAtAction(nameof(GetTestRun), new { id = savedModel.Id }, savedModel);
        }

        private async Task<SavedTestRunModel> SaveTestRunForAssignment(CreateAssignmentTestRunModel model, Assignment assignment)
        {
            if (IsLector())
            {
                var testNames = model.Results.Select(testResult => testResult.TestName);
                await _assignmentService.LoadOrCreateTestsForAssignmentAsync(assignment, testNames);
            }
            else
            {
                await _assignmentService.LoadTestsForAssignmentAsync(assignment);
            }

            TestRun testRun = _testRunConverter.From(model.Results, GetUserId(), assignment);
            LogTestRun(testRun);

            var solutionFiles = model.SolutionFiles?.Select(sourceFileModel =>
                SolutionFile.CreateNew(assignment.Id, GetUserId(), sourceFileModel.FilePath, sourceFileModel.Content));

            var savedTestRun = await _testRunService.RegisterRunAsync(testRun, solutionFiles);

            var savedModel = _testRunConverter.ToTestRunModel(savedTestRun);
            return savedModel;
        }

        private void LogTestRun(TestRun testRun)
        {
            if (testRun == null)
            {
                _logger.LogInformation("Test run is null");
            }
            else
            {
                StringBuilder logMessageBuilder = new StringBuilder();
                logMessageBuilder.AppendLine($"Saving test run for user {testRun.UserId}:");
                logMessageBuilder.AppendLine($"AssignmentId: {testRun.AssignmentId}");
                logMessageBuilder.AppendLine($"#Passing tests: {testRun.TestResults.Count(r => r.Passed)}");
                logMessageBuilder.AppendLine($"#Failing tests: {testRun.TestResults.Count(r => !r.Passed)}");
                logMessageBuilder.AppendLine("-------------------------------------");
                _logger.LogInformation(logMessageBuilder.ToString());
            }
        }
    }
}
