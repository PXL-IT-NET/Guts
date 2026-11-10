using System.Collections.Generic;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.AssignmentAggregate;

[TestFixture]
public class AssignmentPassageDistributionTests
{
    [Test]
    public void Constructor_NullTestResults_InitializesEmptyStatistics()
    {
        var distribution = new AssignmentPassageDistribution(42, null);

        Assert.That(distribution.AssignmentId, Is.EqualTo(42));
        Assert.That(distribution.Statistics, Is.Empty);
    }

    [Test]
    public void Constructor_CalculatesCorrectDistributionAndOrdersByAmountOfPassedTests()
    {
        // 3 users:
        // User 1 passes 2 tests
        // User 2 passes 2 tests
        // User 3 passes 1 test
        // Result:
        // 1 user passed 1 test
        // 2 users passed 2 tests
        var results = new List<TestResult>
        {
            new() { UserId = 1, TestId = 10, Passed = true },
            new() { UserId = 1, TestId = 11, Passed = true },
            new() { UserId = 1, TestId = 12, Passed = false },

            new() { UserId = 2, TestId = 10, Passed = true },
            new() { UserId = 2, TestId = 11, Passed = true },

            new() { UserId = 3, TestId = 10, Passed = true },
            new() { UserId = 3, TestId = 11, Passed = false }
        };

        var distribution = new AssignmentPassageDistribution(99, results);

        Assert.That(distribution.AssignmentId, Is.EqualTo(99));
        Assert.That(distribution.Statistics, Has.Count.EqualTo(2));

        Assert.That(distribution.Statistics[0].AmountOfPassedTests, Is.EqualTo(1));
        Assert.That(distribution.Statistics[0].AmountOfUsers, Is.EqualTo(1));

        Assert.That(distribution.Statistics[1].AmountOfPassedTests, Is.EqualTo(2));
        Assert.That(distribution.Statistics[1].AmountOfUsers, Is.EqualTo(2));
    }
}

