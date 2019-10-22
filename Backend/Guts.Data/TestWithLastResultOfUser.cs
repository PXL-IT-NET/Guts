using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Data
{
    public class TestWithLastResultOfUser
    {
        public Test Test { get; set; }
        public TestResult TestResult { get; set; }
    }
}