using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class ChapterServiceTests
    {
        private ChapterService _service;

        private Mock<IChapterRepository> _chapterRepositoryMock;
        private Mock<IPeriodRepository> _periodRepositoryMock;
        private Mock<ICourseRepository> _courseRepositoryMock;
        private Mock<IAssignmentService> _assignmentServiceMock;

        [SetUp]
        public void Setup()
        {
            _chapterRepositoryMock = new Mock<IChapterRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _assignmentServiceMock = new Mock<IAssignmentService>();

            _service = new ChapterService(_chapterRepositoryMock.Object,
                null,
                _courseRepositoryMock.Object, 
                _periodRepositoryMock.Object,
                _assignmentServiceMock.Object);
        }

        [Test]
        public void GetChaptersOfCourseAsyncShouldReturnEmptyListWhenNotCurrentPeriodIsFound()
        {
            //Arrange
            int courseId = Random.Shared.NextPositive();
            int? periodId = Random.Shared.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(It.IsAny<int?>())).Throws<DataNotFoundException>();

            //Act
            var result = _service.GetChaptersOfCourseAsync(courseId, periodId).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(periodId), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetChaptersOfCourseAsyncShouldRetrieveChaptersFromRepository()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            int courseId = Random.Shared.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingPeriod.Id)).ReturnsAsync(existingPeriod);

            var chaptersOfCourse = new List<Chapter>();
            _chapterRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(chaptersOfCourse);

            //Act
            var result = _service.GetChaptersOfCourseAsync(courseId, existingPeriod.Id).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetByCourseIdAsync(courseId, existingPeriod.Id), Times.Once);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetOrCreateChapterAsyncShouldReturnChapterIfItExistsForTheCurrentPeriod()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            string courseCode = Guid.NewGuid().ToString();
            Chapter existingChapter = new ChapterBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingPeriod.Id)).ReturnsAsync(existingPeriod);

            _chapterRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            //Act
            Chapter result = _service.GetOrCreateChapterAsync(courseCode, existingChapter.Code, existingPeriod.Id).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingChapter.Code, existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Chapter>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingChapter));
        }

        [Test]
        public void GetOrCreateChapterAsync_ShouldThrowDataNotFoundExceptionWhenNoCurrentPeriodIsFound()
        {
            //Arrange
            string courseCode = Guid.NewGuid().ToString();
            Chapter existingChapter = new ChapterBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(It.IsAny<int?>())).Throws<DataNotFoundException>();

            //Act + Assert
            Assert.That(() => _service.GetOrCreateChapterAsync(courseCode, existingChapter.Code, null).Result,
                Throws.InstanceOf<AggregateException>().With.Matches((AggregateException ex) =>
                    ex.InnerExceptions.OfType<DataNotFoundException>().Any()));
        }

        [Test]
        public void GetOrCreateChapterAsyncShouldCreateChapterIfItDoesNotExist()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(existingPeriod.Id)).ReturnsAsync(existingPeriod);

            Course existingCourse = new CourseBuilder().WithId().Build();

            _courseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>())).ReturnsAsync(existingCourse);

            _chapterRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            Chapter addedChapter = new ChapterBuilder().WithId().Build();

            _chapterRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Chapter>())).ReturnsAsync(addedChapter);


            //Act
            Chapter result = _service.GetOrCreateChapterAsync(existingCourse.Code, addedChapter.Code, existingPeriod.Id).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code, addedChapter.Code, existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(
                repo => repo.AddAsync(It.Is<Chapter>(ch =>
                    ch.PeriodId == existingPeriod.Id && ch.CourseId == existingCourse.Id &&
                    ch.Code == addedChapter.Code)), Times.Once);

            _courseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code), Times.Once);

            Assert.That(result, Is.EqualTo(addedChapter));
        }

        [Test]
        public void LoadChapterAsync_ShouldLoadTheChapterWithItsAssignments()
        {
            //Arrange
            var existingPeriod = new PeriodBuilder().WithId().Build();
            Chapter existingChapter = new ChapterBuilder().WithId()
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);

            _chapterRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            //Act
            Chapter chapter = _service.LoadChapterAsync(existingChapter.CourseId, existingChapter.Code, null).Result;

            //Assert
            Assert.That(chapter, Is.EqualTo(existingChapter));
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(null), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.LoadWithAssignmentsAsync(existingChapter.CourseId, existingChapter.Code, existingPeriod.Id), Times.Once);
        }

        [Test]
        public void LoadChapterWithTestsAsync_ShouldLoadTheChapterItsAssignmentsAndItsTests()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();
            var courseCode = Guid.NewGuid().ToString();
            var existingChapter = new ChapterBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);

            _chapterRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTestsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            //Act
            var result = _service.LoadChapterWithTestsAsync(existingChapter.CourseId, existingChapter.Code).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetPeriodAsync(null), Times.Once);

            _chapterRepositoryMock.Verify();

            _chapterRepositoryMock.Verify(repo => repo.LoadWithAssignmentsAndTestsAsync(existingChapter.CourseId, existingChapter.Code, existingPeriod.Id), Times.Once);

            Assert.That(result, Is.EqualTo(existingChapter));
        }

        [Test]
        public void LoadChapterWithTestsAsync_ShouldThrowDataNotFoundExceptionWhenChapterDoesNotExists()
        {
            //Arrange
            Period existingPeriod = new PeriodBuilder().WithId().Build();

            _periodRepositoryMock.Setup(repo => repo.GetPeriodAsync(null)).ReturnsAsync(existingPeriod);
            _chapterRepositoryMock.Setup(repo => repo.LoadWithAssignmentsAndTestsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var courseId = Random.Shared.NextPositive();
            var chapterCode = Guid.NewGuid().ToString();

            //Act
            Assert.That(() => _service.LoadChapterWithTestsAsync(courseId, chapterCode),
                Throws.InstanceOf<DataNotFoundException>());
        }

        [Test]
        public void GetResultsForUserAsync_ShouldRetrieveLastTestsResultsOfUserForEachAssignment()
        {
            //Arrange
            int userId = Random.Shared.NextPositive();
            int numberOfAssignments = Random.Shared.Next(3, 10);
            Chapter chapter = new ChapterBuilder().WithId().WithAssignments(numberOfAssignments, 1).Build();

            AssignmentResultDto assignmentResultDto = new AssignmentResultDto();
            _assignmentServiceMock
                .Setup(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(assignmentResultDto);

            //Act
            IReadOnlyList<AssignmentResultDto> results = _service.GetResultsForUserAsync(chapter, userId, null).Result;

            //Assert
            Assert.That(results, Has.Count.EqualTo(numberOfAssignments));
            _assignmentServiceMock.Verify(
                service => service.GetResultsForUserAsync(
                    It.Is<int>(assignmentId => chapter.Assignments.Any(e => e.Id == assignmentId)), userId, null),
                Times.Exactly(numberOfAssignments));
        }

        [Test]
        public void GetChapterStatisticsAsyncShouldRetrieveTestResultsForEachAssignmentAndConvertThemToStatistics()
        {
            //Arrange
            var numberOfAssignments = Random.Shared.Next(2, 5);
            var chapter = new ChapterBuilder().WithId().WithAssignments(numberOfAssignments, 1).Build();
            var nowUtc = DateTime.UtcNow;

            var assignmentStatisticsDto = new AssignmentStatisticsDto();
            _assignmentServiceMock
                .Setup(service => service.GetAssignmentUserStatisticsAsync(It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(assignmentStatisticsDto);

            //Act
            var results = _service.GetChapterStatisticsAsync(chapter, nowUtc).Result;

            //Assert
            Assert.That(results, Has.Count.EqualTo(numberOfAssignments));
            _assignmentServiceMock.Verify(
                service => service.GetAssignmentUserStatisticsAsync(
                    It.Is<int>(assignmentId => chapter.Assignments.Any(e => e.Id == assignmentId)), nowUtc),
                Times.Exactly(numberOfAssignments));
        }
    }
}