using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ChapterConverter : IChapterConverter
    {
        public ChapterContentsModel ToChapterContentsModel(Chapter chapter, IList<ExerciseResultDto> exerciseResults)
        {
            if (chapter.Exercises == null)
            {
                throw new ArgumentException("Chapter should have exercises loaded", nameof(chapter));
            }

            if (chapter.Exercises.Any(ex => ex.Tests == null))
            {
                throw new ArgumentException("All exercises of the chapter should have their tests loaded", nameof(chapter));
            }

            var model = new ChapterContentsModel {Exercises = new List<ExerciseSummaryModel>()};

            foreach (var exercise in chapter.Exercises)
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
                    exerciseSummaryModel.NumberOfPassedTests = matchingResult.TestResults.Count(result => result.Passed);
                    exerciseSummaryModel.NumberOfFailedTests = matchingResult.TestResults.Count(result => !result.Passed);
                }

                model.Exercises.Add(exerciseSummaryModel);
            }
            return model;
        }
    }
}
