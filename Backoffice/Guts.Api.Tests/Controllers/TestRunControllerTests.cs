using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Api.Tests.Builders;
using Guts.Business.Dtos;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class TestRunControllerTests
    {
        private TestRunController _controller;
        private Random _random;
        private Mock<ITestRunService> _testRunServiceMock;
        private Mock<ITestRunConverter> _testResultConverterMock;
        private Mock<IAssignmentService> _assignmentServiceMock;
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<IProjectService> _projectServiceMock;
        private int _userId;
        private Mock<ILogger<TestRunController>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _testResultConverterMock = new Mock<ITestRunConverter>();
            _testRunServiceMock = new Mock<ITestRunService>();
            _assignmentServiceMock = new Mock<IAssignmentService>();
            _assignmentServiceMock.Setup(service =>
                    service.ValidateTestCodeHashAsync(It.IsAny<string>(), It.IsAny<Assignment>(), It.IsAny<bool>()))
                .ReturnsAsync(true);
            _chapterServiceMock = new Mock<IChapterService>();
            _projectServiceMock = new Mock<IProjectService>();
            _loggerMock = new Mock<ILogger<TestRunController>>();

            _userId = _random.Next(1, int.MaxValue);
            _controller = CreateControllerWithUserInContext(Role.Constants.Lector);
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void PostExerciseTestRun_ShouldSaveItInTheRepository()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();
            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();

            _chapterServiceMock.Setup(service => service.GetOrCreateChapterAsync(assignmentDto.CourseCode, assignmentDto.TopicCode, null))
                .ReturnsAsync(chapter);
            _assignmentServiceMock
                .Setup(service => service.GetOrCreateAssignmentAsync(chapter.Id, assignmentDto.AssignmentCode))
                .ReturnsAsync(assignment);

            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSolutionFile()
                .Build();

            TestPostAssignmentTestRun(() => _controller.PostExerciseTestRun(postedModel), postedModel, assignment);

            _chapterServiceMock.Verify();
            _assignmentServiceMock.Verify();
        }

        [Test]
        public void PostExerciseTestRun_ShouldReturnBadRequestIfPostedModelIsInvalid()
        {
            //Arrange
            var errorKey = Guid.NewGuid().ToString();
            var errorMessage = Guid.NewGuid().ToString();
            _controller.ModelState.AddModelError(errorKey, errorMessage);

            var assignmentDto = new AssignmentDtoBuilder().Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder().WithAssignment(assignmentDto).Build();

            //Act
            var badRequestResult = _controller.PostExerciseTestRun(postedModel).Result as BadRequestObjectResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Has.One.Matches((KeyValuePair<string, object> kv) => kv.Key == errorKey));
            
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>(), It.IsAny<IEnumerable<SolutionFile>>()), Times.Never);
        }

        [Test]
        public void PostExerciseTestRun_ShouldReturnBadRequestWhenTheTestCodeHashIsInvalid()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(chapter).Build();

            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();

            _chapterServiceMock.Setup(service => service.GetOrCreateChapterAsync(assignmentDto.CourseCode, assignmentDto.TopicCode, null))
                .ReturnsAsync(chapter);
            _assignmentServiceMock
                .Setup(service => service.GetOrCreateAssignmentAsync(chapter.Id, assignmentDto.AssignmentCode))
                .ReturnsAsync(assignment);

            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithTestCodeHash()
                .Build();

            _assignmentServiceMock.Setup(service =>
                    service.ValidateTestCodeHashAsync(postedModel.TestCodeHash, assignment, It.IsAny<bool>()))
                .ReturnsAsync(false);

            //Act
            var badRequestResult = _controller.PostExerciseTestRun(postedModel).Result as BadRequestObjectResult;

            //Assert
            _chapterServiceMock.Verify();
            _assignmentServiceMock.Verify();
            _assignmentServiceMock.Verify();

            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Has.One.Matches((KeyValuePair<string, object> kv) => kv.Key == TestRunController.InvalidTestCodeHashErrorKey));
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>(), It.IsAny<IEnumerable<SolutionFile>>()), Times.Never);
        }

        [Test]
        public void PostExerciseTestRun_ShouldNotCreateNewTestsIfUserIsAStudent()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().Build();
            _assignmentServiceMock.Setup(service => service.GetAssignmentOfCurrentPeriodAsync(It.IsAny<AssignmentDto>()))
                .ReturnsAsync(assignment);

            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSolutionFile()
                .Build();

            _controller = CreateControllerWithUserInContext(Role.Constants.Student);

            //Act
            _controller.PostExerciseTestRun(postedModel).Wait();

            //Assert
            _assignmentServiceMock.Verify(service => service.LoadTestsForAssignmentAsync(assignment), Times.Once);
            _assignmentServiceMock.Verify(
                service => service.LoadOrCreateTestsForAssignmentAsync(It.IsAny<Assignment>(),
                    It.IsAny<IReadOnlyList<string>>()), Times.Never);
        }

        [Test]
        public void PostProjectTestRun_ShouldSaveItInTheRepository()
        {
            //Arrange
            var project = new ProjectBuilder().WithId().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(project).Build();
            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();

            _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(assignmentDto.CourseCode, assignmentDto.TopicCode, null))
                .ReturnsAsync(project);
            _assignmentServiceMock
                .Setup(service => service.GetOrCreateAssignmentAsync(project.Id, assignmentDto.AssignmentCode))
                .ReturnsAsync(assignment);

            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSolutionFile()
                .Build();

            TestPostAssignmentTestRun(() => _controller.PostProjectTestRun(postedModel), postedModel, assignment);

            _projectServiceMock.Verify();
            _assignmentServiceMock.Verify();
        }

        [Test]
        public void PostProjectTestRun_ShouldReturnBadRequestIfPostedModelIsInvalid()
        {
            //Arrange
            var errorKey = Guid.NewGuid().ToString();
            var errorMessage = Guid.NewGuid().ToString();
            _controller.ModelState.AddModelError(errorKey, errorMessage);

            var assignmentDto = new AssignmentDtoBuilder().Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder().WithAssignment(assignmentDto).Build();

            //Act
            var badRequestResult = _controller.PostProjectTestRun(postedModel).Result as BadRequestObjectResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Has.One.Matches((KeyValuePair<string, object> kv) => kv.Key == errorKey));
            _testResultConverterMock.Verify(converter => converter.From(It.IsAny<IEnumerable<TestResultModel>>(), It.IsAny<int>(), It.IsAny<Assignment>()), Times.Never);
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>(), It.IsAny<IEnumerable<SolutionFile>>()), Times.Never);
        }

        [Test]
        public void PostProjectTestRun_ShouldReturnBadRequestWhenTheTestCodeHashIsInvalid()
        {
            //Arrange
            var project = new ProjectBuilder().WithId().Build();
            var assignment = new AssignmentBuilder().WithId().WithTopic(project).Build();
            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();

            _projectServiceMock.Setup(service => service.GetOrCreateProjectAsync(assignmentDto.CourseCode, assignmentDto.TopicCode, null))
                .ReturnsAsync(project);
            _assignmentServiceMock
                .Setup(service => service.GetOrCreateAssignmentAsync(project.Id, assignmentDto.AssignmentCode))
                .ReturnsAsync(assignment);

            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSolutionFile()
                .Build();

            _assignmentServiceMock.Setup(service =>
                    service.ValidateTestCodeHashAsync(postedModel.TestCodeHash, assignment, It.IsAny<bool>()))
                .ReturnsAsync(false);

            //Act
            var badRequestResult = _controller.PostProjectTestRun(postedModel).Result as BadRequestObjectResult;

            //Assert
            _projectServiceMock.Verify();
            _assignmentServiceMock.Verify();
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Has.One.Matches((KeyValuePair<string, object> kv) => kv.Key == TestRunController.InvalidTestCodeHashErrorKey));
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>(), It.IsAny<IEnumerable<SolutionFile>>()), Times.Never);
        }

        [Test]
        public void GetTestRun_ShouldRetrieveItFromTheServiceAndReturnAModel()
        {
            //Arrange
            var storedTestRun = new TestRun
            {
                Id = _random.NextPositive()
            };

            var convertedTestRunModel = new SavedTestRunModel();

            _testRunServiceMock.Setup(repo => repo.GetTestRunAsync(It.IsAny<int>())).ReturnsAsync(storedTestRun);
            _testResultConverterMock.Setup(converter => converter.ToTestRunModel(It.IsAny<TestRun>()))
                .Returns(convertedTestRunModel);

            //Act
            var okResult = _controller.GetTestRun(storedTestRun.Id).Result as OkObjectResult;

            //Assert
            Assert.That(okResult, Is.Not.Null);
            _testRunServiceMock.Verify(repo => repo.GetTestRunAsync(storedTestRun.Id), Times.Once);
            _testResultConverterMock.Verify(converter => converter.ToTestRunModel(storedTestRun), Times.Once);
            Assert.That(okResult.Value, Is.EqualTo(convertedTestRunModel));
        }

        private void TestPostAssignmentTestRun(Func<Task<IActionResult>> actFunction,
            CreateAssignmentTestRunModel postedModel, Assignment existingAssignment)
        {
            var convertedTestRun = new TestRun();
            _testResultConverterMock
                .Setup(converter => converter.From(It.IsAny<IEnumerable<TestResultModel>>(), It.IsAny<int>(), It.IsAny<Assignment>()))
                .Returns(convertedTestRun);

            var savedTestRun = new TestRun();
            _testRunServiceMock.Setup(repo => repo.RegisterRunAsync(It.IsAny<TestRun>(), It.IsAny<IEnumerable<SolutionFile>>())).ReturnsAsync(savedTestRun);

            var savedTestRunModel = new SavedTestRunModel
            {
                Id = _random.NextPositive()
            };
            _testResultConverterMock.Setup(converter => converter.ToTestRunModel(It.IsAny<TestRun>()))
                .Returns(savedTestRunModel);

            //Act
            var createdResult = actFunction.Invoke().Result as CreatedAtActionResult;

            //Assert
            Assert.That(createdResult, Is.Not.Null);
            _testResultConverterMock.Verify(
                converter => converter.From(postedModel.Results, _userId, existingAssignment), Times.Once);

            _assignmentServiceMock.Verify(
                service => service.LoadOrCreateTestsForAssignmentAsync(existingAssignment,
                    It.Is<IReadOnlyList<string>>(testNames => testNames.All(testName =>
                        postedModel.Results.Any(testResult => testResult.TestName == testName)))), Times.Once);
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(convertedTestRun, It.IsAny<IEnumerable<SolutionFile>>()), Times.Once); //TODO: also test if solution files are passed in
            _testResultConverterMock.Verify(converter => converter.ToTestRunModel(savedTestRun), Times.Once);
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(_controller.GetTestRun)));
            Assert.That(createdResult.RouteValues["id"], Is.EqualTo(savedTestRunModel.Id));
            Assert.That(createdResult.Value, Is.EqualTo(savedTestRunModel));
        }

        private TestRunController CreateControllerWithUserInContext(string role)
        {
            return new TestRunController(
                _testResultConverterMock.Object, 
                _testRunServiceMock.Object, 
                _assignmentServiceMock.Object,
                _chapterServiceMock.Object,
                _projectServiceMock.Object, 
                _loggerMock.Object)
            {
                ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
            };
        }
    }
}
