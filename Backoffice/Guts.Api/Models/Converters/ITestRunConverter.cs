using System.Collections.Generic;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Api.Models.Converters
{
    public interface ITestRunConverter
    {
        TestRun From(IEnumerable<TestResultModel> testResultModels, int userId, Assignment assignment);
        SavedTestRunModel ToTestRunModel(TestRun testRun);
    }
}