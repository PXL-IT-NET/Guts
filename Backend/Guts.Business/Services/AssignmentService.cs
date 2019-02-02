using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;

namespace Guts.Business.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IChapterService _chapterService;
        private readonly IProjectService _projectService;
        private readonly ITestRepository _testRepository;
        private readonly ITestResultRepository _testResultRepository;
        private readonly ITestRunRepository _testRunRepository;

        public AssignmentService(IAssignmentRepository assignmentRepository,
            IChapterService chapterService,
            IProjectService projectService,
            ITestRepository testRepository,
            ITestResultRepository testResultRepository, 
            ITestRunRepository testRunRepository)
        {
            _chapterService = chapterService;
            _projectService = projectService;
            _assignmentRepository = assignmentRepository;
            _testRepository = testRepository;
            _testResultRepository = testResultRepository;
            _testRunRepository = testRunRepository;
        }

        public async Task<Assignment> GetOrCreateExerciseAsync(AssignmentDto assignmentDto)
        {
            var chapter = await _chapterService.GetOrCreateChapterAsync(assignmentDto.CourseCode, assignmentDto.TopicCode);
            return await GetOrCreateAssignmentAsync(chapter.Id, assignmentDto.AssignmentCode);
        }

        public async Task<Assignment> GetOrCreateProjectComponentAsync(AssignmentDto assignmentDto)
        {
            var project =
                await _projectService.GetOrCreateProjectAsync(assignmentDto.CourseCode, assignmentDto.TopicCode);
            return await GetOrCreateAssignmentAsync(project.Id, assignmentDto.AssignmentCode);
        }

        public async Task LoadTestsForAssignmentAsync(Assignment assignment)
        {
            var assignmentTests = await _testRepository.FindByAssignmentId(assignment.Id);
            assignment.Tests = assignmentTests;
        }

        public async Task LoadOrCreateTestsForAssignmentAsync(Assignment assignment, IEnumerable<string> testNames)
        {
            var assignmentTests = await _testRepository.FindByAssignmentId(assignment.Id);

            foreach (var testName in testNames)
            {
                if (assignmentTests.All(test => test.TestName != testName))
                {
                    var newTest = new Test
                    {
                        AssignmentId = assignment.Id,
                        TestName = testName
                    };
                    var savedTest = await _testRepository.AddAsync(newTest);
                    assignmentTests.Add(savedTest);
                }
            }
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
            var testRunInfo = new AssignmentTestRunInfoDto();

            var testRuns = await _testRunRepository.GetUserTestRunsForAssignmentAsync(assignmentId, userId, dateUtc);
            if (testRuns.Any())
            {
                var firstTestRun = testRuns.First();
                var lastTestRun = testRuns.Last();
                testRunInfo.FirstRunDateTime = firstTestRun.CreateDateTime;
                testRunInfo.LastRunDateTime = lastTestRun.CreateDateTime;
                testRunInfo.SourceCode = lastTestRun.SourceCode;
                testRunInfo.NumberOfRuns = testRuns.Count;
            }

            return testRunInfo;
        }

        public async Task<AssignmentResultDto> GetResultsForTeamAsync(int assignmentId, int teamId, DateTime? dateUtc)
        {
            //TODO: add unit tests
            return new AssignmentResultDto
            {
                AssignmentId = assignmentId,
                TestResults = await _testResultRepository.GetLastTestResultsOfTeam(assignmentId, teamId, dateUtc)
            };
        }

        public async Task<AssignmentTestRunInfoDto> GetTeamTestRunInfoForAssignment(int assignmentId, int teamId, DateTime? dateUtc)
        {
            //TODO: add unit tests
            var testRunInfo = new AssignmentTestRunInfoDto();

            var testRuns = await _testRunRepository.GetTeamTestRunsForAssignmentAsync(assignmentId, teamId, dateUtc);
            if (testRuns.Any())
            {
                var firstTestRun = testRuns.First();
                var lastTestRun = testRuns.Last();
                testRunInfo.FirstRunDateTime = firstTestRun.CreateDateTime;
                testRunInfo.LastRunDateTime = lastTestRun.CreateDateTime;
                testRunInfo.SourceCode = lastTestRun.SourceCode;
                testRunInfo.NumberOfRuns = testRuns.Count;
            }

            return testRunInfo;
        }

        public async Task<IList<AssignmentSourceDto>> GetAllSourceCodes(int assignmentId)
        {
            var testRunsWithUser = await _testRunRepository.GetLastTestRunForAssignmentOfAllUsersAsync(assignmentId);

            return testRunsWithUser.Select(testrun => new AssignmentSourceDto
            {
                Source = testrun.SourceCode,
                UserId = testrun.UserId,
                UserFullName = $"{testrun.User.FirstName} {testrun.User.LastName}".Trim()
            }).OrderBy(dto => dto.UserFullName).ToList();
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

        private async Task<Assignment> GetOrCreateAssignmentAsync(int topicId, string assignmentCode)
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
    }
}