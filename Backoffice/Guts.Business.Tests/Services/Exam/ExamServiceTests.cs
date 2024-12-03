using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services.Exam;
using Guts.Business.Tests.Builders;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services.Exam
{
    [TestFixture]
    public class ExamServiceTests
    {
        private ExamService _service;
        private Mock<IExamRepository> _examRepositoryMock;
        private Mock<IExamFactory> _examFactoryMock;

        private Mock<IExamPartRepository> _examPartRepositoryMock;
        private Mock<IAssignmentRepository> _assignmentRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<IExamTestResultLoader> _examTestResultLoaderMock;

        [SetUp]
        public void Setup()
        {
            _examRepositoryMock = new Mock<IExamRepository>();
            _examFactoryMock = new Mock<IExamFactory>();
            _examPartRepositoryMock = new Mock<IExamPartRepository>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _examTestResultLoaderMock = new Mock<IExamTestResultLoader>();

            _service = new ExamService(_examRepositoryMock.Object,
                _examPartRepositoryMock.Object,
                _assignmentRepositoryMock.Object,
                _periodRepositoryMock.Object,
                _examFactoryMock.Object,
                _userRepositoryMock.Object,
                _examTestResultLoaderMock.Object);
        }

        [Test]
        public void CreateExamAsync_ShouldCreateAndSaveAnExam()
        {
            int courseId = Random.Shared.NextPositive();
            string name = Random.Shared.NextString();

            var existingPeriod = new Period { Id = Random.Shared.NextPositive() };
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);

            Domain.ExamAggregate.Exam createdExam = new ExamBuilder().Build();
            _examFactoryMock
                .Setup(f => f.CreateNew(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(createdExam);
            Domain.ExamAggregate.Exam savedExam = new ExamBuilder().Build();
            _examRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Domain.ExamAggregate.Exam>())).ReturnsAsync(savedExam);

            IExam result = _service.CreateExamAsync(courseId, name).Result;

            _examFactoryMock.Verify(f => f.CreateNew(courseId, existingPeriod.Id, name), Times.Once);
            _examRepositoryMock.Verify(repo => repo.AddAsync(createdExam), Times.Once);
            Assert.That(result, Is.SameAs(savedExam));
        }

        [Test]
        public void GetExamAsync_ShouldReturnExamWithPartsAndEvaluationsLoaded()
        {
            var examId = Random.Shared.NextPositive();
            Domain.ExamAggregate.Exam existingExam = new ExamBuilder().WithId(examId).Build();
            _examRepositoryMock
                .Setup(repo => repo.LoadDeepAsync(It.IsAny<int>())).ReturnsAsync(existingExam);

            IExam result = _service.GetExamAsync(examId).Result;

            _examRepositoryMock.Verify(repo => repo.LoadDeepAsync(examId), Times.Once);
            Assert.That(result, Is.SameAs(existingExam));
        }

        [Test]
        public void GetExamsAsync_ShouldFindAllExamsOfACourseForTheCurrentPeriod()
        {
            //Arrange
            Period existingPeriod = new Period { Id = Random.Shared.NextPositive() };
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);

            int courseId = Random.Shared.NextPositive();

            List<Domain.ExamAggregate.Exam> retrievedExams = new List<Domain.ExamAggregate.Exam>();
            _examRepositoryMock
                .Setup(repo => repo.FindWithPartsAndEvaluationsAsync(courseId, existingPeriod.Id)).ReturnsAsync(retrievedExams).Verifiable();

            //Act
            IReadOnlyList<IExam> results = _service.GetExamsAsync(courseId).Result;

            //Assert
            _examRepositoryMock.Verify();
            Assert.That(results, Is.SameAs(retrievedExams));
        }

        [Test]
        public void CreateExamPartAsync_ShouldCreateAndSaveAnExam()
        {
            //Arrange
            int examId = Random.Shared.NextPositive();

            ExamPartDto examPartDto = new ExamPartDto
            {
                Name = Random.Shared.NextString(),
                Deadline = Random.Shared.NextDateTimeInFuture(),
                AssignmentEvaluations = new List<AssignmentEvaluationDto>
                {
                    new AssignmentEvaluationDtoBuilder().Build(),
                    new AssignmentEvaluationDtoBuilder().Build(),
                    new AssignmentEvaluationDtoBuilder().Build()
                }
            };

            Mock<IExam> examMock = new Mock<IExam>();
            _examRepositoryMock.Setup(repo => repo.LoadDeepAsync(examId)).ReturnsAsync(examMock.Object);

            ExamPart createdExamPart = new ExamPartBuilder().Build();
            examMock.Setup(exam => exam.AddExamPart(examPartDto.Name, examPartDto.Deadline)).Returns(createdExamPart).Verifiable();

            _assignmentRepositoryMock
                .Setup(repo =>
                    repo.GetSingleWithTestsAsync( It.IsAny<int>()))
                .ReturnsAsync((int assignmentId) =>
                    new AssignmentBuilder().WithId(assignmentId).WithRandomTests(10).Build());

            _examPartRepositoryMock.Setup(repo => repo.AddAsync(createdExamPart)).ReturnsAsync(createdExamPart);

            //Act
            IExamPart result = _service.CreateExamPartAsync(examId, examPartDto).Result;

            //Assert
            examMock.Verify();
            Assert.That(result, Is.SameAs(createdExamPart));

            _assignmentRepositoryMock
                .Verify(repo =>
                        repo.GetSingleWithTestsAsync(
                            It.IsIn<int>(examPartDto.AssignmentEvaluations.Select(ae => ae.AssignmentId))),
                    Times.Exactly(examPartDto.AssignmentEvaluations.Count));

            Assert.That(result.AssignmentEvaluations, Has.Count.EqualTo(examPartDto.AssignmentEvaluations.Count));
        }

        [Test]
        public void CreateExamPartAsync_ShouldThrowContractException_WhenDtoContainsNoAssignmentEvaluations()
        {
            //Arrange
            int examId = Random.Shared.NextPositive();

            ExamPartDto examPartDto = new ExamPartDto
            {
                Name = Random.Shared.NextString(),
                Deadline = Random.Shared.NextDateTimeInFuture(),
                AssignmentEvaluations = new List<AssignmentEvaluationDto>()
            };

            //Act  +Assert
            Assert.That(() => _service.CreateExamPartAsync(examId, examPartDto), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void DeleteExamPartAsync_ShouldThrowContractException_WhenDtoContainsNoAssignmentEvaluations()
        {
            //Arrange
            int examPartId = Random.Shared.NextPositive();
            Mock<IExam> examMock = new Mock<IExam>();

            //Act 
            _service.DeleteExamPartAsync(examMock.Object, examPartId).Wait();

            //Assert
            examMock.Verify(exam => exam.DeleteExamPart(examPartId), Times.Once);
            _examPartRepositoryMock.Verify(repo => repo.DeleteByIdAsync(examPartId), Times.Once);
        }

        [Test]
        public void CalculateExamScoresForCsvAsync_ShouldCalculateTheScoreForAllUsersOfTheCourse()
        {
            //Arrange
            int examId = Random.Shared.NextPositive();
            int courseId = Random.Shared.NextPositive();
            Mock<IExam> examMock = new Mock<IExam>();
            _examRepositoryMock.Setup(repo => repo.LoadDeepAsync(examId)).ReturnsAsync(examMock.Object);

            ExamTestResultCollection examTestResultCollection = new ExamTestResultCollection();
            _examTestResultLoaderMock.Setup(loader => loader.GetExamResultsAsync(examMock.Object))
                .ReturnsAsync(examTestResultCollection);

            Mock<IExamScore> examScoreMock = new Mock<IExamScore>();
            examMock.SetupGet(exam => exam.CourseId).Returns(courseId);
            examMock.Setup(exam => exam.CalculateScoreForUser(It.IsAny<User>(), examTestResultCollection))
                .Returns(examScoreMock.Object);

            int numberOfUsers = Random.Shared.Next(2, 11);
            List<User> users = new List<User>();
            for (int i = 0; i < numberOfUsers; i++)
            {
                users.Add(new UserBuilder().Build());
            }
            _userRepositoryMock.Setup(repo => repo.GetUsersOfCourseForCurrentPeriodAsync(courseId)).ReturnsAsync(users);

            //Act
            IList<ExpandoObject> results = _service.CalculateExamScoresForCsvAsync(examId).Result;

            //Assert
            Assert.That(results, Has.Count.EqualTo(numberOfUsers));
            examMock.Verify(exam => exam.CalculateScoreForUser(It.IsIn<User>(users), examTestResultCollection), Times.Exactly(numberOfUsers));
        }
    }
}