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
        private Mock<ITestResultConverter> _testResultConverterMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();

            _chapterRepositoryMock = new Mock<IChapterRepository>();
            _periodRepositoryMock = new Mock<IPeriodRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _testResultConverterMock = new Mock<ITestResultConverter>();

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
        public void GetChaptersOfCourseAsyncShouldSortChaptersByNumber()
        {
            //Arrange
            var existingPeriod = new Period { Id = _random.NextPositive() };
            var courseId = _random.NextPositive();
            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            var chaptersOfCourse = new List<Chapter>
            {
                new ChapterBuilder().WithNumber(2).Build(),
                new ChapterBuilder().WithNumber(1).Build()
            };
            _chapterRepositoryMock.Setup(repo => repo.GetByCourseIdAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(chaptersOfCourse);

            //Act
            var results = _service.GetChaptersOfCourseAsync(courseId).Result;

            //Assert
            Assert.That(results.Count, Is.EqualTo(chaptersOfCourse.Count));
            Assert.That(results.ElementAt(0).Number, Is.LessThan(results.ElementAt(1).Number));
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

            _chapterRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            //Act
            var result = _service.GetOrCreateChapterAsync(courseCode, existingChapter.Number).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetSingleAsync(courseCode, existingChapter.Number, existingPeriod.Id), Times.Once);
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
            Assert.That(() => _service.GetOrCreateChapterAsync(courseCode, existingChapter.Number).Result,
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

            _chapterRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var addedChapter = new ChapterBuilder().WithId().Build();

            _chapterRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Chapter>())).ReturnsAsync(addedChapter);


            //Act
            var result = _service.GetOrCreateChapterAsync(existingCourse.Code, addedChapter.Number).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);
            _chapterRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code, addedChapter.Number, existingPeriod.Id), Times.Once);
            _chapterRepositoryMock.Verify(
                repo => repo.AddAsync(It.Is<Chapter>(ch =>
                    ch.PeriodId == existingPeriod.Id && ch.CourseId == existingCourse.Id &&
                    ch.Number == addedChapter.Number)), Times.Once);

            _courseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingCourse.Code), Times.Once);

            Assert.That(result, Is.EqualTo(addedChapter));
        }

        [Test]
        public void LoadChapterWithTestsAsyncShouldLoadTheChapterItsExercisesAndItsTests()
        {
            //Arrange
            var existingPeriod = new Period();
            var courseCode = Guid.NewGuid().ToString();
            var existingChapter = new ChapterBuilder().WithId()
                .WithCourse(courseCode)
                .WithPeriod(existingPeriod).Build();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);

            _chapterRepositoryMock.Setup(repo => repo.LoadWithExercisesAndTestsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            //Act
            var result = _service.LoadChapterWithTestsAsync(existingChapter.CourseId, existingChapter.Number).Result;

            //Assert
            _periodRepositoryMock.Verify(repo => repo.GetCurrentPeriodAsync(), Times.Once);

            _chapterRepositoryMock.Verify();

            _chapterRepositoryMock.Verify(repo => repo.LoadWithExercisesAndTestsAsync(existingChapter.CourseId, existingChapter.Number, existingPeriod.Id), Times.Once);

            Assert.That(result, Is.EqualTo(existingChapter));
        }

        [Test]
        public void LoadChapterWithTestsAsyncShouldThrowDataNotFoundExeptionWhenChapterDoesNotExists()
        {
            //Arrange
            var existingPeriod = new Period();

            _periodRepositoryMock.Setup(repo => repo.GetCurrentPeriodAsync()).ReturnsAsync(existingPeriod);
            _chapterRepositoryMock.Setup(repo => repo.LoadWithExercisesAndTestsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var courseId = _random.NextPositive();
            var chapterNumber = _random.NextPositive();

            //Act
            Assert.That(() => _service.LoadChapterWithTestsAsync(courseId, chapterNumber),
                Throws.InstanceOf<DataNotFoundException>());
        }

        [Test]
        public void GetResultsForUserAsyncShouldRetrieveLastTestsResultsForUserAndConvertThemToExerciseResultDtos()
        {
            //Arrange
            var chapterId = _random.NextPositive();
            var userId = _random.NextPositive();
            IList<TestWithLastUserResults> existingLastTestResults = new List<TestWithLastUserResults>();

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfChapterAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(existingLastTestResults);

            var exerciseResultDtos = new List<ExerciseResultDto>();

            _testResultConverterMock
                .Setup(converter => converter.ToExerciseResultDto(It.IsAny<IList<TestWithLastUserResults>>()))
                .Returns(exerciseResultDtos);

            //Act
            var results = _service.GetResultsForUserAsync(chapterId, userId).Result;

            //Assert
            Assert.That(results, Is.EqualTo(exerciseResultDtos));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfChapterAsync(chapterId, userId), Times.Once);
            _testResultConverterMock.Verify(converter => converter.ToExerciseResultDto(existingLastTestResults), Times.Once);
        }

        [Test]
        public void GetAverageResultsAsyncShouldRetrieveLastTestsResultsForAllUsersAndConvertThemToExerciseResultDtos()
        {
            //Arrange
            var chapterId = _random.NextPositive();
            IList<TestWithLastUserResults> existingLastTestResults = new List<TestWithLastUserResults>();

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfChapterAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(existingLastTestResults);

            var exerciseResultDtos = new List<ExerciseResultDto>();

            _testResultConverterMock
                .Setup(converter => converter.ToExerciseResultDto(It.IsAny<IList<TestWithLastUserResults>>()))
                .Returns(exerciseResultDtos);

            //Act
            var results = _service.GetAverageResultsAsync(chapterId).Result;

            //Assert
            Assert.That(results, Is.EqualTo(exerciseResultDtos));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfChapterAsync(chapterId, null), Times.Once);
            _testResultConverterMock.Verify(converter => converter.ToExerciseResultDto(existingLastTestResults), Times.Once);
        }
    }
}