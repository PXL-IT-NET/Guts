using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Business
{
    public class AssignmentResultDto
    {
        public int AssignmentId { get; set; }
        public IList<TestResult> TestResults { get; set; }
    }
}