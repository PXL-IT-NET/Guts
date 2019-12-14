using System.Collections.Generic;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Dtos
{
    public class AssignmentResultDto
    {
        public int AssignmentId { get; set; }
        public IList<TestResult> TestResults { get; set; }
    }
}