using System;
using System.Collections.Generic;
using System.Linq;
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
    public class AssignmentServiceTests
    {
        private AssignmentService _service;
        private Random _random;
        private Mock<IExerciseRepository> _exerciseRepositoryMock;
        private Mock<ITestRepository> _testRepositoryMock;
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<ITestResultRepository> _testResultRepositoryMock;
        private Mock<ITestRunRepository> _testRunRepositoryMock;
        private Mock<IProjectService> _projectServiceMock;
        private Mock<IProjectComponentRepository> _projectComponentRepositoryMock;
        private Mock<IAssignmentRepository> _assignmentRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _exerciseRepositoryMock = new Mock<IExerciseRepository>();
            _testRepositoryMock = new Mock<ITestRepository>();
            _chapterServiceMock = new Mock<IChapterService>();
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _testRunRepositoryMock = new Mock<ITestRunRepository>();
            _projectServiceMock = new Mock<IProjectService>();
            _projectComponentRepositoryMock = new Mock<IProjectComponentRepository>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();

            _service = new AssignmentService(_exerciseRepositoryMock.Object, 
                _chapterServiceMock.Object, 
                _projectServiceMock.Object,
                _projectComponentRepositoryMock.Object,
                _assignmentRepositoryMock.Object,
                _testRepositoryMock.Object,
                _testResultRepositoryMock.Object,
                _testRunRepositoryMock.Object);
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
                Code = exerciseDto.ExerciseCode
            };

            _exerciseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(existingExercise);

            //Act
            var result = _service.GetOrCreateExerciseAsync(exerciseDto).Result;

            //Assert
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, existingExercise.Code), Times.Once());
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

            _chapterServiceMock.Setup(service => service.GetOrCreateChapterAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);

            var addedExercise = new Exercise
            {
                Id = _random.NextPositive(),
                Code = exerciseDto.ExerciseCode
            };

            _exerciseRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).Throws<DataNotFoundException>();
            _exerciseRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Exercise>())).ReturnsAsync(addedExercise);

            //Act
            var result = _service.GetOrCreateExerciseAsync(exerciseDto).Result;

            //Assert
            _exerciseRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, addedExercise.Code), Times.Once());
            _exerciseRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Exercise>()), Times.Once);
            Assert.That(result, Is.EqualTo(addedExercise));
        }

        [Test]
        public void GetOrCreateProjectComponentAsync_ShouldReturnComponentIfItExists()
        {
            //Arrange
            var projectComponentDto = new ProjectComponentDtoBuilder().Build();
            var existingProject = new Project
            {
                Id = _random.NextPositive(),
                Code = projectComponentDto.ProjectCode
            };

            _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingProject);

            var existingComponent = new ProjectComponent
            {
                Id = _random.NextPositive(),
                Code = projectComponentDto.ComponentCode
            };

            _projectComponentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(existingComponent);

            //Act
            var result = _service.GetOrCreateProjectComponentAsync(projectComponentDto).Result;

            //Assert
            _projectComponentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.Id, existingComponent.Code), Times.Once());
            _projectComponentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ProjectComponent>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingComponent));
        }

        [Test]
        public void GetOrCreateProjectComponentAsync_ShouldCreateComponentIfItDoesNotExist()
        {
            //Arrange
            var projectComponentDto = new ProjectComponentDtoBuilder().Build();
            var existingProject = new Project
            {
                Id = _random.NextPositive(),
                Code = projectComponentDto.ProjectCode
            };

            _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingProject);

            var addedComponent = new ProjectComponent
            {
                Id = _random.NextPositive(),
                Code = projectComponentDto.ComponentCode
            };

            _projectComponentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).Throws<DataNotFoundException>();
            _projectComponentRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<ProjectComponent>())).ReturnsAsync(addedComponent);

            //Act
            var result = _service.GetOrCreateProjectComponentAsync(projectComponentDto).Result;

            //Assert
            _projectComponentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.Id, addedComponent.Code), Times.Once());
            _projectComponentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ProjectComponent>()), Times.Once);
            Assert.That(result, Is.EqualTo(addedComponent));
        }

        [Test]
        public void LoadTestsForAssignmentAsync_ShouldLoadExistingTests()
        {
            //Arrange
            var assignment = new ExerciseBuilder().WithId().Build();

            var existingTests = new List<Test>
            {
                new TestBuilder().WithId().WithAssignmentId(assignment.Id).Build(),
                new TestBuilder().WithId().WithAssignmentId(assignment.Id).Build()
            };

            _testRepositoryMock.Setup(repo => repo.FindByAssignmentId(It.IsAny<int>())).ReturnsAsync(existingTests);

            //Act
            _service.LoadTestsForAssignmentAsync(assignment).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByAssignmentId(assignment.Id), Times.Once);
            Assert.That(assignment.Tests, Is.Not.Null);
            Assert.That(assignment.Tests, Is.EquivalentTo(existingTests));
        }

        [Test]
        public void LoadOrCreateTestsForAssignmentAsync_ShouldCreateNonExistingTests()
        {
            //Arrange
            var testNames = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            var exercise = new Exercise
            {
                Id = _random.NextPositive(),
            };

            _testRepositoryMock.Setup(repo => repo.FindByAssignmentId(It.IsAny<int>())).ReturnsAsync(new List<Test>());
            _testRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Test>())).ReturnsAsync((Test test) =>
            {
                test.Id = _random.NextPositive();
                return test;
            });

            //Act
            _service.LoadOrCreateTestsForAssignmentAsync(exercise, testNames).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByAssignmentId(exercise.Id), Times.Once);
            _testRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Test>(test => testNames.Contains(test.TestName))), Times.Exactly(testNames.Count));
            Assert.That(exercise.Tests, Is.Not.Null);
            Assert.That(exercise.Tests.Count, Is.EqualTo(testNames.Count));
            Assert.That(exercise.Tests, Has.All.Matches<Test>(test => test.Id > 0) );
            Assert.That(exercise.Tests, Has.All.Matches<Test>(test => testNames.Contains(test.TestName)));

        }

        [Test]
        public void LoadOrCreateTestsForAssignmentAsync_ShouldLoadExistingTests()
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

            _testRepositoryMock.Setup(repo => repo.FindByAssignmentId(It.IsAny<int>())).ReturnsAsync(existingTests);

            //Act
            _service.LoadOrCreateTestsForAssignmentAsync(exercise, testNames).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByAssignmentId(exercise.Id), Times.Once);
            _testRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Test>()), Times.Never);
            Assert.That(exercise.Tests, Is.Not.Null);
            Assert.That(exercise.Tests, Is.EquivalentTo(existingTests));
        }

        [Test]
        public void GetResultsForUserAsyncShouldRetrieveLastTestsResultsForUserAndConvertThemToAnAssignmentResultDto()
        {
            //Arrange
            var exerciseId = _random.NextPositive();
            var userId = _random.NextPositive();
            var lastTestResults = new List<TestResult>();

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfAssignmentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(lastTestResults);

            //Act
            var result = _service.GetResultsForUserAsync(exerciseId, userId, null).Result;

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestResults, Is.SameAs(lastTestResults));
            Assert.That(result.AssignmentId, Is.EqualTo(exerciseId));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfAssignmentAsync(exerciseId, userId, null), Times.Once);
        }

        [Test]
        public void ValidateTestCodeHashAsyncShouldReturnTrueForAValidHash()
        {
            //Arrange
            var testCodeHash = Guid.NewGuid().ToString();
            var assignment = new ExerciseBuilder().WithId().WithTestCodeHash(testCodeHash).Build();

            //Act
            var isValid = _service.ValidateTestCodeHashAsync(testCodeHash, assignment, false).Result;

            //Assert
            Assert.That(isValid, Is.True);
            _assignmentRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Assignment>()), Times.Never);
        }

        [Test]
        public void ValidateTestCodeHashAsyncShouldReturnFalseForAnInValidHash()
        {
            //Arrange
            var validHash = Guid.NewGuid().ToString();
            var invalidHash = Guid.NewGuid().ToString();
            var assignment = new ExerciseBuilder().WithId().WithTestCodeHash(validHash).Build();

            //Act
            var isValid = _service.ValidateTestCodeHashAsync(invalidHash, assignment, false).Result;

            //Assert
            Assert.That(isValid, Is.False);
            _assignmentRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Assignment>()), Times.Never);
        }

        [Test]
        public void ValidateTestCodeHashAsyncShouldAddTheHashForLectors()
        {
            //Arrange
            var testCodeHash = Guid.NewGuid().ToString();
            var assignment = new ExerciseBuilder().WithId().Build();

            //Act
            var isValid = _service.ValidateTestCodeHashAsync(testCodeHash, assignment, true).Result;

            //Assert
            Assert.That(isValid, Is.True);
            Assert.That(assignment.TestCodeHashes, Has.Count.EqualTo(1));
            _assignmentRepositoryMock.Verify(repo => repo.UpdateAsync(assignment), Times.Once);
        }

        [Test]
        public void ValidateTestCodeHashAsyncShouldNotAddHashesThatAlreadyExist()
        {
            //Arrange
            var testCodeHash = Guid.NewGuid().ToString();
            var assignment = new ExerciseBuilder().WithId().WithTestCodeHash(testCodeHash).Build();

            //Act
            var isValid = _service.ValidateTestCodeHashAsync(testCodeHash, assignment, true).Result;

            //Assert
            Assert.That(isValid, Is.True);
            _assignmentRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Assignment>()), Times.Never);
        }

        [Test]
        public void ValidateTestCodeHashAsyncShouldReturnTrueIfTheHashIsEmptyAndTheAssignmentHasNoHashes()
        {
            //Arrange
            var testCodeHash = string.Empty;
            var assignment = new ExerciseBuilder().WithId().Build();

            //Act
            var isValid = _service.ValidateTestCodeHashAsync(testCodeHash, assignment, false).Result;

            //Assert
            Assert.That(isValid, Is.True);
            _assignmentRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Assignment>()), Times.Never);
        }
    }
}