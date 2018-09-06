using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface ITestRunConverter
    {
        TestRun From(IEnumerable<TestResultModel> testResultModels, string sourceCode, int userId, Exercise exercise);
        SavedTestRunModel ToTestRunModel(TestRun testRun);
    }
}