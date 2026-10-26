using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Services;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Guts.Api.Tests")]

namespace Guts.Api.Controllers;

/// <summary>
/// Manage test runs.
/// </summary>
[Produces("application/json")]
[Route("api/testruns")]
public class TestRunController(
    ITestRunConverter testRunConverter,
    ITestRunService testRunService,
    IAssignmentService assignmentService,
    IChapterService chapterService,
    IProjectService projectService,
    ILogger<TestRunController> logger)
    : ControllerBase
{
    internal const string InvalidTestCodeHashErrorKey = "InvalidTestCodeHash";

    /// <summary>
    /// Retrieves a testrun.
    /// </summary>
    /// <param name="id">Identifier of the testrun in the database</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SavedTestRunModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<ActionResult> GetTestRun(int id)
    {
        var storedTestRun = await testRunService.GetTestRunAsync(id);
        var model = testRunConverter.ToTestRunModel(storedTestRun);
        return Ok(model);
    }

    /// <summary>
    /// Saves a testrun for an exercise. The testrun may contain results for one, multiple or all tests.
    /// If the exercise (or its chapter) does not exists yet (for the current period) a new exercise / chapter is created for the current period. 
    /// </summary>
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

        LogTestRunReceived(model);

        Assignment assignment;
        if (IsLector())
        {
            var chapter = await chapterService.GetOrCreateChapterAsync(model.Assignment.CourseCode, model.Assignment.TopicCode);
            assignment = await assignmentService.GetOrCreateAssignmentAsync(chapter.Id, model.Assignment.AssignmentCode);
        }
        else
        {
            try
            {
                assignment = await assignmentService.GetAssignmentOfCurrentPeriodAsync(model.Assignment);
            }
            catch (DataNotFoundException)
            {
                ModelState.AddModelError("InvalidAssignment",
                    "The exercise is not created yet. A lector first must create the exercise.");
                return BadRequest(ModelState);
            }
        }

        if (!await assignmentService.ValidateTestCodeHashAsync(model.TestCodeHash, assignment, IsLector()))
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

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Received testrun for project (assignmentCode: {AssignmentCode}, projectCode: {ProjectCode}) from user {UserId}",
                model.Assignment.AssignmentCode, model.Assignment.TopicCode, GetUserId());
        }

        Assignment component;
        if (IsLector())
        {
            var project = await projectService.GetOrCreateProjectAsync(model.Assignment.CourseCode, model.Assignment.TopicCode, null);
            component = await assignmentService.GetOrCreateAssignmentAsync(project.Id, model.Assignment.AssignmentCode);
        }
        else
        {
            try
            {
                component = await assignmentService.GetAssignmentOfCurrentPeriodAsync(model.Assignment);
            }
            catch (DataNotFoundException)
            {
                ModelState.AddModelError("InvalidAssignment", 
                    "The project component is not created yet. A lector first must first create the component.");
                return BadRequest(ModelState);
            }
        }

        if (! await assignmentService.ValidateTestCodeHashAsync(model.TestCodeHash, component, IsLector()))
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
            var testNames = model.Results.Select(testResult => testResult.TestName).ToList();
            await assignmentService.LoadOrCreateTestsForAssignmentAsync(assignment, testNames);
        }
        else
        {
            await assignmentService.LoadTestsForAssignmentAsync(assignment);
        }

        TestRun testRun = testRunConverter.From(model.Results, GetUserId(), assignment);

        IList<SolutionFile> solutionFiles = model.SolutionFiles?.Select(sourceFileModel =>
            SolutionFile.CreateNew(assignment.Id, GetUserId(), sourceFileModel.FilePath,
                sourceFileModel.Content.TryConvertFromBase64())).ToList();

        TestRun savedTestRun = await testRunService.RegisterRunAsync(testRun, solutionFiles);

        LogTestRunRegistered(testRun, solutionFiles);

        var savedModel = testRunConverter.ToTestRunModel(savedTestRun);
        return savedModel;
    }

    private void LogTestRunReceived(CreateAssignmentTestRunModel model)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;

        string sourcePaths = string.Empty;
        if (model.SolutionFiles.Any())
        {
            sourcePaths = string.Join(";", model.SolutionFiles.Select(sf => sf.FilePath));
        }

        logger.LogInformation(
            "Received testrun for exercise (assignmentCode: {AssignmentCode}, chapterCode: {ChapterCode}, Source files: {SourcePaths}) from user {UserId}",
            model.Assignment.AssignmentCode, model.Assignment.TopicCode, sourcePaths, GetUserId());
    }
        
    private void LogTestRunRegistered(TestRun testRun, IList<SolutionFile> solutionFiles)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;

        if (testRun == null)
        {
            logger.LogInformation("Test run is null");
        }
        else
        {
            string sourcePaths = string.Empty;
            if (solutionFiles != null && solutionFiles.Count > 0)
            {
                sourcePaths = string.Join(";", solutionFiles.Select(sf => sf.FilePath));
            }
                
            StringBuilder logMessageBuilder = new StringBuilder();
            logMessageBuilder.AppendLine($"Saving test run for user {testRun.UserId}:");
            logMessageBuilder.AppendLine($"AssignmentId: {testRun.AssignmentId}");
            logMessageBuilder.AppendLine($"#Passing tests: {testRun.TestResults.Count(r => r.Passed)}");
            logMessageBuilder.AppendLine($"#Failing tests: {testRun.TestResults.Count(r => !r.Passed)}");
            logMessageBuilder.AppendLine($"Sources: {sourcePaths}");
            logMessageBuilder.AppendLine("-------------------------------------");
            logger.LogInformation("{LogMessage}", logMessageBuilder.ToString());
        }
    }
}