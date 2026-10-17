using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.AssignmentAggregate
{
    [TestFixture]
    public class AssignmentResultTests
    {
        [Test]
        public void Constructor_NullLastTestResults_ShouldInitializeEmptyResultsAndZeroPassingTests()
        {
            int assignmentId = Random.Shared.NextPositive();
            int userId = Random.Shared.NextPositive();

            var result = new AssignmentResult(assignmentId, userId, null);

            Assert.That(result.AssignmentId, Is.EqualTo(assignmentId));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.LastTestResults, Is.Empty);
            Assert.That(result.NumberOfPassingTests, Is.Zero);
        }

        [Test]
        public void Constructor_NonNullLastTestResults_ShouldPopulateResultsAndCountPassingTests()
        {
            int assignmentId = Random.Shared.NextPositive();
            int userId = Random.Shared.NextPositive();
            var testResults = new List<TestResult>
            {
                new TestResultBuilder().WithUser(userId).WithPassed(true).Build(),
                new TestResultBuilder().WithUser(userId).WithPassed(false).Build(),
                new TestResultBuilder().WithUser(userId).WithPassed(true).Build()
            };

            var result = new AssignmentResult(assignmentId, userId, testResults);

            Assert.That(result.AssignmentId, Is.EqualTo(assignmentId));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.LastTestResults, Is.EquivalentTo(testResults));
            Assert.That(result.NumberOfPassingTests, Is.EqualTo(2));
        }

        [Test]
        public void Equality_SameAssignmentUserAndPassingTestCount_ShouldBeEqual()
        {
            int assignmentId = Random.Shared.NextPositive();
            int userId = Random.Shared.NextPositive();
            var testResultsA = new List<TestResult>
            {
                new TestResultBuilder().WithUser(userId).WithPassed(true).Build(),
                new TestResultBuilder().WithUser(userId).WithPassed(false).Build()
            };
            var testResultsB = new List<TestResult>
            {
                new TestResultBuilder().WithUser(userId).WithPassed(true).Build()
            };

            var resultA = new AssignmentResult(assignmentId, userId, testResultsA);
            var resultB = new AssignmentResult(assignmentId, userId, testResultsB);

            Assert.That(resultA.Equals(resultB), Is.True);
            Assert.That(resultA == resultB, Is.True);
            Assert.That(resultA != resultB, Is.False);
            Assert.That(resultA.GetHashCode(), Is.EqualTo(resultB.GetHashCode()));
        }

        [Test]
        public void Equality_DifferentAssignmentUserOrPassingTestCount_ShouldNotBeEqual()
        {
            int assignmentId = Random.Shared.NextPositive();
            int userId = Random.Shared.NextPositive();
            var passingResult = new List<TestResult>
            {
                new TestResultBuilder().WithUser(userId).WithPassed(true).Build()
            };
            var failingResult = new List<TestResult>
            {
                new TestResultBuilder().WithUser(userId).WithPassed(false).Build()
            };

            var baseResult = new AssignmentResult(assignmentId, userId, passingResult);
            var diffAssignment = new AssignmentResult(assignmentId + 1, userId, passingResult);
            var diffUser = new AssignmentResult(assignmentId, userId + 1, passingResult);
            var diffPassingCount = new AssignmentResult(assignmentId, userId, failingResult);

            Assert.That(baseResult, Is.Not.EqualTo(diffAssignment));
            Assert.That(baseResult, Is.Not.EqualTo(diffUser));
            Assert.That(baseResult, Is.Not.EqualTo(diffPassingCount));
        }
    }
}
