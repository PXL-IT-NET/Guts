using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Converters;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Business.Services;

internal class AssignmentService(
    IAssignmentRepository assignmentRepository,
    ITopicService topicService,
    ITestRepository testRepository,
    ITestResultRepository testResultRepository,
    ITestRunRepository testRunRepository,
    ISolutionFileRepository solutionFileRepository,
    IAssignmentWithResultsConverter assignmentWithResultsConverter)
    : IAssignmentService
{
    public async Task<Assignment> GetAssignmentOfCurrentPeriodAsync(AssignmentDto assignmentDto)
    {
        ITopic topic = await topicService.GetTopicAsync(assignmentDto.CourseCode, assignmentDto.TopicCode);
        return await assignmentRepository.GetSingleAsync(topic.Id, assignmentDto.AssignmentCode);
    }

    public async Task<Assignment> GetOrCreateAssignmentAsync(int topicId, string assignmentCode)
    {
        Assignment assignment;
        try
        {
            assignment = await assignmentRepository.GetSingleAsync(topicId, assignmentCode);
        }
        catch (DataNotFoundException)
        {
            assignment = new Assignment
            {
                TopicId = topicId,
                Code = assignmentCode
            };
            assignment = await assignmentRepository.AddAsync(assignment);
        }

        return assignment;
    }

    public async Task LoadTestsForAssignmentAsync(Assignment assignment)
    {
        var assignmentTests = await testRepository.FindByAssignmentId(assignment.Id);
        assignment.Tests = assignmentTests;
    }

    public async Task LoadOrCreateTestsForAssignmentAsync(Assignment assignment, IReadOnlyList<string> testNames)
    {
        var assignmentTests = await testRepository.FindByAssignmentId(assignment.Id);

        IList<string> testNamesToCreate =
            testNames.Where(testName => assignmentTests.All(test => test.TestName != testName)).ToList();

        foreach (string testNameToCreate in testNamesToCreate)
        {
            var newTest = new Test
            {
                AssignmentId = assignment.Id,
                TestName = testNameToCreate
            };
            var savedTest = await testRepository.AddAsync(newTest);
            assignmentTests.Add(savedTest);
        }



        assignment.Tests = assignmentTests;
    }

    public async Task<AssignmentResultDto> GetResultsForUserAsync(int assignmentId, int userId, DateTime? dateUtc)
    {
        return new AssignmentResultDto
        {
            AssignmentId = assignmentId,
            TestResults = await testResultRepository.GetLastTestResultsOfUser(assignmentId, userId, dateUtc)
        };
    }

    public async Task<AssignmentTestRunInfoDto> GetUserTestRunInfoForAssignment(int assignmentId, int userId, DateTime? dateUtc)
    {
        var testRuns = await testRunRepository.GetUserTestRunsForAssignmentAsync(assignmentId, userId, dateUtc);
        return ConstructTestRunInfoFromTestRuns(testRuns);
    }

    public async Task<AssignmentResultDto> GetResultsForTeamAsync(int projectId, int assignmentId, int teamId, DateTime? dateUtc)
    {
        return new AssignmentResultDto
        {
            AssignmentId = assignmentId,
            TestResults = await testResultRepository.GetLastTestResultsOfTeam(projectId, assignmentId, teamId, dateUtc)
        };
    }

    public async Task<AssignmentTestRunInfoDto> GetTeamTestRunInfoForAssignment(int assignmentId, int teamId, DateTime? dateUtc)
    {
        var testRuns = await testRunRepository.GetTeamTestRunsForAssignmentAsync(assignmentId, teamId, dateUtc);
        return ConstructTestRunInfoFromTestRuns(testRuns);
    }

    public async Task<IReadOnlyList<SolutionDto>> GetAllSolutions(int assignmentId)
    {
        var assignmentSolutions = new List<SolutionDto>();

        var allSolutionFiles = await solutionFileRepository.GetAllLatestOfAssignmentAsync(assignmentId);

        var userFileGroups = allSolutionFiles.GroupBy(sf => sf.User);
        foreach (var userFileGroup in userFileGroups)
        {
            assignmentSolutions.Add(new SolutionDto
            {
                WriterId = userFileGroup.Key.Id,
                WriterName = $"{userFileGroup.Key.FirstName} {userFileGroup.Key.LastName}".Trim(),
                SolutionFiles = userFileGroup
            });
        }

        return assignmentSolutions;
    }

    public async Task<bool> ValidateTestCodeHashAsync(string testCodeHash, Assignment assignment, bool isLector)
    {
        if (assignment.TestCodeHashes.Count == 0 && string.IsNullOrEmpty(testCodeHash)) return true; //Ok to send an empty hash when no hashes are associated with the assignment

        if (assignment.TestCodeHashes.Any(ah => ah.Hash == testCodeHash)) return true;

        if (isLector && !string.IsNullOrEmpty(testCodeHash))
        {
            //add the hash to the collection of hashed associated with the assignment
            assignment.TestCodeHashes.Add(new TestCodeHash
            {
                AssignmentId = assignment.Id,
                Hash = testCodeHash
            });
            await assignmentRepository.UpdateAsync(assignment);
            return true;
        }
        return false;
    }

    public async Task<AssignmentStatisticsDto> GetAssignmentUserStatisticsAsync(int assignmentId, DateTime? dateUtc)
    {
        var testResults = await testResultRepository.GetLastTestResultsOfAllUsers(assignmentId, dateUtc);
        return assignmentWithResultsConverter.ToAssignmentStatisticsDto(assignmentId, testResults);
    }

    public async Task<AssignmentStatisticsDto> GetAssignmentTeamStatisticsAsync(int projectId, int assignmentId, DateTime? dateUtc)
    {
        var testResults = await testResultRepository.GetLastTestResultsOfAllTeams(projectId, assignmentId, dateUtc);
        return assignmentWithResultsConverter.ToAssignmentStatisticsDto(assignmentId, testResults);
    }

    private static AssignmentTestRunInfoDto ConstructTestRunInfoFromTestRuns(IList<TestRun> testRuns)
    {
        var testRunInfo = new AssignmentTestRunInfoDto();
        if (testRuns.Count > 0)
        {
            var firstTestRun = testRuns[0];
            var lastTestRun = testRuns[^1];
            testRunInfo.FirstRunDateTime = firstTestRun.CreateDateTime;
            testRunInfo.LastRunDateTime = lastTestRun.CreateDateTime;
            testRunInfo.NumberOfRuns = testRuns.Count;
        }
        return testRunInfo;
    }
}