using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Api.Tests.Builders;
using Guts.Business;
using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain;
using Microsoft.AspNetCore.Mvc;
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
       
        private int _userId;

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
            var assignment = new AssignmentBuilder().WithId().Build();
           
            _assignmentServiceMock.Setup(service => service.GetOrCreateExerciseAsync(It.IsAny<AssignmentDto>()))
                .ReturnsAsync(assignment);

            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSourceCode()
                .Build();

            TestPostAssignmentTestRun(() => _controller.PostExerciseTestRun(postedModel), postedModel, assignment);

            _assignmentServiceMock.Verify(service => service.GetOrCreateExerciseAsync(postedModel.Assignment), Times.Once);
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
            
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>()), Times.Never);
        }

        [Test]
        public void PostExerciseTestRun_ShouldReturnBadRequestWhenTheTestCodeHashIsInvalid()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().Build();
            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithTestCodeHash()
                .Build();

            _assignmentServiceMock.Setup(service =>
                    service.ValidateTestCodeHashAsync(It.IsAny<string>(), It.IsAny<Assignment>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            //Act
            var badRequestResult = _controller.PostExerciseTestRun(postedModel).Result as BadRequestObjectResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Has.One.Matches((KeyValuePair<string, object> kv) => kv.Key == TestRunController.InvalidTestCodeHashErrorKey));
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>()), Times.Never);
        }

        [Test]
        public void PostExerciseTestRun_ShouldNotCreateNewTestsIfUserIsAStudent()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().Build();
            _assignmentServiceMock.Setup(service => service.GetAssignmentAsync(It.IsAny<AssignmentDto>()))
                .ReturnsAsync(assignment);

            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSourceCode()
                .Build();

            _controller = CreateControllerWithUserInContext(Role.Constants.Student);

            //Act
            _controller.PostExerciseTestRun(postedModel).Wait();

            //Assert
            _assignmentServiceMock.Verify(service => service.LoadTestsForAssignmentAsync(assignment), Times.Once);
            _assignmentServiceMock.Verify(
                service => service.LoadOrCreateTestsForAssignmentAsync(It.IsAny<Assignment>(),
                    It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Test]
        public void PostProjectTestRun_ShouldSaveItInTheRepository()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().Build();
            _assignmentServiceMock.Setup(service => service.GetOrCreateProjectComponentAsync(It.IsAny<AssignmentDto>()))
                .ReturnsAsync(assignment);

            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSourceCode()
                .Build();

            TestPostAssignmentTestRun(() => _controller.PostProjectTestRun(postedModel), postedModel, assignment);

            _assignmentServiceMock.Verify(service => service.GetOrCreateProjectComponentAsync(postedModel.Assignment), Times.Once);
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
            _testResultConverterMock.Verify(converter => converter.From(It.IsAny<IEnumerable<TestResultModel>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Assignment>()), Times.Never);
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>()), Times.Never);
        }

        [Test]
        public void PostProjectTestRun_ShouldReturnBadRequestWhenTheTestCodeHashIsInvalid()
        {
            //Arrange
            var assignment = new AssignmentBuilder().WithId().Build();
            var assignmentDto = new AssignmentDtoBuilder().WithAssignmentCode(assignment.Code).Build();
            var postedModel = new CreateAssignmentTestRunModelBuilder()
                .WithAssignment(assignmentDto)
                .WithSourceCode()
                .Build();

            _assignmentServiceMock.Setup(service =>
                    service.ValidateTestCodeHashAsync(It.IsAny<string>(), It.IsAny<Assignment>(), It.IsAny<bool>()))
                .ReturnsAsync(false);

            //Act
            var badRequestResult = _controller.PostProjectTestRun(postedModel).Result as BadRequestObjectResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Has.One.Matches((KeyValuePair<string, object> kv) => kv.Key == TestRunController.InvalidTestCodeHashErrorKey));
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(It.IsAny<TestRun>()), Times.Never);
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
                .Setup(converter => converter.From(It.IsAny<IEnumerable<TestResultModel>>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<Assignment>()))
                .Returns(convertedTestRun);

            var savedTestRun = new TestRun();
            _testRunServiceMock.Setup(repo => repo.RegisterRunAsync(It.IsAny<TestRun>())).ReturnsAsync(savedTestRun);

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
                converter => converter.From(postedModel.Results, postedModel.SourceCode, _userId, existingAssignment), Times.Once);

            _assignmentServiceMock.Verify(
                service => service.LoadOrCreateTestsForAssignmentAsync(existingAssignment,
                    It.Is<IEnumerable<string>>(testNames => testNames.All(testName =>
                        postedModel.Results.Any(testResult => testResult.TestName == testName)))), Times.Once);
            _testRunServiceMock.Verify(repo => repo.RegisterRunAsync(convertedTestRun), Times.Once);
            _testResultConverterMock.Verify(converter => converter.ToTestRunModel(savedTestRun), Times.Once);
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(_controller.GetTestRun)));
            Assert.That(createdResult.RouteValues["id"], Is.EqualTo(savedTestRunModel.Id));
            Assert.That(createdResult.Value, Is.EqualTo(savedTestRunModel));
        }

        private TestRunController CreateControllerWithUserInContext(string role)
        {
            return new TestRunController(_testResultConverterMock.Object, _testRunServiceMock.Object, _assignmentServiceMock.Object)
            {
                ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
            };
        }
    }
}
