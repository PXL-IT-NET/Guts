using System;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ExerciseConverter : IExerciseConverter
    {
        public ExerciseDetailModel ToExerciseDetailModel(Exercise exercise, ExerciseResultDto results, ExerciseTestRunInfoDto testRunInfo)
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
                CourseId= exercise.Chapter.CourseId,
                TestResults = results.TestResults,
                FirstRun = testRunInfo.FirstRunDateTime?.ToString("dd/MM/yyyy HH:mm"),
                LastRun = testRunInfo.LastRunDateTime?.ToString("dd/MM/yyyy HH:mm"),
                NumberOfRuns = testRunInfo.NumberOfRuns
            };

            return model;
        }
    }
}