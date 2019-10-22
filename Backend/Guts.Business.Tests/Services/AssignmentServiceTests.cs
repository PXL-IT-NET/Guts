using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class AssignmentServiceTests
    {
        private AssignmentService _service;
        private Random _random;
        private Mock<IAssignmentRepository> _assignmentRepositoryMock;
        private Mock<ITestRepository> _testRepositoryMock;
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<ITestResultRepository> _testResultRepositoryMock;
        private Mock<ITestRunRepository> _testRunRepositoryMock;
        private Mock<IProjectService> _projectServiceMock;
        private Mock<ITopicService> _topicServiceMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _testRepositoryMock = new Mock<ITestRepository>();
            _topicServiceMock = new Mock<ITopicService>();
            _chapterServiceMock = new Mock<IChapterService>();
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _testRunRepositoryMock = new Mock<ITestRunRepository>();
            _projectServiceMock = new Mock<IProjectService>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();

            _service = new AssignmentService(_assignmentRepositoryMock.Object, 
                _topicServiceMock.Object,
                _chapterServiceMock.Object, 
                _projectServiceMock.Object,
                _testRepositoryMock.Object,
                _testResultRepositoryMock.Object,
                _testRunRepositoryMock.Object);
        }

        [Test]
        public void GetOrCreateExerciseAsync_ShouldReturnExerciseIfItExists()
        {
            //Arrange
            var assignmentDto = new AssignmentDtoBuilder().Build();
            var existingChapter = new Chapter
            {
                Id = _random.NextPositive(),
                Code = assignmentDto.TopicCode
            };

            _chapterServiceMock.Setup(repo => repo.GetOrCreateChapterAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingChapter);

            var existingAssignment = new Assignment
            {
                Id = _random.NextPositive(),
                Code = assignmentDto.AssignmentCode
            };

            _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(existingAssignment);

            //Act
            var result = _service.GetOrCreateExerciseAsync(assignmentDto).Result;

            //Assert
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, existingAssignment.Code), Times.Once());
            _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingAssignment));
        }

        [Test]
        public void GetOrCreateExerciseAsync_ShouldCreateExerciseIfItDoesNotExist()
        {
            //Arrange
            var assignmentDto = new AssignmentDtoBuilder().Build();

            var existingChapter = new Chapter
            {
                Id = _random.NextPositive(),
                Code = assignmentDto.TopicCode
            };
            
            _chapterServiceMock.Setup(service => service.GetOrCreateChapterAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingChapter);

            var addedAssignment = new Assignment
            {
                Id = _random.NextPositive(),
                Code = assignmentDto.AssignmentCode
            };

            _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).Throws<DataNotFoundException>();
            _assignmentRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Assignment>())).ReturnsAsync(addedAssignment);

            //Act
            var result = _service.GetOrCreateExerciseAsync(assignmentDto).Result;

            //Assert
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, addedAssignment.Code), Times.Once());
            _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Once);
            Assert.That(result, Is.EqualTo(addedAssignment));
        }

        [Test]
        public void GetOrCreateProjectComponentAsync_ShouldReturnComponentIfItExists()
        {
            //Arrange
            var projectComponentDto = new AssignmentDtoBuilder().Build();
            var existingProject = new Project
            {
                Id = _random.NextPositive(),
                Code = projectComponentDto.TopicCode
            };

            _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingProject);

            var existingComponent = new Assignment()
            {
                Id = _random.NextPositive(),
                Code = projectComponentDto.AssignmentCode
            };

            _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(existingComponent);

            //Act
            var result = _service.GetOrCreateProjectComponentAsync(projectComponentDto).Result;

            //Assert
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.Id, existingComponent.Code), Times.Once());
            _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Never);
            Assert.That(result, Is.EqualTo(existingComponent));
        }

        [Test]
        public void GetOrCreateProjectComponentAsync_ShouldCreateComponentIfItDoesNotExist()
        {
            //Arrange
            var assignmentDto = new AssignmentDtoBuilder().Build();
            var existingProject = new Project
            {
                Id = _random.NextPositive(),
                Code = assignmentDto.TopicCode
            };

            _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingProject);

            var addedComponent = new Assignment()
            {
                Id = _random.NextPositive(),
                Code = assignmentDto.AssignmentCode
            };

            _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).Throws<DataNotFoundException>();
            _assignmentRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Assignment>())).ReturnsAsync(addedComponent);

            //Act
            var result = _service.GetOrCreateProjectComponentAsync(assignmentDto).Result;

            //Assert
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.Id, addedComponent.Code), Times.Once());
            _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Once);
            Assert.That(result, Is.EqualTo(addedComponent));
        }

        [Test]
        public void LoadTestsForAssignmentAsync_ShouldLoadExistingTests()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().Build();

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
            var assignment = new Assignment
            {
                Id = _random.NextPositive()
            };

            _testRepositoryMock.Setup(repo => repo.FindByAssignmentId(It.IsAny<int>())).ReturnsAsync(new List<Test>());
            _testRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Test>())).ReturnsAsync((Test test) =>
            {
                test.Id = _random.NextPositive();
                return test;
            });

            //Act
            _service.LoadOrCreateTestsForAssignmentAsync(assignment, testNames).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByAssignmentId(assignment.Id), Times.Once);
            _testRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Test>(test => testNames.Contains(test.TestName))), Times.Exactly(testNames.Count));
            Assert.That(assignment.Tests, Is.Not.Null);
            Assert.That(assignment.Tests.Count, Is.EqualTo(testNames.Count));
            Assert.That(assignment.Tests, Has.All.Matches<Test>(test => test.Id > 0) );
            Assert.That(assignment.Tests, Has.All.Matches<Test>(test => testNames.Contains(test.TestName)));

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

            var assignment = new Assignment
            {
                Id = _random.NextPositive(),
            };

            _testRepositoryMock.Setup(repo => repo.FindByAssignmentId(It.IsAny<int>())).ReturnsAsync(existingTests);

            //Act
            _service.LoadOrCreateTestsForAssignmentAsync(assignment, testNames).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByAssignmentId(assignment.Id), Times.Once);
            _testRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Test>()), Times.Never);
            Assert.That(assignment.Tests, Is.Not.Null);
            Assert.That(assignment.Tests, Is.EquivalentTo(existingTests));
        }

        [Test]
        public void GetResultsForUserAsyncShouldRetrieveLastTestsResultsForUserAndConvertThemToAnAssignmentResultDto()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var userId = _random.NextPositive();
            var lastTestResults = new List<TestResult>();

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfUser(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(lastTestResults);

            //Act
            var result = _service.GetResultsForUserAsync(assignmentId, userId, null).Result;

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestResults, Is.SameAs(lastTestResults));
            Assert.That(result.AssignmentId, Is.EqualTo(assignmentId));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfUser(assignmentId, userId, null), Times.Once);
        }

        [Test]
        public void ValidateTestCodeHashAsyncShouldReturnTrueForAValidHash()
        {
            //Arrange
            var testCodeHash = Guid.NewGuid().ToString();
            var assignment = new AssignmentBuilder().WithId().WithTestCodeHash(testCodeHash).Build();

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
            var assignment = new AssignmentBuilder().WithId().WithTestCodeHash(validHash).Build();

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
            var assignment = new AssignmentBuilder().WithId().Build();

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
            var assignment = new AssignmentBuilder().WithId().WithTestCodeHash(testCodeHash).Build();

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
            var assignment = new AssignmentBuilder().WithId().Build();

            //Act
            var isValid = _service.ValidateTestCodeHashAsync(testCodeHash, assignment, false).Result;

            //Assert
            Assert.That(isValid, Is.True);
            _assignmentRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Assignment>()), Times.Never);
        }

        [Test]
        public void GetUserTestRunInfoForAssignment_ShouldRetrieveTestrunsOfAUserForAnAssignmentAndReturnTheCorrectInfo()
        {
            //Arrange
            var random = new Random();
            var now = DateTime.UtcNow;
            int assignmentId = random.NextPositive();
            int userId = random.NextPositive();
            var firstRun = new TestRunBuilder(random).WithCreationDate(now.AddDays(-10)).Build();
            var secondRun = new TestRunBuilder(random).WithCreationDate(now.AddDays(-5)).Build();
            var lastRun = new TestRunBuilder(random).WithCreationDate(now.AddDays(-1)).Build();
            var testRuns = new List<TestRun>
            {
                firstRun,
                secondRun,
                lastRun
            };
            _testRunRepositoryMock
                .Setup(repo => repo.GetUserTestRunsForAssignmentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(testRuns);

            //Act
            var testRunInfo = _service.GetUserTestRunInfoForAssignment(assignmentId, userId, now).Result;

            //Assert
            Assert.That(testRunInfo, Is.Not.Null);
            _testRunRepositoryMock.Verify(repo => repo.GetUserTestRunsForAssignmentAsync(assignmentId, userId, now), Times.Once);
            Assert.That(testRunInfo.FirstRunDateTime, Is.EqualTo(firstRun.CreateDateTime));
            Assert.That(testRunInfo.LastRunDateTime, Is.EqualTo(lastRun.CreateDateTime));
            Assert.That(testRunInfo.NumberOfRuns, Is.EqualTo(testRuns.Count));
            Assert.That(testRunInfo.SourceCode, Is.EqualTo(lastRun.SourceCode));
        }
    }
}