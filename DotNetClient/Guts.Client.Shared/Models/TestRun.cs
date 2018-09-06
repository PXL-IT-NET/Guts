using System.Collections.Generic;

namespace Guts.Client.Shared.Models
{
    public class TestRun
    {
        public Exercise Exercise { get; set; }

        public IEnumerable<TestResult> Results { get; set; }

        public string SourceCode { get; set; }
    }
}