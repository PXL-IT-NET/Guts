using System.Collections.Generic;
using System.Linq;
using Guts.Domain.TestRunAggregate;

namespace Guts.Domain.AssignmentAggregate;

public class AssignmentPassageDistribution
{
    public int AssignmentId { get; }
    public IReadOnlyList<TestPassageCount> Statistics { get; }

    public AssignmentPassageDistribution(int assignmentId, IEnumerable<TestResult> testResults)
    {
        AssignmentId = assignmentId;

        if (testResults == null)
        {
            Statistics = [];
            return;
        }

        var passedTestsPerUserQuery = from testResult in testResults
            group testResult by testResult.UserId into userGroup
            select new
            {
                UserId = userGroup.Key,
                AmountOfPassedTests = userGroup.Count(g => g.Passed)
            };

        var testPassageStatisticsQuery = from userStatistic in passedTestsPerUserQuery
            group userStatistic by userStatistic.AmountOfPassedTests into amountGroup
            select new TestPassageCount(amountGroup.Count(), amountGroup.Key);

        Statistics = testPassageStatisticsQuery.OrderBy(s => s.AmountOfPassedTests).ToList();
    }
}

