using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ChapterConverter : IChapterConverter
    {
        public ChapterContentsModel ToChapterContentsModel(Chapter chapter, 
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

            var model = new ChapterContentsModel
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

            var matchingUserResult = exerciseResults.FirstOrDefault(result => result.ExerciseId == exercise.Id);
            if (matchingUserResult != null)
            {
                exerciseSummaryModel.NumberOfPassedTests = matchingUserResult.TestResults.Count(result => result.Passed);
                exerciseSummaryModel.NumberOfFailedTests = matchingUserResult.TestResults.Count(result => !result.Passed);
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
    }
}
