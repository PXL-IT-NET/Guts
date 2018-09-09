using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class ExerciseConverter : IExerciseConverter
    {
        public ExerciseDetailModel ToExerciseDetailModel(Exercise exercise, ExerciseResultDto results, ExerciseTestRunInfoDto testRunInfo)
        {
            if (exercise.Chapter?.Course == null)
            {
                throw new ArgumentException("Exercise should have chapter and course loaded", nameof(exercise));
            }

            if (exercise.Tests == null)
            {
                throw new ArgumentException("Exercise should have tests loaded", nameof(exercise));
            }

            if (testRunInfo == null)
            {
                throw new ArgumentNullException(nameof(testRunInfo));
            }

            var model = new ExerciseDetailModel
            {
                ChapterNumber = exercise.Chapter.Number,
                Number = exercise.Number,
                ExerciseId = exercise.Id,
                CourseName = exercise.Chapter.Course.Name,
                CourseId= exercise.Chapter.CourseId,
                TestResults = new List<TestResultModel>(),
                FirstRun = testRunInfo.FirstRunDateTime?.ToString("dd/MM/yyyy HH:mm"),
                LastRun = testRunInfo.LastRunDateTime?.ToString("dd/MM/yyyy HH:mm"),
                NumberOfRuns = testRunInfo.NumberOfRuns,
                SourceCode = testRunInfo.SourceCode
            };

            foreach (var test in exercise.Tests)
            {
                var testResultModel = new TestResultModel
                {
                    TestName = test.TestName,
                    Runned = false,
                    Passed = false,
                    Message = string.Empty
                };

                var matchingResult = results?.TestResults?.FirstOrDefault(r => r.TestId == test.Id);
                if (matchingResult != null)
                {
                    testResultModel.Runned = true;
                    testResultModel.Passed = matchingResult.Passed;
                    testResultModel.Message = matchingResult.Message;
                }

                model.TestResults.Add(testResultModel);
            }

            return model;
        }
    }
}