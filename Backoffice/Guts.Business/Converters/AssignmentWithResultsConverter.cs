using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Converters
{
    internal class AssignmentWithResultsConverter : IAssignmentWithResultsConverter
    {
        public AssignmentStatisticsDto ToAssignmentStatisticsDto(int assignmentId, IList<TestResult> lastResultsOfMultipleUsers)
        {
            var passedTestsPerUserQuery = from testResult in lastResultsOfMultipleUsers
                                          group testResult by testResult.UserId into userGroup
                                          select new
                                          {
                                              UserId = userGroup.Key,
                                              AmountOfPassedTests = userGroup.Count(g => g.Passed)
                                          };

            var testPassageStatisticsQuery = from userStatistic in passedTestsPerUserQuery
                                             group userStatistic by userStatistic.AmountOfPassedTests into amountGroup
                                             select new TestPassageStatisticDto
                                             {
                                                 AmountOfUsers = amountGroup.Count(),
                                                 AmountOfPassedTests = amountGroup.Key
                                             };

            var result = new AssignmentStatisticsDto
            {
                AssignmentId = assignmentId,
                TestPassageStatistics = testPassageStatisticsQuery.OrderBy(statistic => statistic.AmountOfPassedTests).ToList()
            };
            return result;
        }
    }
}