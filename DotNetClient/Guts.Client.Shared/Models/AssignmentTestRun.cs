using System.Collections.Generic;

namespace Guts.Client.Shared.Models
{
    public class AssignmentTestRun
    {
        public Assignment Assignment { get; set; }

        public IEnumerable<TestResult> Results { get; set; }

        public string SourceCode { get; set; }

        public string TestCodeHash { get; set; }
    }
}