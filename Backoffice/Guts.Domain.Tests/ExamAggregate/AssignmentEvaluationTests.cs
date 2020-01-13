using System.Collections;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    [TestFixture]
    public class AssignmentEvaluationTests : DomainTestBase
    {
        [Test]
        public void Constructor_ShouldConstructValidExamPart()
        {
            var validExamPartId = Random.NextPositive();
            var assignment = new AssignmentBuilder()
                .WithId()
                .WithRandomTests(Random.Next(2, 11))
                .Build();
            var validMaximumScore = Random.Next(1, 101);
            var validNumberOfTestsAlreadyGreenAtStart = Random.Next(0, assignment.Tests.Count);

            var evaluation = new AssignmentEvaluation(validExamPartId, assignment, 
                validMaximumScore, validNumberOfTestsAlreadyGreenAtStart);

            Assert.That(evaluation, Is.Not.Null);
            Assert.That(evaluation.Id, Is.EqualTo(0));
            Assert.That(evaluation.ExamPartId, Is.EqualTo(validExamPartId));
            Assert.That(evaluation.AssignmentId, Is.EqualTo(assignment.Id));
            Assert.That(evaluation.MaximumScore, Is.EqualTo(validMaximumScore));
            Assert.That(evaluation.NumberOfTestsAlreadyGreenAtStart, Is.EqualTo(validNumberOfTestsAlreadyGreenAtStart));
        }

        [Test]
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
                .WithRandomTests(Random.Next(2, 11))
                .Build();
            Assert.That(() => new AssignmentEvaluation(
                examPartId, 
                validAssignment, 
                maximumScore, 
                numberOfTestsAlreadyGreenAtStart), 
                Throws.InstanceOf<ContractException>());
        }

        [Test]
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

        [Test]
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

        [Test]
        public void CalculateScore_ShouldReturnAnAssignmentEvaluationScoreBasedOnTheNumberOfPassingTests()
        {
            //Arrange
            int numberOfTests = Random.Next(5, 21);
            var assignment = new AssignmentBuilder().WithRandomTests(numberOfTests).Build();
            var assignmentEvaluation = new AssignmentEvaluationBuilder().WithId().WithAssignment(assignment).Build();

            var assignmentResultMock = new Mock<IAssignmentResult>();
            int numberOfPassingTests = Random.Next(0, numberOfTests + 1);
            assignmentResultMock.SetupGet(assignmentResult => assignmentResult.NumberOfPassingTests)
                .Returns(numberOfPassingTests);
            
            //Act
            IAssignmentEvaluationScore score = assignmentEvaluation.CalculateScore(assignmentResultMock.Object);

            //Assert
            Assert.That(score.NumberOfPassedTests, Is.EqualTo(numberOfPassingTests));
            Assert.That(score.AssignmentEvaluationId, Is.EqualTo(assignmentEvaluation.Id));
        }

    }
}