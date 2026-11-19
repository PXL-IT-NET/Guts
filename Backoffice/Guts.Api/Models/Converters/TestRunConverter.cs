using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Api.Models.Converters;

public class TestRunConverter : ITestRunConverter
{
    public TestRun From(IEnumerable<TestResultModel> testResultModels, int userId, Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        var incomingResults = testResultModels?.Select(m => (m.TestName, m.Passed, m.Message));
        return assignment.CreateTestRun(userId, incomingResults, DateTime.UtcNow);
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