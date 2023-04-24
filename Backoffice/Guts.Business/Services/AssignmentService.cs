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

namespace Guts.Business.Services
{
    internal class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ITopicService _topicService;
        private readonly ITestRepository _testRepository;
        private readonly ITestResultRepository _testResultRepository;
        private readonly ITestRunRepository _testRunRepository;
        private readonly ISolutionFileRepository _solutionFileRepository;
        private readonly IAssignmentWithResultsConverter _assignmentWithResultsConverter;

        public AssignmentService(IAssignmentRepository assignmentRepository,
            ITopicService topicService,
            ITestRepository testRepository,
            ITestResultRepository testResultRepository,
            ITestRunRepository testRunRepository,
            ISolutionFileRepository solutionFileRepository,
            IAssignmentWithResultsConverter assignmentWithResultsConverter)
        {
            _assignmentRepository = assignmentRepository;
            _topicService = topicService;
            _testRepository = testRepository;
            _testResultRepository = testResultRepository;
            _testRunRepository = testRunRepository;
            _solutionFileRepository = solutionFileRepository;
            _assignmentWithResultsConverter = assignmentWithResultsConverter;
        }

        public async Task<Assignment> GetAssignmentAsync(AssignmentDto assignmentDto)
        {
            var topic = await _topicService.GetTopicAsync(assignmentDto.CourseCode, assignmentDto.TopicCode);
            return await _assignmentRepository.GetSingleAsync(topic.Id, assignmentDto.AssignmentCode);
        }

        public async Task<Assignment> GetOrCreateAssignmentAsync(int topicId, string assignmentCode)
        {
            Assignment assignment;
            try
            {
                assignment = await _assignmentRepository.GetSingleAsync(topicId, assignmentCode);
            }
            catch (DataNotFoundException)
            {
                assignment = new Assignment
                {
                    TopicId = topicId,
                    Code = assignmentCode
                };
                assignment = await _assignmentRepository.AddAsync(assignment);
            }

            return assignment;
        }

        public async Task LoadTestsForAssignmentAsync(Assignment assignment)
        {
            var assignmentTests = await _testRepository.FindByAssignmentId(assignment.Id);
            assignment.Tests = assignmentTests;
        }

        public async Task LoadOrCreateTestsForAssignmentAsync(Assignment assignment, IReadOnlyList<string> testNames)
        {
            var assignmentTests = await _testRepository.FindByAssignmentId(assignment.Id);

            IList<string> testNamesToCreate =
                testNames.Where(testName => assignmentTests.All(test => test.TestName != testName)).ToList();

            foreach (string testNameToCreate in testNamesToCreate)
            {
                var newTest = new Test
                {
                    AssignmentId = assignment.Id,
                    TestName = testNameToCreate
                };
                var savedTest = await _testRepository.AddAsync(newTest);
                assignmentTests.Add(savedTest);
            }

            //IList<Test> testsToDelete =
            //    assignmentTests.Where(test => testNames.All(testName => testName != test.TestName)).ToList();

            //foreach (Test testToDelete in testsToDelete)
            //{
            //    await _testRepository.DeleteAsync(testToDelete);
            //    assignmentTests.Remove(testToDelete);
            //}

            assignment.Tests = assignmentTests;
        }

        public async Task<AssignmentResultDto> GetResultsForUserAsync(int assignmentId, int userId, DateTime? dateUtc)
        {
            return new AssignmentResultDto
            {
                AssignmentId = assignmentId,
                TestResults = await _testResultRepository.GetLastTestResultsOfUser(assignmentId, userId, dateUtc)
            };
        }

        public async Task<AssignmentTestRunInfoDto> GetUserTestRunInfoForAssignment(int assignmentId, int userId, DateTime? dateUtc)
        {
            var testRuns = await _testRunRepository.GetUserTestRunsForAssignmentAsync(assignmentId, userId, dateUtc);
            return ConstructTestRunInfoFromTestRuns(testRuns);
        }

        public async Task<AssignmentResultDto> GetResultsForTeamAsync(int projectId, int assignmentId, int teamId, DateTime? dateUtc)
        {
            return new AssignmentResultDto
            {
                AssignmentId = assignmentId,
                TestResults = await _testResultRepository.GetLastTestResultsOfTeam(projectId, assignmentId, teamId, dateUtc)
            };
        }

        public async Task<AssignmentTestRunInfoDto> GetTeamTestRunInfoForAssignment(int assignmentId, int teamId, DateTime? dateUtc)
        {
            var testRuns = await _testRunRepository.GetTeamTestRunsForAssignmentAsync(assignmentId, teamId, dateUtc);
            return ConstructTestRunInfoFromTestRuns(testRuns);
        }

        public async Task<IReadOnlyList<SolutionDto>> GetAllSolutions(int assignmentId)
        {
            var assignmentSolutions = new List<SolutionDto>();

            var allSolutionFiles = await _solutionFileRepository.GetAllLatestOfAssignmentAsync(assignmentId);

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
            if (!assignment.TestCodeHashes.Any() && string.IsNullOrEmpty(testCodeHash)) return true; //Ok to send an empty hash when no hashes are associated with the assignment

            if (assignment.TestCodeHashes.Any(ah => ah.Hash == testCodeHash)) return true;

            if (isLector && !string.IsNullOrEmpty(testCodeHash))
            {
                //add the hash to the collection of hashed associated with the assignment
                assignment.TestCodeHashes.Add(new TestCodeHash
                {
                    AssignmentId = assignment.Id,
                    Hash = testCodeHash
                });
                await _assignmentRepository.UpdateAsync(assignment);
                return true;
            }
            return false;
        }

        public async Task<AssignmentStatisticsDto> GetAssignmentUserStatisticsAsync(int assignmentId, DateTime? dateUtc)
        {
            var testResults = await _testResultRepository.GetLastTestResultsOfAllUsers(assignmentId, dateUtc);
            return _assignmentWithResultsConverter.ToAssignmentStatisticsDto(assignmentId, testResults);
        }

        public async Task<AssignmentStatisticsDto> GetAssignmentTeamStatisticsAsync(int projectId, int assignmentId, DateTime? dateUtc)
        {
            var testResults = await _testResultRepository.GetLastTestResultsOfAllTeams(projectId, assignmentId, dateUtc);
            return _assignmentWithResultsConverter.ToAssignmentStatisticsDto(assignmentId, testResults);
        }

        private AssignmentTestRunInfoDto ConstructTestRunInfoFromTestRuns(IList<TestRun> testRuns)
        {
            var testRunInfo = new AssignmentTestRunInfoDto();
            if (testRuns.Any())
            {
                var firstTestRun = testRuns.First();
                var lastTestRun = testRuns.Last();
                testRunInfo.FirstRunDateTime = firstTestRun.CreateDateTime;
                testRunInfo.LastRunDateTime = lastTestRun.CreateDateTime;
                testRunInfo.NumberOfRuns = testRuns.Count;
            }
            return testRunInfo;
        }
    }
}