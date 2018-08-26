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
    public class ExerciseService : IExerciseService
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IChapterService _chapterService;
        private readonly ITestRepository _testRepository;
        private readonly ITestResultRepository _testResultRepository;
        private readonly ITestResultConverter _testResultConverter;

        public ExerciseService(IExerciseRepository exerciseRepository, 
            IChapterService chapterService, 
            ITestRepository testRepository,
            ITestResultRepository testResultRepository, 
            ITestResultConverter testResultConverter)
        {
            _exerciseRepository = exerciseRepository;
            _chapterService = chapterService;
            _testRepository = testRepository;
            _testResultRepository = testResultRepository;
            _testResultConverter = testResultConverter;
        }

        public async Task<Exercise> GetOrCreateExerciseAsync(ExerciseDto exerciseDto)
        {
            var chapter = await _chapterService.GetOrCreateChapterAsync(exerciseDto.CourseCode, exerciseDto.ChapterNumber);

            Exercise exercise;
            try
            {
                exercise = await _exerciseRepository.GetSingleAsync(chapter.Id, exerciseDto.ExerciseNumber);
            }
            catch (DataNotFoundException)
            {
                exercise = new Exercise
                {
                    ChapterId = chapter.Id,
                    Number = exerciseDto.ExerciseNumber
                };
                exercise = await _exerciseRepository.AddAsync(exercise);
            }

            return exercise;
        }

        public async Task LoadOrCreateTestsForExerciseAsync(Exercise exercise, IEnumerable<string> testNames)
        {
            var exerciseTests = await _testRepository.FindByExercise(exercise.Id);

            foreach (var testName in testNames)
            {
                if (exerciseTests.All(test => test.TestName != testName))
                {
                    var newTest = new Test
                    {
                        ExerciseId = exercise.Id,
                        TestName = testName
                    };
                    var savedTest = await _testRepository.AddAsync(newTest);
                    exerciseTests.Add(savedTest);
                }
            }
            exercise.Tests = exerciseTests;

        }

        public async Task<ExerciseResultDto> GetResultsForUserAsync(int exerciseId, int userId)
        {
            var lastTestResults = await _testResultRepository.GetLastTestResultsOfExerciseAsync(exerciseId, userId);

            return _testResultConverter.ToExerciseResultDto(lastTestResults).FirstOrDefault();
        }
    }
}