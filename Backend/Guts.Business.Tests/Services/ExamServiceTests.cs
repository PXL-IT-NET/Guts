using System;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Guts.Domain.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class ExamServiceTests
    {
        private ExamService _service;
        private Mock<IExamRepository> _examRepositoryMock;
        private Mock<IExamFactory> _examFactoryMock;

        private Random _random;
        private Mock<IExamPartRepository> _examPartRepositoryMock;
        private Mock<IAssignmentRepository> _assignmentRepositoryMock;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _random = new Random();
        }

        [SetUp]
        public void Setup()
        {
            _examRepositoryMock = new Mock<IExamRepository>();
            _examFactoryMock = new Mock<IExamFactory>();
            _examPartRepositoryMock = new Mock<IExamPartRepository>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
            _service = new ExamService(_examRepositoryMock.Object,
                _examPartRepositoryMock.Object,
                _assignmentRepositoryMock.Object,
                _examFactoryMock.Object);
        }

        [Test]
        public void CreateExamAsync_ShouldCreateAndSaveAnExam()
        {
            var courseId = _random.NextPositive();
            var name = _random.NextString();
            var createdExam = new ExamBuilder().Build();
            _examFactoryMock
                .Setup(f => f.CreateNew(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(createdExam);
            var savedExam = new ExamBuilder().Build();
            _examRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Exam>())).ReturnsAsync(savedExam);

            var result = _service.CreateExamAsync(courseId, name).Result;

            _examFactoryMock.Verify(f => f.CreateNew(courseId, name), Times.Once);
            _examRepositoryMock.Verify(repo => repo.AddAsync(createdExam), Times.Once);
            Assert.That(result, Is.SameAs(savedExam));
        }

        [Test]
        public void GetExamAsync_ShouldReturnExamWithPartsAndEvaluationsLoaded()
        {
            var examId = _random.NextPositive();
            var existingExam = new ExamBuilder().WithId(examId).Build();
            _examRepositoryMock
                .Setup(repo => repo.LoadWithPartsAndEvaluations(It.IsAny<int>())).ReturnsAsync(existingExam);

            var result = _service.GetExamAsync(examId).Result;

            _examRepositoryMock.Verify(repo => repo.LoadWithPartsAndEvaluations(examId), Times.Once);
            Assert.That(result, Is.SameAs(existingExam));
        }
    }
}