using System.Collections.Generic;
using Guts.Domain;

namespace Guts.Data
{
    public class TestWithLastResultOfMultipleUsers
    {
        public Test Test { get; set; }
        public IEnumerable<TestResult> TestResults { get; set; }
    }
}