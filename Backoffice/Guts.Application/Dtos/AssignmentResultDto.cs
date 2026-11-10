using System.Collections.Generic;
using Guts.Domain.TestRunAggregate;

namespace Guts.Application.Dtos;

public class AssignmentResultDto
{
    public int AssignmentId { get; set; }
    public IList<TestResult> TestResults { get; set; }
}