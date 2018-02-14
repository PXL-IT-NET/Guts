using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Data
{
    public class TestWithLastUserResults
    {
        public Test Test { get; set; }
        public int NumberOfUsers { get; set; }
        public IEnumerable<TestResult> ResultsOfUsers { get; set; }
    }
}