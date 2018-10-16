using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public class TestRunConverter : ITestRunConverter
    {
        public TestRun From(IEnumerable<TestResultModel> testResultModels, string sourceCode, int userId, Exercise exercise)
        {
            if(exercise == null) throw new ArgumentNullException(nameof(exercise));

            var testRun = new TestRun
            {
                UserId = userId,
                AssignmentId = exercise.Id,
                SourceCode = sourceCode,
                CreateDateTime = DateTime.Now.ToUniversalTime(),
                TestResults = new List<TestResult>()
            };

            testResultModels = testResultModels ?? new List<TestResultModel>();

            foreach (var testResultModel in testResultModels)
            {
                var test = exercise.Tests.FirstOrDefault(t => t.TestName.ToLower() == testResultModel.TestName.ToLower());
                if(test == null) throw new ArgumentException($"No test found that has the name '{testResultModel.TestName}'.");

                var testResult = new TestResult
                {
                    TestId = test.Id,
                    Passed = testResultModel.Passed,
                    Message = testResultModel.Message,
                    UserId = userId,
                    CreateDateTime = DateTime.Now.ToUniversalTime()
                };
                testRun.TestResults.Add(testResult);
            }
            return testRun;
        }

        

        public SavedTestRunModel ToTestRunModel(TestRun testRun)
        {
            var model = new SavedTestRunModel
            {
                Id = testRun.Id,
                ExerciseId = testRun.AssignmentId,
                TestResults = new List<SavedTestResultModel>()
            };

            testRun.TestResults = testRun.TestResults ?? new List<TestResult>();

            foreach (var testResult in testRun.TestResults)
            {
                var testResultModel = new SavedTestResultModel
                {
                    Id = testResult.Id,
                    Passed = testResult.Passed
                };
                model.TestResults.Add(testResultModel);
            }

            return model;
        }
    }
}