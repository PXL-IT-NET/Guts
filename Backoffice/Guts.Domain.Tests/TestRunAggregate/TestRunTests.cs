using System;
using System.Collections.Generic;
using Guts.Domain.TestRunAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.TestRunAggregate;

[TestFixture]
public class TestRunTests
{
    [Test]
    public void HasPassingTests_WhenTestResultsNull_ShouldReturnFalse()
    {
        var run = new TestRun { TestResults = null };
        Assert.That(run.HasPassingTests, Is.False);
    }

    [Test]
    public void HasPassingTests_WhenEmpty_ShouldReturnFalse()
    {
        var run = new TestRun { TestResults = new List<TestResult>() };
        Assert.That(run.HasPassingTests, Is.False);
    }

    [Test]
    public void HasPassingTests_WhenOnlyFailingTests_ShouldReturnFalse()
    {
        var run = new TestRun
        {
            TestResults = new List<TestResult>
            {
                new() { Passed = false },
                new() { Passed = false }
            }
        };
        Assert.That(run.HasPassingTests, Is.False);
    }

    [Test]
    public void HasPassingTests_WhenHasPassingTest_ShouldReturnTrue()
    {
        var run = new TestRun
        {
            TestResults = new List<TestResult>
            {
                new() { Passed = false },
                new() { Passed = true }
            }
        };
        Assert.That(run.HasPassingTests, Is.True);
    }

    [Test]
    public void PassedTestsCount_And_FailedTestsCount_ShouldCountCorrectly()
    {
        var run = new TestRun
        {
            TestResults = new List<TestResult>
            {
                new() { Passed = true },
                new() { Passed = true },
                new() { Passed = false }
            }
        };

        Assert.That(run.PassedTestsCount, Is.EqualTo(2));
        Assert.That(run.FailedTestsCount, Is.EqualTo(1));
    }

    [Test]
    public void PassedTestsCount_And_FailedTestsCount_WhenNull_ShouldReturnZero()
    {
        var run = new TestRun { TestResults = null };

        Assert.That(run.PassedTestsCount, Is.Zero);
        Assert.That(run.FailedTestsCount, Is.Zero);
    }

    [Test]
    public void AddTestResult_ShouldCreateAndAddTestResultToCollection()
    {
        var timestamp = new DateTime(2026, 11, 19, 14, 0, 0, DateTimeKind.Utc);
        var run = new TestRun
        {
            Id = 5,
            AssignmentId = 10,
            UserId = 20,
            CreateDateTime = timestamp,
            TestResults = null
        };

        var result = run.AddTestResult(testId: 101, passed: true, message: "OK", userId: 20, createDateTime: timestamp);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.TestId, Is.EqualTo(101));
        Assert.That(result.Passed, Is.True);
        Assert.That(result.Message, Is.EqualTo("OK"));
        Assert.That(result.UserId, Is.EqualTo(20));
        Assert.That(result.CreateDateTime, Is.EqualTo(timestamp));
        Assert.That(run.TestResults, Has.Count.EqualTo(1));
    }
}

