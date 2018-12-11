using Guts.Data;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Business.Converters
{
    public class AssignmentWitResultsConverter : IAssignmentWitResultsConverter
    {
        public AssignmentResultDto ToAssignmentResultDto(AssignmentWithLastResultsOfUser assignmentWithLastResultsOfUser)
        {
            var resultDto = new AssignmentResultDto
            {
                AssignmentId = assignmentWithLastResultsOfUser?.Assignment?.Id ?? 0,
                TestResults = new List<TestResultDto>()
            };

            var testsWithResults = assignmentWithLastResultsOfUser?.TestsWithLastResultOfUser?.ToList() ??
                                   new List<TestWithLastResultOfUser>();

            foreach (var testWithResults in testsWithResults)
            {
                var testResultDto = new TestResultDto
                {
                    TestId = testWithResults.Test.Id,
                    TestName = testWithResults.Test.TestName,
                    Passed = testWithResults.TestResult.Passed,
                    Message = testWithResults.TestResult.Message
                };
                resultDto.TestResults.Add(testResultDto);
            }

            return resultDto;

        }

        public AssignmentStatisticsDto ToAssignmentStatisticsDto(
            AssignmentWithLastResultsOfMultipleUsers assignmentWithLastResultsOfMultipleUsers)
        {
            var passedTestsPerUserQuery = from testWithLastResultOfMultipleUsers in assignmentWithLastResultsOfMultipleUsers.TestsWithLastResultOfMultipleUsers
                                          from testResult in testWithLastResultOfMultipleUsers.TestResults
                                          group testResult by testResult.UserId into userGroup
                                          select new
                                          {
                                              UserId = userGroup.Key,
                                              AmountOfPassedTests = userGroup.Count(g => g.Passed)
                                          };

            var testPassageStatisticsQuery = from userStatistic in passedTestsPerUserQuery
                                             group userStatistic by userStatistic.AmountOfPassedTests into amountGroup
                                             select new TestPassageStatistic
                                             {
                                                 AmountOfUsers = amountGroup.Count(),
                                                 AmountOfPassedTests = amountGroup.Key
                                             };

            var result = new AssignmentStatisticsDto
            {
                AssignmentId = assignmentWithLastResultsOfMultipleUsers.Assignment.Id,
                TestPassageStatistics = testPassageStatisticsQuery.OrderBy(statistic => statistic.AmountOfPassedTests).ToList()
            };
            return result;
        }
    }
}