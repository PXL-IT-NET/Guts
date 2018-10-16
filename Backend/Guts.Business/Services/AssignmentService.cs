using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Converters;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;

namespace Guts.Business.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IChapterService _chapterService;
        private readonly ITestRepository _testRepository;
        private readonly ITestResultRepository _testResultRepository;
        private readonly ITestResultConverter _testResultConverter;
        private readonly ITestRunRepository _testRunRepository;

        public AssignmentService(IExerciseRepository exerciseRepository, 
            IChapterService chapterService, 
            ITestRepository testRepository,
            ITestResultRepository testResultRepository, 
            ITestResultConverter testResultConverter,
            ITestRunRepository testRunRepository)
        {
            _exerciseRepository = exerciseRepository;
            _chapterService = chapterService;
            _testRepository = testRepository;
            _testResultRepository = testResultRepository;
            _testResultConverter = testResultConverter;
            _testRunRepository = testRunRepository;
        }

        public async Task<Exercise> GetOrCreateExerciseAsync(ExerciseDto exerciseDto)
        {
            var chapter = await _chapterService.GetOrCreateChapterAsync(exerciseDto.CourseCode, exerciseDto.ChapterNumber);

            Exercise exercise;
            try
            {
                exercise = await _exerciseRepository.GetSingleAsync(chapter.Id, Convert.ToString(exerciseDto.ExerciseNumber));
            }
            catch (DataNotFoundException)
            {
                exercise = new Exercise
                {
                    ChapterId = chapter.Id,
                    Code = Convert.ToString(exerciseDto.ExerciseNumber)
                };
                exercise = await _exerciseRepository.AddAsync(exercise);
            }

            return exercise;
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

        public async Task<ExerciseResultDto> GetResultsForUserAsync(int exerciseId, int userId, DateTime? dateUtc)
        {
            var lastTestResults = await _testResultRepository.GetLastTestResultsOfExerciseAsync(exerciseId, userId, dateUtc);

            return _testResultConverter.ToExerciseResultDto(lastTestResults).FirstOrDefault();
        }

        public async Task<ExerciseTestRunInfoDto> GetUserTestRunInfoForExercise(int exerciseId, int userId, DateTime? dateUtc)
        {
            //TODO: write unit test
            var testRunInfo = new ExerciseTestRunInfoDto();

            var testRuns = await _testRunRepository.GetUserTestRunsForExercise(exerciseId, userId, dateUtc);
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
    }
}