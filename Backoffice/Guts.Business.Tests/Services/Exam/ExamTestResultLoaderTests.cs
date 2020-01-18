using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Repositories;
using Guts.Business.Services.Exam;
using Guts.Domain.ExamAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services.Exam
{
    [TestFixture]
    public class ExamTestResultLoaderTests
    {
        private ExamTestResultLoader _loader;

        private Random _random;
        private Mock<ITestResultRepository> _testResultRepositoryMock;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _random = new Random();
        }

        [SetUp]
        public void Setup()
        {
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _loader = new ExamTestResultLoader(_testResultRepositoryMock.Object);
        }

        [Test]
        public void GetExamResultsAsync_ShouldCollectResultsForAllAssignmentsOfEachExamPart()
        {
            //Arrange
            var examMock = new Mock<IExam>();

            var assignmentEvaluation1 = new AssignmentEvaluationBuilder().Build();
            var assignmentEvaluation2 = new AssignmentEvaluationBuilder().WithId().Build();
            var assignmentEvaluation3 = new AssignmentEvaluationBuilder().WithId().Build();

            var examPart1 = new ExamPartBuilder()
                .WithId()
                .WithAssignmentEvaluation(assignmentEvaluation1)
                .WithAssignmentEvaluation(assignmentEvaluation2)
                .Build();
            int[] examPart1AssignmentIds = new int[]{assignmentEvaluation1.AssignmentId, assignmentEvaluation2.AssignmentId};

            var examPart2 = new ExamPartBuilder()
                .WithId()
                .WithAssignmentEvaluation(assignmentEvaluation3)
                .Build();
            int[] examPart2AssignmentIds = new int[] { assignmentEvaluation3.AssignmentId};

            var examParts = new List<IExamPart>{examPart1, examPart2};

            examMock.SetupGet(exam => exam.Parts).Returns(examParts);

            _testResultRepositoryMock
                .Setup(repo => repo.GetLastTestResultsOfAssignmentsAsync(It.IsAny<int[]>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<TestResult>());

            //Act
            var result = _loader.GetExamResultsAsync(examMock.Object).Result;

            //Assert
            Assert.That(result, Is.Not.Null);
            _testResultRepositoryMock
                .Verify(
                    repo => repo.GetLastTestResultsOfAssignmentsAsync(
                        It.Is<int[]>(assignmentIds =>
                            assignmentIds.Intersect(examPart1AssignmentIds).Count() == assignmentIds.Length),
                        examPart1.Deadline), Times.Once);

            _testResultRepositoryMock
                .Verify(
                    repo => repo.GetLastTestResultsOfAssignmentsAsync(
                        It.Is<int[]>(assignmentIds =>
                            assignmentIds.Intersect(examPart2AssignmentIds).Count() == assignmentIds.Length),
                        examPart2.Deadline), Times.Once);
        }
    }
}