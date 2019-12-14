using System;
using System.Collections;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    [TestFixture]
    public class AssignmentEvaluationTest
    {
        private Random _random;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _random = new Random();
        }

        [Test]
        public void Constructor_ShouldConstructValidExamPart()
        {
            var validExamPartId = _random.NextPositive();
            var assignment = new AssignmentBuilder()
                .WithId()
                .WithRandomTests(_random.Next(2, 11))
                .Build();
            var validMaximumScore = _random.Next(1, 101);
            var validNumberOfTestsAlreadyGreenAtStart = _random.Next(0, assignment.Tests.Count);

            var evaluation = new AssignmentEvaluation(validExamPartId, assignment, 
                validMaximumScore, validNumberOfTestsAlreadyGreenAtStart);

            Assert.That(evaluation, Is.Not.Null);
            Assert.That(evaluation.Id, Is.EqualTo(0));
            Assert.That(evaluation.ExamPartId, Is.EqualTo(validExamPartId));
            Assert.That(evaluation.AssignmentId, Is.EqualTo(assignment.Id));
            Assert.That(evaluation.MaximumScore, Is.EqualTo(validMaximumScore));
            Assert.That(evaluation.NumberOfTestsAlreadyGreenAtStart, Is.EqualTo(validNumberOfTestsAlreadyGreenAtStart));
        }

        [TestCase(-1, 10, 1)]
        [TestCase(1, 0, 0)]
        [TestCase(1, 10, -1)]
        public void Constructor_ShouldThrowContractExceptionOnInvalidInput(
            int examPartId, 
            int maximumScore,
            int numberOfTestsAlreadyGreenAtStart)
        {
            var validAssignment = new AssignmentBuilder()
                .WithId()
                .WithRandomTests(_random.Next(2, 11))
                .Build();
            Assert.That(() => new AssignmentEvaluation(
                examPartId, 
                validAssignment, 
                maximumScore, 
                numberOfTestsAlreadyGreenAtStart), 
                Throws.InstanceOf<ContractException>());
        }

        [TestCaseSource(typeof(InvalidAssignmentCases))]
        public void Constructor_ShouldThrowContractExceptionOnInvalidAssignment(Assignment invalidAssignment)
        {
            Assert.That(() => new AssignmentEvaluation(
                    1,
                    invalidAssignment,
                    10,
                    0),
                Throws.InstanceOf<ContractException>());
        }

        private class InvalidAssignmentCases : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                var assignmentWithoutTests = new AssignmentBuilder().WithId().Build();
                yield return assignmentWithoutTests;

                var assignmentWithoutId = new AssignmentBuilder().WithRandomTests(1).Build();
                yield return assignmentWithoutId;
            }
        }

        [TestCase(5, 6)]
        [TestCase(5, 5)]
        public void Constructor_ShouldThrowContractExceptionWhenTestsAlreadyGreenAreEqualOrGreaterThanNumberOfTestsInTheAssignment(int numberOfTests, int numberOfTestsAlreadyGreen)
        {
            var assignment = new AssignmentBuilder()
                .WithId()
                .WithRandomTests(numberOfTests)
                .Build();

            Assert.That(() => new AssignmentEvaluation(
                    1,
                    assignment,
                    10,
                    numberOfTestsAlreadyGreen),
                Throws.InstanceOf<ContractException>());
        }
    }
}