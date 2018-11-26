using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ChapterConverter : IChapterConverter
    {
        private readonly IUserConverter _userConverter;

        public ChapterConverter(IUserConverter userConverter)
        {
            _userConverter = userConverter;
        }

        public ChapterSummaryModel ToChapterSummaryModel(Chapter chapter, IList<AssignmentResultDto> userExerciseResults)
        {
            if (chapter.Exercises == null)
            {
                throw new ArgumentException("Chapter should have exercises loaded", nameof(chapter));
            }

            if (chapter.Exercises.Any(ex => ex.Tests == null))
            {
                throw new ArgumentException("All exercises of the chapter should have their tests loaded", nameof(chapter));
            }

            var model = new ChapterSummaryModel
            {
                Id = chapter.Id,
                Number = chapter.Number,
                ExerciseSummaries = new List<ExerciseSummaryModel>()
            };

            foreach (var exercise in chapter.Exercises.OrderBy(exercise => exercise.Code))
            {
                var userExerciseSummaryModel = CreateExerciseSummaryModel(exercise, userExerciseResults);
                model.ExerciseSummaries.Add(userExerciseSummaryModel);
            }
            return model;
        }

        public ChapterModel ToChapterModel(Chapter chapter)
        {
            return new ChapterModel
            {
                Id = chapter.Id,
                Number = chapter.Number
            };
        }

        public ChapterDetailModel ToChapterDetailModel(Chapter chapter, IList<User> chapterUsers)
        {
            return new ChapterDetailModel
            {
                Id = chapter.Id,
                Number = chapter.Number,
                Exercises = chapter.Exercises.Select(exercise => new ExerciseModel
                {
                    ExerciseId = exercise.Id,
                    Code = exercise.Code
                }).ToList(),
                Users = chapterUsers.Select(user => _userConverter.FromUser(user)).ToList()
            };
        }

        public ChapterStatisticsModel ToChapterStatisticsModel(Chapter chapter, IList<AssignmentStatisticsDto> chapterStatistics)
        {
            var model = new ChapterStatisticsModel
            {
                Id = chapter.Id,
                Number = chapter.Number,
                ExerciseStatistics = new List<ExerciseStatisticsModel>()
            };

            foreach (var exercise in chapter.Exercises.OrderBy(exercise => exercise.Code))
            {
                var exerciseStatisticsModel = CreateExerciseStatisticsModel(exercise, chapterStatistics);
                model.ExerciseStatistics.Add(exerciseStatisticsModel);
            }

            return model;
        }

        private ExerciseSummaryModel CreateExerciseSummaryModel(Exercise exercise, IList<AssignmentResultDto> exerciseResults)
        {
            var exerciseSummaryModel = new ExerciseSummaryModel
            {
                ExerciseId = exercise.Id,
                Code = exercise.Code,
                NumberOfTests = exercise.Tests.Count
            };

            var matchingResult = exerciseResults.FirstOrDefault(result => result.AssignmentId == exercise.Id);
            if (matchingResult != null)
            {
                exerciseSummaryModel.NumberOfPassedTests = matchingResult.TestResults.Count(result => result.Passed);
                exerciseSummaryModel.NumberOfFailedTests = matchingResult.TestResults.Count(result => !result.Passed);
            }
            return exerciseSummaryModel;
        }

        private ExerciseStatisticsModel CreateExerciseStatisticsModel(Exercise exercise, IList<AssignmentStatisticsDto> chapterStatistics)
        {
            var model = new ExerciseStatisticsModel
            {
                ExerciseId = exercise.Id,
                Code = exercise.Code,
                TotalNumberOfUsers = 0,
                TestPassageStatistics = new List<TestPassageStatisticModel>()
            };

            var exerciseStatistics = chapterStatistics.FirstOrDefault(result => result.AssignmentId == exercise.Id);
            if (exerciseStatistics != null)
            {
                model.TotalNumberOfUsers = exerciseStatistics.TestPassageStatistics.Sum(s => s.AmountOfUsers);
                foreach (var testPassageStatistic in exerciseStatistics.TestPassageStatistics)
                {
                    var testPassageStatisticModel = new TestPassageStatisticModel
                    {
                        AmountOfPassedTestsRange = $"{testPassageStatistic.AmountOfPassedTests} tests",
                        NumberOfUsers = testPassageStatistic.AmountOfUsers
                    };
                    model.TestPassageStatistics.Add(testPassageStatisticModel);
                }
            }

            return model;
        }
    }
}
