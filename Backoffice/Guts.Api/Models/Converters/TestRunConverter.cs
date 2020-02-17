using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Api.Models.Converters
{
    public class TestRunConverter : ITestRunConverter
    {
        public TestRun From(IEnumerable<TestResultModel> testResultModels, int userId, Assignment assignment)
        {
            if(assignment == null) throw new ArgumentNullException(nameof(assignment));

            var testRun = new TestRun
            {
                UserId = userId,
                AssignmentId = assignment.Id,
                CreateDateTime = DateTime.UtcNow,
                TestResults = new List<TestResult>()
            };

            testResultModels ??= new List<TestResultModel>();

            foreach (var testResultModel in testResultModels)
            {
                var test = assignment.Tests.FirstOrDefault(t => t.TestName.ToLower() == testResultModel.TestName.ToLower());
                if (test == null) continue;

                var testResult = new TestResult
                {
                    TestId = test.Id,
                    Passed = testResultModel.Passed,
                    Message = testResultModel.Message,
                    UserId = userId,
                    CreateDateTime = DateTime.UtcNow
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
                AssignmentId = testRun.AssignmentId,
                TestResults = new List<SavedTestResultModel>()
            };

            testRun.TestResults ??= new List<TestResult>();

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