using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.TestRunAggregate;

public class TestRun : AggregateRoot
{
    public int Id { get; set; }

    public virtual Assignment Assignment { get; set; }
    public int AssignmentId { get; set; }

    public virtual User User { get; set; }
    public int UserId { get; set; }

    public DateTime CreateDateTime { get; set; }

    public virtual ICollection<TestResult> TestResults { get; set; } = new HashSet<TestResult>();

    public bool HasPassingTests => TestResults != null && TestResults.Any(r => r.Passed);
    public int PassedTestsCount => TestResults?.Count(r => r.Passed) ?? 0;
    public int FailedTestsCount => TestResults?.Count(r => !r.Passed) ?? 0;

    public TestResult AddTestResult(int testId, bool passed, string message, int userId, DateTime createDateTime)
    {
        TestResults ??= new HashSet<TestResult>();
        var testResult = new TestResult
        {
            TestId = testId,
            Passed = passed,
            Message = message,
            UserId = userId,
            CreateDateTime = createDateTime
        };
        TestResults.Add(testResult);
        return testResult;
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}