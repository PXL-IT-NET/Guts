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
    public class ExerciseServiceTests
    {
        private ExerciseService _service;
        private Random _random;
        private Mock<IExerciseRepository> _exerciseRepositoryMock;
        private Mock<ITestRepository> _testRepositoryMock;
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<ITestResultRepository> _testResultRepositoryMock;
        private Mock<ITestResultConverter> _testResultConverterMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _exerciseRepositoryMock = new Mock<IExerciseRepository>();
            _testRepositoryMock = new Mock<ITestRepository>();
            _chapterServiceMock = new Mock<IChapterService>();
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _testResultConverterMock = new Mock<ITestResultConverter>();

            _service = new ExerciseService(_exerciseRepositoryMock.Object, 
                _chapterServiceMock.Object, 
                _testRepositoryMock.Object,
                _testResultRepositoryMock.Object,
                _testResultConverterMock.Object);
        }

        [Test]
        public void GetOrCreateExerciseAsync_ShouldReturnExerciseIfItExists()
        {
            //Arrange
            var exerciseDto = new ExerciseDtoBuilder().Build();
            var existingChapter = new Chapter
            {
                Id = _random.NextPositive(),
                Number = exerciseDto.ChapterNumber
            };

            _chapterServiceMock.Setup(repo => repo.GetOrCreateChapterAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(existingChapter);

            var existingExercise = new Exercise
            {
                Id = _random.NextPositive(),
                Number = exerciseDto.ExerciseNumber
            };

            _exerciseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(existingExercise);

            //Act
            var result = _service.GetOrCreateExerciseAsync(exerciseDto).Result;

            //Assert
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, exerciseDto.ExerciseNumber), Times.Once());
            _exerciseRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Exercise>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingExercise));
        }

        [Test]
        public void GetOrCreateExerciseAsync_ShouldCreateExerciseIfItDoesNotExist()
        {
            //Arrange
            var exerciseDto = new ExerciseDtoBuilder().Build();

            var existingChapter = new Chapter
            {
                Id = _random.NextPositive(),
                Number = exerciseDto.ChapterNumber
            };

            _chapterServiceMock.Setup(repo => repo.GetOrCreateChapterAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            var addedExercise = new Exercise
            {
                Id = _random.NextPositive(),
                Number = exerciseDto.ChapterNumber
            };

            _exerciseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<int>())).Throws<DataNotFoundException>();
            _exerciseRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Exercise>())).ReturnsAsync(addedExercise);

            //Act
            var result = _service.GetOrCreateExerciseAsync(exerciseDto).Result;

            //Assert
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, exerciseDto.ExerciseNumber), Times.Once());
            _exerciseRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Exercise>()), Times.Once);
            Assert.That(result, Is.EqualTo(addedExercise));
        }

        [Test]
        public void LoadOrCreateTestsForExerciseAsync_ShouldCreateNonExistingTests()
        {
            //Arrange
            var testNames = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var exercise = new Exercise
            {
                Id = _random.NextPositive(),
            };

            _testRepositoryMock.Setup(repo => repo.FindByExercise(It.IsAny<int>())).ReturnsAsync(new List<Test>());
            _testRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Test>())).ReturnsAsync((Test test) =>
            {
                test.Id = _random.NextPositive();
                return test;
            });

            //Act
            _service.LoadOrCreateTestsForExerciseAsync(exercise, testNames).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByExercise(exercise.Id), Times.Once);
            _testRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Test>(test => testNames.Contains(test.TestName))), Times.Exactly(testNames.Count));
            Assert.That(exercise.Tests, Is.Not.Null);
            Assert.That(exercise.Tests.Count, Is.EqualTo(testNames.Count));
            Assert.That(exercise.Tests, Has.All.Matches<Test>(test => test.Id > 0) );
            Assert.That(exercise.Tests, Has.All.Matches<Test>(test => testNames.Contains(test.TestName)));

        }

        [Test]
        public void LoadOrCreateTestsForExerciseAsync_ShouldLoadExistingTests()
        {
            //Arrange
            var testNames = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var existingTests = testNames.Select(name => new Test
            {
                Id = _random.NextPositive(),
                TestName = name
            }).ToList();

            var exercise = new Exercise
            {
                Id = _random.NextPositive(),
            };

            _testRepositoryMock.Setup(repo => repo.FindByExercise(It.IsAny<int>())).ReturnsAsync(existingTests);

            //Act
            _service.LoadOrCreateTestsForExerciseAsync(exercise, testNames).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByExercise(exercise.Id), Times.Once);
            _testRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Test>()), Times.Never);
            Assert.That(exercise.Tests, Is.Not.Null);
            Assert.That(exercise.Tests, Is.EquivalentTo(existingTests));
        }

        [Test]
        public void GetResultsForUserAsyncShouldRetrieveLastTestsResultsForUserAndConvertThemToExerciseResultDtos()
        {
            //Arrange
            var exerciseId = _random.NextPositive();
            var userId = _random.NextPositive();
            IList<TestWithLastUserResults> existingLastTestResults = new List<TestWithLastUserResults>();

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfExerciseAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingLastTestResults);

            var exerciseResultDto = new ExerciseResultDto();
            var exerciseResultDtos = new List<ExerciseResultDto> {exerciseResultDto};

            _testResultConverterMock
                .Setup(converter => converter.ToExerciseResultDto(It.IsAny<IList<TestWithLastUserResults>>()))
                .Returns(exerciseResultDtos);

            //Act
            var result = _service.GetResultsForUserAsync(exerciseId, userId).Result;

            //Assert
            Assert.That(result, Is.EqualTo(exerciseResultDto));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfExerciseAsync(exerciseId, userId), Times.Once);
            _testResultConverterMock.Verify(converter => converter.ToExerciseResultDto(existingLastTestResults), Times.Once);
        }
    }
}