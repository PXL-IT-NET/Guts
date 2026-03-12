namespace Guts.Client.Core.Models;

public class TestResult(string testName, bool passed, string message)
{
    public string TestName { get; } = testName;

    public bool Passed { get; } = passed;

    public string Message { get; } = message;
}