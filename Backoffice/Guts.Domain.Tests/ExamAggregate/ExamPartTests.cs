using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    public class ExamPartTests : DomainTestBase
    {
        [Test]
        public void Constructor_ShouldConstructValidExamPart()
        {
            var validExamId = Random.NextPositive();
            var validName = Random.NextString();
            var validDeadline = DateTime.UtcNow.AddDays(1);

            var examPart = new ExamPart(validExamId, validName, validDeadline);
            
            Assert.That(examPart, Is.Not.Null);
            Assert.That(examPart.Id, Is.EqualTo(0));
            Assert.That(examPart.ExamId, Is.EqualTo(validExamId));
            Assert.That(examPart.Name, Is.EqualTo(validName));
            Assert.That(examPart.Deadline, Is.EqualTo(validDeadline));
            Assert.That(examPart.AssignmentEvaluations, Is.Empty);
        }

        [TestCase(-1, "some name")]
        [TestCase(1, null)]
        [TestCase(1, "")]
        public void Constructor_ShouldThrowContractExceptionOnInvalidInput(int examId, string name)
        {
            var validDeadline = DateTime.UtcNow.AddDays(1);
            Assert.That(() => new ExamPart(examId, name, validDeadline), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void Constructor_ShouldThrowContractExceptionWhenDeadlineIsNotUtc()
        {
            var deadline = DateTime.UtcNow.AddDays(1);
            var localDeadline = deadline.ToLocalTime();
            Assert.That(() => new ExamPart(1, "some name", localDeadline), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void AddAssignmentEvaluation_ShouldCorrectlyCreateAndAddAnEvaluation()
        {
            var existingAssignment = new AssignmentBuilder().WithId().WithRandomTests(5).Build();
            var existingExamPart = new ExamPartBuilder().WithId().Build();
            var maximumScore = Random.Next(5, 101);
            var numberOfTestsAlreadyGreenAtStart = Random.Next(1, 5);

            var addedEvaluation = existingExamPart.AddAssignmentEvaluation(existingAssignment, maximumScore,
                numberOfTestsAlreadyGreenAtStart);

            Assert.That(addedEvaluation, Is.Not.Null);
            Assert.That(addedEvaluation, Is.SameAs(existingExamPart.AssignmentEvaluations.FirstOrDefault()));
            Assert.That(addedEvaluation.AssignmentId, Is.EqualTo(existingAssignment.Id));
            Assert.That(addedEvaluation.ExamPartId, Is.EqualTo(existingExamPart.Id));
            Assert.That(addedEvaluation.MaximumScore, Is.EqualTo(maximumScore));
            Assert.That(addedEvaluation.NumberOfTestsAlreadyGreenAtStart, Is.EqualTo(numberOfTestsAlreadyGreenAtStart));
        }

        [Test]
        public void CalculateScores_ShouldAggregateAssignmentEvaluationScoresForUser()
        {
            //Arrange
            int userId = Random.NextPositive();
            var examPartBuilder = new ExamPartBuilder();

            var examPartTestResultCollectionMock = new Mock<IExamPartTestResultCollection>();
            var verifyAssignmentEvaluationActions = new List<Action<IExamPartScore>>();
            for (int i = 0; i < Random.Next(2, 10); i++)
            {
                verifyAssignmentEvaluationActions.Add(AddAssignmentEvaluationMockToExamPart(examPartBuilder, userId, examPartTestResultCollectionMock));
            }

            var examPart = examPartBuilder.Build();

            ////Act
            var examPartScore = examPart.CalculateScoreForUser(userId, examPartTestResultCollectionMock.Object);

            //Assert
            Assert.That(examPartScore, Is.Not.Null);
            foreach (var verifyExamPartAction in verifyAssignmentEvaluationActions)
            {
                verifyExamPartAction(examPartScore);
            }
        }

        private Action<IExamPartScore> AddAssignmentEvaluationMockToExamPart(ExamPartBuilder examPartBuilder, int userId,
            Mock<IExamPartTestResultCollection> examPartTestResultCollectionMock)
        {
            var assignmentEvaluationScoreMock = new Mock<IAssignmentEvaluationScore>();
            IAssignmentEvaluationScore assignmentEvaluationScore = assignmentEvaluationScoreMock.Object;

            var assignmentEvaluationMock = new Mock<IAssignmentEvaluation>();
            var assignmentId = Random.NextPositive();
            assignmentEvaluationMock.SetupGet(evaluation => evaluation.AssignmentId).Returns(assignmentId);
            assignmentEvaluationMock
                .Setup(evaluation => evaluation.CalculateScore(It.IsAny<IAssignmentResult>()))
                .Returns(assignmentEvaluationScoreMock.Object);

            var assignmentResultMock = new Mock<IAssignmentResult>();
            examPartTestResultCollectionMock.Setup(collection => collection.GetAssignmentResultFor(userId, assignmentId))
                .Returns(assignmentResultMock.Object);

            examPartBuilder.WithAssignmentEvaluation(assignmentEvaluationMock.Object);

            void VerifyAssignmentEvaluationUsage(IExamPartScore examPartScore)
            {
                assignmentEvaluationMock.Verify(assignmentEvaluation => assignmentEvaluation.CalculateScore(assignmentResultMock.Object));
                Assert.That(examPartScore.AssignmentEvaluationScores, Has.One.EqualTo(assignmentEvaluationScore));
            }

            return VerifyAssignmentEvaluationUsage;
        }
    }
}