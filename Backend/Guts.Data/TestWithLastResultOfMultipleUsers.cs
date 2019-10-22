using System.Collections.Generic;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Data
{
    public class TestWithLastResultOfMultipleUsers
    {
        public Test Test { get; set; }
        public IEnumerable<TestResult> TestResults { get; set; }
    }
}