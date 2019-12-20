using System.Collections.Generic;
using Guts.Business.Dtos;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Converters
{
    public interface IAssignmentWithResultsConverter
    {
        AssignmentStatisticsDto ToAssignmentStatisticsDto(int assignmentId, IList<TestResult> lastResultsOfMultipleUsers);
    }
}
