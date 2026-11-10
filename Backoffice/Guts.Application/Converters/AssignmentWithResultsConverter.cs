using System.Collections.Generic;
using System.Linq;
using Guts.Application.Dtos;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Application.Converters;

internal class AssignmentWithResultsConverter : IAssignmentWithResultsConverter
{
    public AssignmentStatisticsDto ToAssignmentStatisticsDto(int assignmentId, IList<TestResult> lastResultsOfMultipleUsers)
    {
        var distribution = new AssignmentPassageDistribution(assignmentId, lastResultsOfMultipleUsers);

        return new AssignmentStatisticsDto
        {
            AssignmentId = distribution.AssignmentId,
            TestPassageStatistics = distribution.Statistics.Select(s => new TestPassageStatisticDto
            {
                AmountOfUsers = s.AmountOfUsers,
                AmountOfPassedTests = s.AmountOfPassedTests
            }).ToList()
        };
    }
}