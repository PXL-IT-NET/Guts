using System.Collections.Generic;
using Guts.Application.Dtos;
using Guts.Domain.TestRunAggregate;

namespace Guts.Application.Converters;

public interface IAssignmentWithResultsConverter
{
    AssignmentStatisticsDto ToAssignmentStatisticsDto(int assignmentId, IList<TestResult> lastResultsOfMultipleUsers);
}