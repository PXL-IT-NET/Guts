using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Converters;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class ChapterServiceTests
    {
        private ChapterService _service;
        private Random _random;

        private Mock<IChapterRepository> _chapterRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;
        private Mock<ICourseRepository> _courseRepositoryMock;
        private Mock<ITestResultRepository> _testResultRepositoryMock;
        private Mock<IAssignmentWitResultsConverter> _testResultConverterMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();

            _chapterRepositoryMock = new Mock<IChapterRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _testResultConverterMock = new Mock<IAssignmentWitResultsConverter>();

            _service = new ChapterService(_chapterRepositoryMock.Object, 
                _courseRepositoryMock.Object, 
                _periodRepositoryMock.Object, 
                _testResultRepositoryMock.Object,
                _testResultConverterMock.Object);
        }

        [Test]
        public void GetChaptersOfCourseAsyncShouldReturnEmptyListWhenNotCurrentPeriodIsFound()
        {
            //Arrange
            var courseId = _random.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).Throws<DataNotFoundException>();

            //Act
            var result = _service.GetChaptersOfCourseAsync(courseId).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetChaptersOfCourseAsyncShouldRetrieveChaptersFromRepository()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseId = _random.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            var chaptersOfCourse = new List<Chapter>();
            _chapterRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(chaptersOfCourse);

            //Act
            var result = _service.GetChaptersOfCourseAsync(courseId).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(courseId, existingPeriod.Id), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetChaptersOfCourseAsyncShouldSortChaptersByCode()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseId = _random.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            var chaptersOfCourse = new List<Chapter>
            {
                new ChapterBuilder().WithCode("2").Build(),
                new ChapterBuilder().WithCode("1").Build()
            };
            _chapterRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(chaptersOfCourse);

            //Act
            var results = _service.GetChaptersOfCourseAsync(courseId).Result;

            //Assert
            Assert.That(results.Count, Is.EqualTo(chaptersOfCourse.Count));
            Assert.That(results.ElementAt(0).Code, Is.LessThan(results.ElementAt(1).Code));
        }

        [Test]
        public void GetOrCreateChapterAsyncShouldReturnChapterIfItExistsForTheCurrentPeriod()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseCode = Guid.NewGuid().ToString();
            var existingChapter = new ChapterBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            _chapterRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            //Act
            var result = _service.GetOrCreateChapterAsync(courseCode, existingChapter.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingChapter.Code, existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Chapter>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingChapter));
        }

        [Test]
        public void GetOrCreateChapterAsync_ShouldThrowDataNotFoundExeptionWhenNoCurrentPeriodIsFound()
        {
            //Arrange
            var courseCode = Guid.NewGuid().ToString();
            var existingChapter = new ChapterBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).Throws<DataNotFoundException>();

            //Act + Assert
            Assert.That(() => _service.GetOrCreateChapterAsync(courseCode, existingChapter.Code).Result,
                Throws.InstanceOf<AggregateException>().With.Matches((AggregateException ex) =>
                    ex.InnerExceptions.OfType<DataNotFoundException>().Any()));
        }

        [Test]
        public void GetOrCreateChapterAsyncShouldCreateChapterIfItDoesNotExist()
        {
            //Arrange
            var existingPeriod = new Period()
            {
                Id = _random.NextPositive()
            };

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            var existingCourse = new CourseBuilder().WithId().Build();

            _courseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>())).ReturnsAsync(existingCourse);

            _chapterRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var addedChapter = new ChapterBuilder().WithId().Build();

            _chapterRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Chapter>())).ReturnsAsync(addedChapter);


            //Act
            var result = _service.GetOrCreateChapterAsync(existingCourse.Code, addedChapter.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code, addedChapter.Code, existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(
                repo => repo.AddAsync(It.Is<Chapter>(ch =>
                    ch.PeriodId == existingPeriod.Id && ch.CourseId == existingCourse.Id &&
                    ch.Code == addedChapter.Code)), Times.Once);

            _courseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code), Times.Once);

            Assert.That(result, Is.EqualTo(addedChapter));
        }

        [Test]
        public void LoadChapterWithTestsAsync_ShouldLoadTheChapterItsAssignmentsAndItsTests()
        {
            //Arrange
            var existingPeriod = new Period();
            var courseCode = Guid.NewGuid().ToString();
            var existingChapter = new ChapterBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            _chapterRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTestsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            //Act
            var result = _service.LoadChapterWithTestsAsync(existingChapter.CourseId, existingChapter.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);

            _chapterRepositoryMock.Verify();

            _chapterRepositoryMock.Verify(repo => repo.LoadWithAssignmentsAndTestsAsync(existingChapter.CourseId, existingChapter.Code, existingPeriod.Id), Times.Once);

            Assert.That(result, Is.EqualTo(existingChapter));
        }

        [Test]
        public void LoadChapterWithTestsAsyncShouldThrowDataNotFoundExeptionWhenChapterDoesNotExists()
        {
            //Arrange
            var existingPeriod = new Period();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _chapterRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTestsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var courseId = _random.NextPositive();
            var chapterCode = Guid.NewGuid().ToString();

            //Act
            Assert.That(() => _service.LoadChapterWithTestsAsync(courseId, chapterCode),
                Throws.InstanceOf<DataNotFoundException>());
        }

        [Test]
        public void GetResultsForUserAsyncShouldRetrieveLastTestsResultsOfUserForEachAssignment()
        {
            //Arrange
            var userId = _random.NextPositive();
            var numberOfAssignments = _random.Next(3, 10);
            var chapter = new ChapterBuilder().WithId().WithAssignments(numberOfAssignments, 1).Build();
            var lastTestResults = new List<TestResult> {new TestResult()};

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfUser(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(lastTestResults);

            //Act
            var results = _service.GetResultsForUserAsync(chapter, userId, null).Result;

            //Assert
            Assert.That(results, Has.Count.EqualTo(numberOfAssignments));
            _testResultRepositoryMock.Verify(
                repo => repo.GetLastTestResultsOfUser(
                    It.Is<int>(assignmentId => chapter.Assignments.Any(e => e.Id == assignmentId)), userId, null),
                Times.Exactly(numberOfAssignments));
            Assert.That(results, Has.All.Matches((AssignmentResultDto dto) => dto.TestResults.Count == lastTestResults.Count));
        }

        [Test]
        public void GetChapterStatisticsAsyncShouldRetrieveTestResultsForEachAssignmentAndConvertThemToStatistics()
        {
            //Arrange
            var numberOfAssignments = _random.Next(2, 5);
            var chapter = new ChapterBuilder().WithId().WithAssignments(numberOfAssignments, 1).Build();
            var nowUtc = DateTime.UtcNow;

            var testResults = new List<TestResult>();
            _testResultRepositoryMock
                .Setup(repo => repo.GetLastTestResultsOfAllUsers(It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(testResults);

            var assignmentStatisticsDto = new AssignmentStatisticsDto();
            _testResultConverterMock
                .Setup(converter => converter.ToAssignmentStatisticsDto(It.IsAny<int>(), It.IsAny<IList<TestResult>>()))
                .Returns(assignmentStatisticsDto);

            //Act
            var results = _service.GetChapterStatisticsAsync(chapter, nowUtc).Result;

            //Assert
            Assert.That(results, Has.Count.EqualTo(numberOfAssignments));
            _testResultRepositoryMock.Verify(
                repo => repo.GetLastTestResultsOfAllUsers(
                    It.Is<int>(assignmentId => chapter.Assignments.Any(e => e.Id == assignmentId)), nowUtc),
                Times.Exactly(numberOfAssignments));
            _testResultConverterMock.Verify(
                converter =>
                    converter.ToAssignmentStatisticsDto(
                        It.Is<int>(assignmentId => chapter.Assignments.Any(e => e.Id == assignmentId)), testResults),
                Times.Exactly(numberOfAssignments));
        }
    }
}