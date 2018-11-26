using System.Collections.Generic;

namespace Guts.Business
{
    public class AssignmentResultDto
    {
        public int AssignmentId { get; set; }
        public IList<TestResultDto> TestResults { get; set; }
    }
}