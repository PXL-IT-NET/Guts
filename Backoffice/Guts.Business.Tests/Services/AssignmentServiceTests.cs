using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Converters;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
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
        private Mock<ITestResultRepository> _testResultRepositoryMock;
        private Mock<ITestRunRepository> _testRunRepositoryMock;
        private Mock<ITopicService> _topicServiceMock;
        private Mock<IAssignmentWithResultsConverter> _assignmentWithResultsConverterMock;
        private Mock<ISolutionFileRepository> _solutionFileRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _testRepositoryMock = new Mock<ITestRepository>();
            _topicServiceMock = new Mock<ITopicService>();
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _testRunRepositoryMock = new Mock<ITestRunRepository>();
            _assignmentRepositoryMock = new Mock<IAssignmentRepository>();
            _solutionFileRepositoryMock = new Mock<ISolutionFileRepository>();
            _assignmentWithResultsConverterMock = new Mock<IAssignmentWithResultsConverter>();

            _service = new AssignmentService(_assignmentRepositoryMock.Object, 
                _topicServiceMock.Object,
                _testRepositoryMock.Object,
                _testResultRepositoryMock.Object,
                _testRunRepositoryMock.Object,
                _solutionFileRepositoryMock.Object,
                _assignmentWithResultsConverterMock.Object);
        }

        [Test]
        public void GetAssignmentAsync_ShouldRetrieveTheTopicAndThenTheAssignment()
        {
            //Arrange
            var dto = new AssignmentDtoBuilder().Build();
            var existingTopic = new ChapterBuilder().WithId().Build();
            var existingAssignment = new AssignmentBuilder().WithId().Build();

            _topicServiceMock.Setup(service => service.GetTopicAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingTopic);

            _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(existingAssignment);

            //Act
            var assignment = _service.GetAssignmentAsync(dto).Result;

            //Assert
            Assert.That(assignment, Is.SameAs(existingAssignment));
            _topicServiceMock.Verify(service => service.GetTopicAsync(dto.CourseCode, dto.TopicCode), Times.Once);
            _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingTopic.Id, dto.AssignmentCode), Times.Once);
        }

        //[Test]
        //public void GetOrCreateExerciseAsync_ShouldReturnExerciseIfItExists()
        //{
        //    //Arrange
        //    var assignmentDto = new AssignmentDtoBuilder().Build();
        //    var existingChapter = new Chapter
        //    {
        //        Id = _random.NextPositive(),
        //        Code = assignmentDto.TopicCode
        //    };

        //    _chapterServiceMock.Setup(repo => repo.GetOrCreateChapterAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingChapter);

        //    var existingAssignment = new AssignmentBuilder()
        //        .WithId()
        //        .WithCode(assignmentDto.AssignmentCode).Build();

        //    _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(existingAssignment);

        //    //Act
        //    var result = _service.GetOrCreateExerciseAsync(assignmentDto).Result;

        //    //Assert
        //    _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, existingAssignment.Code), Times.Once());
        //    _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Never);
        //    Assert.That(result, Is.EqualTo(existingAssignment));
        //}

        //[Test]
        //public void GetOrCreateExerciseAsync_ShouldCreateExerciseIfItDoesNotExist()
        //{
        //    //Arrange
        //    var assignmentDto = new AssignmentDtoBuilder().Build();

        //    var existingChapter = new Chapter
        //    {
        //        Id = _random.NextPositive(),
        //        Code = assignmentDto.TopicCode
        //    };
            
        //    _chapterServiceMock.Setup(service => service.GetOrCreateChapterAsync(It.IsAny<string>(), It.IsAny<string>()))
        //        .ReturnsAsync(existingChapter);

        //    var addedAssignment = new AssignmentBuilder()
        //        .WithId()
        //        .WithCode(assignmentDto.AssignmentCode).Build();

        //    _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).Throws<DataNotFoundException>();
        //    _assignmentRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Assignment>())).ReturnsAsync(addedAssignment);

        //    //Act
        //    var result = _service.GetOrCreateExerciseAsync(assignmentDto).Result;

        //    //Assert
        //    _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingChapter.Id, addedAssignment.Code), Times.Once());
        //    _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Once);
        //    Assert.That(result, Is.EqualTo(addedAssignment));
        //}

        //[Test]
        //public void GetOrCreateProjectComponentAsync_ShouldReturnComponentIfItExists()
        //{
        //    //Arrange
        //    var projectComponentDto = new AssignmentDtoBuilder().Build();
        //    var existingProject = new Project
        //    {
        //        Id = _random.NextPositive(),
        //        Code = projectComponentDto.TopicCode
        //    };

        //    _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingProject);

        //    var existingComponent = new Assignment()
        //    {
        //        Id = _random.NextPositive(),
        //        Code = projectComponentDto.AssignmentCode
        //    };

        //    _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(existingComponent);

        //    //Act
        //    var result = _service.GetOrCreateProjectComponentAsync(projectComponentDto).Result;

        //    //Assert
        //    _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.Id, existingComponent.Code), Times.Once());
        //    _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Never);
        //    Assert.That(result, Is.EqualTo(existingComponent));
        //}

        //[Test]
        //public void GetOrCreateProjectComponentAsync_ShouldCreateComponentIfItDoesNotExist()
        //{
        //    //Arrange
        //    var assignmentDto = new AssignmentDtoBuilder().Build();
        //    var existingProject = new Project
        //    {
        //        Id = _random.NextPositive(),
        //        Code = assignmentDto.TopicCode
        //    };

        //    _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingProject);

        //    var addedComponent = new Assignment()
        //    {
        //        Id = _random.NextPositive(),
        //        Code = assignmentDto.AssignmentCode
        //    };

        //    _assignmentRepositoryMock.Setup(repo => repo.GetSingleAsync(It.IsAny<int>(), It.IsAny<string>())).Throws<DataNotFoundException>();
        //    _assignmentRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Assignment>())).ReturnsAsync(addedComponent);

        //    //Act
        //    var result = _service.GetOrCreateProjectComponentAsync(assignmentDto).Result;

        //    //Assert
        //    _assignmentRepositoryMock.Verify(repo => repo.GetSingleAsync(existingProject.Id, addedComponent.Code), Times.Once());
        //    _assignmentRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Assignment>()), Times.Once);
        //    Assert.That(result, Is.EqualTo(addedComponent));
        //}

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
            List<Test> existingTests = testNames.Select(name => new Test
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
        public void LoadOrCreateTestsForAssignmentAsync_ShouldRemoveExistingTestsThatAreNotInTestNamesParameter()
        {
            //Arrange
            Test test1 = new TestBuilder().Build();
            Test deprecatedTest = new TestBuilder().Build();

            var testNames = new List<string> { test1.TestName };
            var assignment = new Assignment
            {
                Id = _random.NextPositive()
            };

            _testRepositoryMock.Setup(repo => repo.FindByAssignmentId(It.IsAny<int>())).ReturnsAsync(new List<Test>{test1, deprecatedTest});

            //Act
            _service.LoadOrCreateTestsForAssignmentAsync(assignment, testNames).Wait();

            //Assert
            _testRepositoryMock.Verify(repo => repo.FindByAssignmentId(assignment.Id), Times.Once);
            _testRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Test>()), Times.Never);
            _testRepositoryMock.Verify(repo => repo.DeleteAsync(deprecatedTest), Times.Once);

            Assert.That(assignment.Tests, Is.Not.Null);
            Assert.That(assignment.Tests.Count, Is.EqualTo(testNames.Count));
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
        public void GetResultsForTeamAsync_ShouldRetrieveLastTestsResultsForTeamAndConvertThemToAnAssignmentResultDto()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var teamId = _random.NextPositive();
            var lastTestResults = new List<TestResult>();

            _testResultRepositoryMock.Setup(repo => repo.GetLastTestResultsOfTeam(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(lastTestResults);

            //Act
            var result = _service.GetResultsForTeamAsync(assignmentId, teamId, null).Result;

            //Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TestResults, Is.SameAs(lastTestResults));
            Assert.That(result.AssignmentId, Is.EqualTo(assignmentId));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfTeam(assignmentId, teamId, null), Times.Once);
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
        public void GetAssignmentUserStatisticsAsync_ShouldGetLastTestResultsAndConvertThem()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var date = DateTime.UtcNow;
            var lastTestResults = new List<TestResult>();
            var assignmentStatisticsDto = new AssignmentStatisticsDto();

            _testResultRepositoryMock
                .Setup(repo => repo.GetLastTestResultsOfAllUsers(It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(lastTestResults);

            _assignmentWithResultsConverterMock
                .Setup(converter => converter.ToAssignmentStatisticsDto(It.IsAny<int>(), It.IsAny<IList<TestResult>>()))
                .Returns(assignmentStatisticsDto);

            //Act
            var result = _service.GetAssignmentUserStatisticsAsync(assignmentId, date).Result;

            //Assert
            Assert.That(result, Is.SameAs(assignmentStatisticsDto));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfAllUsers(assignmentId, date), Times.Once);
            _assignmentWithResultsConverterMock.Verify(converter => converter.ToAssignmentStatisticsDto(assignmentId, lastTestResults), Times.Once);
        }

        [Test]
        public void GetAssignmentTeamStatisticsAsync_ShouldGetLastTestResultsAndConvertThem()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var date = DateTime.UtcNow;
            var lastTestResults = new List<TestResult>();
            var assignmentStatisticsDto = new AssignmentStatisticsDto();

            _testResultRepositoryMock
                .Setup(repo => repo.GetLastTestResultsOfAllTeams(It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(lastTestResults);

            _assignmentWithResultsConverterMock
                .Setup(converter => converter.ToAssignmentStatisticsDto(It.IsAny<int>(), It.IsAny<IList<TestResult>>()))
                .Returns(assignmentStatisticsDto);

            //Act
            var result = _service.GetAssignmentTeamStatisticsAsync(assignmentId, date).Result;

            //Assert
            Assert.That(result, Is.SameAs(assignmentStatisticsDto));
            _testResultRepositoryMock.Verify(repo => repo.GetLastTestResultsOfAllTeams(assignmentId, date), Times.Once);
            _assignmentWithResultsConverterMock.Verify(converter => converter.ToAssignmentStatisticsDto(assignmentId, lastTestResults), Times.Once);
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
        }

        [Test]
        public void GetTeamTestRunInfoForAssignment_ShouldRetrieveTestrunsOfATeamForAnAssignmentAndReturnTheCorrectInfo()
        {
            //Arrange
            var random = new Random();
            var now = DateTime.UtcNow;
            int assignmentId = random.NextPositive();
            int teamId = random.NextPositive();
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
                .Setup(repo => repo.GetTeamTestRunsForAssignmentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(testRuns);

            //Act
            var testRunInfo = _service.GetTeamTestRunInfoForAssignment(assignmentId, teamId, now).Result;

            //Assert
            Assert.That(testRunInfo, Is.Not.Null);
            _testRunRepositoryMock.Verify(repo => repo.GetTeamTestRunsForAssignmentAsync(assignmentId, teamId, now), Times.Once);
            Assert.That(testRunInfo.FirstRunDateTime, Is.EqualTo(firstRun.CreateDateTime));
            Assert.That(testRunInfo.LastRunDateTime, Is.EqualTo(lastRun.CreateDateTime));
            Assert.That(testRunInfo.NumberOfRuns, Is.EqualTo(testRuns.Count));
        }

        [Test]
        public void GetAllSolutions_ShouldGetTheLastSolutionFilesForEachUserOrderedByUserFullName()
        {
            //Arrange
            var assignmentId = _random.NextPositive();
            var numberOfUsers = _random.Next(5, 21);
            var solutionFilesWithUser = new List<SolutionFile>();
            for (int i = 0; i < numberOfUsers; i++)
            {
                solutionFilesWithUser.Add(new SolutionFileBuilder().WithUser().Build());
            }

            _solutionFileRepositoryMock.Setup(repo => repo.GetAllLatestOfAssignmentAsync(assignmentId))
                .ReturnsAsync(solutionFilesWithUser);

            //Act
            var assignmentSolutionDtos = _service.GetAllSolutions(assignmentId).Result;

            //Assert
            _solutionFileRepositoryMock.Verify();

            Assert.That(assignmentSolutionDtos, Has.Count.EqualTo(numberOfUsers));
            foreach (var assignmentSolutionDto in assignmentSolutionDtos)
            {
                var matchingSolutionFile = solutionFilesWithUser.FirstOrDefault(t => t.UserId == assignmentSolutionDto.WriterId);
                Assert.That(matchingSolutionFile, Is.Not.Null);
                Assert.That(assignmentSolutionDto.SolutionFiles, Has.One.EqualTo(matchingSolutionFile));
                Assert.That(assignmentSolutionDto.WriterName, Does.StartWith(matchingSolutionFile.User.FirstName));
                Assert.That(assignmentSolutionDto.WriterName, Does.EndWith(matchingSolutionFile.User.LastName));
            }
           
            Assert.That(assignmentSolutionDtos,Is.EquivalentTo(assignmentSolutionDtos.OrderBy(dto => dto.WriterName)));
        }
    }
}