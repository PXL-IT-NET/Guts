using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.ExamAggregate
{
    [TestFixture]
    public class ExamTests
    {
        private Exam.Factory _factory;
        private Exam _existingExam;

        [SetUp]
        public void SetUp()
        {
            _factory = new Exam.Factory();
            var examId = Random.Shared.NextPositive();
            _existingExam = new ExamBuilder().WithId(examId).Build();
        }

        [Test]
        public void Factory_CreateNew_ShouldConstructExamCorrectly()
        {
            var validCourseId = Random.Shared.NextPositive();
            var validPeriodId = Random.Shared.NextPositive();
            var validName = Random.Shared.NextString();
            var exam = _factory.CreateNew(validCourseId, validPeriodId, validName);

            Assert.That(exam, Is.Not.Null);
            Assert.That(exam.Id, Is.EqualTo(0));
            Assert.That(exam.CourseId, Is.EqualTo(validCourseId));
            Assert.That(exam.Name, Is.EqualTo(validName));
            Assert.That(exam.PeriodId, Is.EqualTo(validPeriodId));
            Assert.That(exam.MaximumScore, Is.EqualTo(20));
            Assert.That(exam.Parts, Is.Empty);
        }

        [TestCase(0, 1, "someName")] //invalid course
        [TestCase(-1, 1, "someName")]
        [TestCase(1, 1, null)] //invalid name
        [TestCase(1, 1, "")]
        [TestCase(1, 0, "someName")] //invalid period
        [TestCase(1, -1, "someName")]
        public void Factory_CreateNew_ShouldThrowContractExceptionOnInvalidInput(int courseId, int periodId, string name)
        {
            Assert.That(() => _factory.CreateNew(courseId, periodId, name), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void AddExamPart_ShouldCorrectlyCreateAndAddAnExamPart()
        {
            var validName = Random.Shared.NextString();
            var validDeadline = DateTime.UtcNow.AddDays(1);

            var addedPart = _existingExam.AddExamPart(validName, validDeadline);

            Assert.That(addedPart, Is.Not.Null);
            Assert.That(addedPart, Is.SameAs(_existingExam.Parts.FirstOrDefault()));
            Assert.That(addedPart.ExamId, Is.EqualTo(_existingExam.Id));
            Assert.That(addedPart.Name, Is.EqualTo(validName));
            Assert.That(addedPart.Deadline, Is.EqualTo(validDeadline));
        }

        [Test]
        public void DeleteExamPart_ShouldRemoveAnExistingExamPart()
        {
            //Arrange
            var examPartToKeep = new ExamPartBuilder().WithId().Build();
            var examPartToDelete = new ExamPartBuilder().WithId().Build();
            var exam = new ExamBuilder()
                .WithExamPart(examPartToKeep)
                .WithExamPart(examPartToDelete)
                .Build();

            //Act
            exam.DeleteExamPart(examPartToDelete.Id);

            //Assert
            Assert.That(exam.Parts, Has.Count.EqualTo(1));
            Assert.That(exam.Parts, Has.One.EqualTo(examPartToKeep));
        }

        [Test]
        public void DeleteExamPart_ShouldThrowContractExceptionWhenPartDoesNotExist()
        {
            //Arrange
            var examPart = new ExamPartBuilder().WithId().Build();
            var exam = new ExamBuilder()
                .WithExamPart(examPart)
                .Build();
            int nonExistingExamPartId = examPart.Id + 1;

            //Act + Assert
            Assert.That(() =>exam.DeleteExamPart(nonExistingExamPartId), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CalculateScoreForUser_ShouldAggregateExamPartScoresForUser()
        {
            //Arrange
            User user = new UserBuilder().WithId().Build();
            var examBuilder = new ExamBuilder();

            var examTestResultCollectionMock = new Mock<IExamTestResultCollection>();
            var verifyExamPartActions = new List<Action<int, IExamScore>>();
            for (int i = 0; i < Random.Shared.Next(2,10); i++)
            {
                verifyExamPartActions.Add(AddExamPartMockToExam(examBuilder, examTestResultCollectionMock));
            }
            
            var exam = examBuilder.Build();

            ////Act
            var examScore = exam.CalculateScoreForUser(user, examTestResultCollectionMock.Object);

            //Assert
            Assert.That(examScore, Is.Not.Null);
            foreach (var verifyExamPartAction in verifyExamPartActions)
            {
                verifyExamPartAction(user.Id, examScore);
            }
        }

        private Action<int, IExamScore> AddExamPartMockToExam(ExamBuilder examBuilder,
            Mock<IExamTestResultCollection> examTestResultCollectionMock)
        {
            var examPartScoreMock = new Mock<IExamPartScore>();
            IExamPartScore examPartScore = examPartScoreMock.Object;

            var examPartMock = new Mock<IExamPart>();
            var examPartId = Random.Shared.NextPositive();
            examPartMock.SetupGet(part => part.Id).Returns(examPartId);
            examPartMock
                .Setup(part => part.CalculateScoreForUser(It.IsAny<int>(), It.IsAny<IExamPartTestResultCollection>()))
                .Returns(examPartScoreMock.Object);

            var examPartTestResultCollectionMock = new Mock<IExamPartTestResultCollection>();
            examTestResultCollectionMock.Setup(examTestResults => examTestResults.GetExamPartResults(examPartId))
                .Returns(examPartTestResultCollectionMock.Object);

            examBuilder.WithExamPart(examPartMock.Object);

            void VerifyExamPartUsageForUserIdAndScore(int userId, IExamScore examScore)
            {
                examPartMock.Verify(part => part.CalculateScoreForUser(userId, examPartTestResultCollectionMock.Object));
                Assert.That(examScore.ExamPartScores, Has.One.EqualTo(examPartScore));
            }

            return VerifyExamPartUsageForUserIdAndScore;
        }
    }
}