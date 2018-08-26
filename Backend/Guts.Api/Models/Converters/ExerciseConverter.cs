using System;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ExerciseConverter : IExerciseConverter
    {
        public ExerciseDetailModel ToExerciseDetailModel(Exercise exercise, ExerciseResultDto results)
        {
            if (exercise.Chapter == null  || exercise.Chapter.Course == null)
            {
                throw new ArgumentException("Exercise should have chapter and course loaded", nameof(exercise));
            }

            var model = new ExerciseDetailModel
            {
                ChapterNumber = exercise.Chapter.Number,
                Number = exercise.Number,
                ExerciseId = exercise.Id,
                CourseName = exercise.Chapter.Course.Name,
                TestResults = results.TestResults
            };

            return model;
        }
    }
}