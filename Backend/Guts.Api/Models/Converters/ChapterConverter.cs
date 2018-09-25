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

        public ChapterSummaryModel ToChapterSummaryModel(Chapter chapter, 
            IList<ExerciseResultDto> userExerciseResults,
            IList<ExerciseResultDto> averageExerciseResults)
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
                UserExerciseSummaries = new List<ExerciseSummaryModel>(),
                AverageExerciseSummaries = new List<ExerciseSummaryModel>()
            };

            foreach (var exercise in chapter.Exercises.OrderBy(exercise => exercise.Number))
            {
                var userExerciseSummaryModel = CreateExerciseSummaryModel(exercise, userExerciseResults);
                model.UserExerciseSummaries.Add(userExerciseSummaryModel);

                var averageExerciseSummaryModel = CreateExerciseSummaryModel(exercise, averageExerciseResults);
                model.AverageExerciseSummaries.Add(averageExerciseSummaryModel);
            }
            return model;
        }

        private ExerciseSummaryModel CreateExerciseSummaryModel(Exercise exercise, IList<ExerciseResultDto> exerciseResults)
        {
            var exerciseSummaryModel = new ExerciseSummaryModel
            {
                ExerciseId = exercise.Id,
                Number = exercise.Number,
                NumberOfTests = exercise.Tests.Count
            };

            var matchingResult = exerciseResults.FirstOrDefault(result => result.ExerciseId == exercise.Id);
            if (matchingResult != null)
            {
                exerciseSummaryModel.NumberOfUsers = matchingResult.UserCount;
                exerciseSummaryModel.NumberOfPassedTests = matchingResult.TestResults.Count(result => result.Passed);
                exerciseSummaryModel.NumberOfFailedTests = matchingResult.TestResults.Count(result => !result.Passed);
            }
            return exerciseSummaryModel;
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
                    Number = exercise.Number
                }).ToList(),
                Users = chapterUsers.Select(user => _userConverter.FromUser(user)).ToList()
            };
        }
    }
}
