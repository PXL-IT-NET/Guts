using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Business.Converters
{
    public interface IAssignmentWitResultsConverter
    {
        AssignmentStatisticsDto ToAssignmentStatisticsDto(int assignmentId, IList<TestResult> lastResultsOfMultipleUsers);
    }
}
