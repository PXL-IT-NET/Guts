namespace Guts.Client.Core.Models
{
    public class TestResult
    {
        public string TestName { get; }

        public bool Passed { get; }

        public string Message { get; }

        public TestResult(string testName, bool passed, string message)
        {
            TestName = testName;
            Passed = passed;
            Message = message;
        }
    }
}
