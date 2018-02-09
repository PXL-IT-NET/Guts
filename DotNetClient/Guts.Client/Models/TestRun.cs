using System.Collections.Generic;

namespace Guts.Client.Models
{
    internal class TestRun
    {
        public Exercise Exercise { get; set; }

        public IEnumerable<TestResult> Results { get; set; }
    }
}