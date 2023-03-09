using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using AutoMapper;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.ExamModels;
using Guts.Api.Tests.Builders;
using Guts.Business;
using Guts.Business.Dtos;
using Guts.Business.Services.Exam;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.Tests.Builders;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class ExamControllerTests
    {
        private ExamController _controller;
        private Random _random;
        private Mock<IExamService> _examServiceMock;
        private Mock<IMapper> _mapperMock;
        private int _userId;

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _examServiceMock = new Mock<IExamService>();
            _mapperMock = new Mock<IMapper>();

            _controller = CreateControllerWithUserInContext(Role.Constants.Lector);
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetExams_ShouldReturnExamsOfCourse()
        {
            //Arrange
            int? courseId = _random.NextPositive();

            var retrievedExams = new List<Exam>()
            {
                new ExamBuilder().Build()
            };

            _examServiceMock.Setup(service => service.GetExamsAsync(courseId))
                .ReturnsAsync(retrievedExams).Verifiable();

            _mapperMock.Setup(m => m.Map<ExamOutputModel>(It.IsAny<Exam>())).Returns(new ExamOutputModel());

            //Act
            var actionResult = _controller.GetExams(courseId).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            var results = actionResult.Value as IEnumerable<ExamOutputModel>;
            Assert.That(results.Count(), Is.EqualTo(retrievedExams.Count));
            _examServiceMock.Verify();
            _mapperMock.Verify(m => m.Map<ExamOutputModel>(It.IsIn<Exam>(retrievedExams)), Times.Once);
        }

        [Test]
        public void GetExams_ShouldReturnBadRequestOnInvalidCourseId()
        {
            //Arrange
            int? invalidCourseId = _random.NextZeroOrNegative();

            //Act
            var badRequestResult = _controller.GetExams(invalidCourseId).Result as BadRequestObjectResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.InstanceOf<ErrorModel>());
        }

        [Test]
        public void GetExam_ShouldReturnOkWhenExamExists()
        {
            //Arrange
            var examId = _random.NextPositive();
            var existingExam = new ExamBuilder().WithId(examId).Build();
            var convertedExam = new ExamOutputModel();

            _examServiceMock.Setup(service => service.GetExamAsync(It.IsAny<int>()))
                .ReturnsAsync(existingExam);

            _mapperMock.Setup(m => m.Map<ExamOutputModel>(It.IsAny<Exam>())).Returns(convertedExam);

            //Act
            var actionResult = _controller.GetExam(examId).Result as OkObjectResult;

            //Assert
            _examServiceMock.Verify(service => service.GetExamAsync(examId), Times.Once);
            _mapperMock.Verify(m => m.Map<ExamOutputModel>(existingExam), Times.Once);
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.SameAs(convertedExam));
        }

        [Test]
        public void GetExam_ShouldReturnNotFoundWhenExamDoesNotExist()
        {
            //Arrange
            var examId = _random.NextPositive();

            _examServiceMock.Setup(service => service.GetExamAsync(It.IsAny<int>()))
                .Throws<DataNotFoundException>();


            //Act
            var actionResult = _controller.GetExam(examId).Result as NotFoundResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _examServiceMock.Verify(service => service.GetExamAsync(examId), Times.Once);
            _mapperMock.Verify(m => m.Map<ExamOutputModel>(It.IsAny<Exam>()), Times.Never);
        }

        [Test]
        public void PostExam_ShouldReturnCreatedWhenInputIsValid()
        {
            //Arrange
            var inputModel = new ExamCreationModel
            {
                CourseId = _random.NextPositive(),
                Name = _random.NextString()
            };
            var createdExam = new ExamBuilder().WithId(_random.NextPositive()).Build();
            _examServiceMock.Setup(service => service.CreateExamAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(createdExam);
            var convertedExam = new ExamOutputModel { Id = createdExam.Id };
            _mapperMock.Setup(m => m.Map<ExamOutputModel>(It.IsAny<Exam>())).Returns(convertedExam);

            //Act
            var actionResult = _controller.PostExam(inputModel).Result as CreatedAtActionResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _examServiceMock.Verify(service => service.CreateExamAsync(inputModel.CourseId, inputModel.Name), Times.Once);
            _mapperMock.Verify(m => m.Map<ExamOutputModel>(createdExam), Times.Once);

            Assert.That(actionResult.Value, Is.SameAs(convertedExam));
            Assert.That(actionResult.ActionName, Is.EqualTo(nameof(ExamController.GetExam)));
            Assert.That(actionResult.RouteValues["id"], Is.EqualTo(convertedExam.Id));
        }

        [Test]
        public void PostExam_ShouldReturnBadRequestWhenInputIsValid()
        {
            //Arrange
            var inputModel = new ExamCreationModel
            {
                CourseId = -1,
                Name = ""
            };

            var contractException = new ContractException(_random.NextString());
            _examServiceMock.Setup(service => service.CreateExamAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Throws(contractException);

            //Act
            var actionResult = _controller.PostExam(inputModel).Result as BadRequestObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _examServiceMock.Verify(service => service.CreateExamAsync(inputModel.CourseId, inputModel.Name), Times.Once);
            _mapperMock.Verify(m => m.Map<ExamOutputModel>(It.IsAny<Exam>()), Times.Never);
            var errorModel = actionResult.Value as ErrorModel;
            Assert.That(errorModel, Is.Not.Null);
            Assert.That(errorModel.Message, Is.EqualTo(contractException.Message));
        }

        [Test]
        public void PostExam_ShouldReturnForbidResultForStudents()
        {
            //Arrange
            _controller = CreateControllerWithUserInContext(Role.Constants.Student);

            var inputModel = new ExamCreationModel
            {
                CourseId = _random.NextPositive(),
                Name = _random.NextString()
            };

            //Act
            var forbidResult = _controller.PostExam(inputModel).Result as ForbidResult;

            //Assert
            Assert.That(forbidResult, Is.Not.Null);
        }

        [Test]
        public void GetExamPart_ShouldReturnAnExamPartOutputModel()
        {
            //Arrange
            int examId = _random.NextPositive();
            IExamPart examPart1 = new ExamPartBuilder().WithId().Build();
            IExamPart examPart2 = new ExamPartBuilder().WithId().Build();
            var examParts = new List<IExamPart> { examPart1, examPart2 };

            var examMock = new Mock<IExam>();
            examMock.SetupGet(exam => exam.Parts).Returns(examParts);

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();

            var convertedExamPart = new ExamPartOutputModel();
            _mapperMock.Setup(m => m.Map<ExamPartOutputModel>(examPart2)).Returns(convertedExamPart).Verifiable();

            //Act
            var okObjectResult = _controller.GetExamPart(examId, examPart2.Id).Result as OkObjectResult;

            //Assert
            Assert.That(okObjectResult, Is.Not.Null);
            Assert.That(okObjectResult.Value, Is.SameAs(convertedExamPart));
            _examServiceMock.Verify();
            _mapperMock.Verify();
        }

        [Test]
        public void GetExamPart_ShouldReturnNotFoundResultWhenExamDoesNotExist()
        {
            //Arrange
            int examId = _random.NextPositive();
            int examPartId = _random.NextPositive();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).Throws<DataNotFoundException>().Verifiable();

            //Act
            var notFoundResult = _controller.GetExamPart(examId, examPartId).Result as NotFoundResult;

            //Assert
            Assert.That(notFoundResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        [Test]
        public void GetExamPart_ShouldReturnNotFoundResultWhenExamPartDoesNotExist()
        {
            //Arrange
            int examId = _random.NextPositive();
            IExamPart examPart1 = new ExamPartBuilder().WithId().Build();
            IExamPart examPart2 = new ExamPartBuilder().WithId().Build();
            var examParts = new List<IExamPart> { examPart1, examPart2 };

            var examMock = new Mock<IExam>();
            examMock.SetupGet(exam => exam.Parts).Returns(examParts);

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();

            int nonExistingExamPartId = Math.Max(examPart1.Id, examPart2.Id) + 1;

            //Act
            var notFoundResult = _controller.GetExamPart(examId, nonExistingExamPartId).Result as NotFoundResult;

            //Assert
            Assert.That(notFoundResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        [Test]
        public void PostExamPart_ShouldReturnCreatedWhenInputIsValid()
        {
            //Arrange
            int examId = _random.NextPositive();
            var inputModel = new ExamPartDto();

            var createdExamPart = new ExamPartBuilder().WithId().Build();
            _examServiceMock.Setup(service => service.CreateExamPartAsync(examId, inputModel))
                .ReturnsAsync(createdExamPart);
            var convertedExamPart = new ExamPartOutputModel { Id = createdExamPart.Id };
            _mapperMock.Setup(m => m.Map<ExamPartOutputModel>(createdExamPart)).Returns(convertedExamPart);

            //Act
            var actionResult = _controller.PostExamPart(examId, inputModel).Result as CreatedAtActionResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _examServiceMock.Verify();
            _mapperMock.Verify();

            Assert.That(actionResult.Value, Is.SameAs(convertedExamPart));
            Assert.That(actionResult.ActionName, Is.EqualTo(nameof(ExamController.GetExamPart)));
            Assert.That(actionResult.RouteValues["id"], Is.EqualTo(examId));
            Assert.That(actionResult.RouteValues["examPartId"], Is.EqualTo(convertedExamPart.Id));
        }

        [Test]
        public void PostExamPart_ShouldReturnBadRequestResultWhenInputIsInvalid()
        {
            //Arrange
            int examId = _random.NextPositive();
            var inputModel = new ExamPartDto();

            _examServiceMock.Setup(service => service.CreateExamPartAsync(examId, inputModel))
                .Throws<ContractException>();

            //Act
            var badRequestObjectResult = _controller.PostExamPart(examId, inputModel).Result as BadRequestObjectResult;

            //Assert
            Assert.That(badRequestObjectResult, Is.Not.Null);
            Assert.That(badRequestObjectResult.Value, Is.TypeOf<ErrorModel>());
            _examServiceMock.Verify();
        }

        [Test]
        public void PostExamPart_ShouldReturnForbidResultForStudents()
        {
            //Arrange
            _controller = CreateControllerWithUserInContext(Role.Constants.Student);
            int examId = _random.NextPositive();
            var inputModel = new ExamPartDto();

            //Act
            var forbidResult = _controller.PostExamPart(examId, inputModel).Result as ForbidResult;

            //Assert
            Assert.That(forbidResult, Is.Not.Null);
        }

        [Test]
        public void DeleteExamPart_ShouldReturnOkWhenOperationSucceeds()
        {
            //Arrange
            int examId = _random.NextPositive();
            int examPartId = _random.NextPositive();

            var examMock = new Mock<IExam>();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();
            _examServiceMock.Setup(service => service.DeleteExamPartAsync(examMock.Object, examPartId)).Verifiable();

            //Act
            var okResult = _controller.DeleteExamPart(examId, examPartId).Result as OkResult;

            //Assert
            Assert.That(okResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        [Test]
        public void DeleteExamPart_ReturnBadRequestWhenExamPartDoesNotExist()
        {
            //Arrange
            int examId = _random.NextPositive();
            int examPartId = _random.NextPositive();

            var examMock = new Mock<IExam>();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();
            _examServiceMock.Setup(service => service.DeleteExamPartAsync(examMock.Object, examPartId))
                .Throws<ContractException>();

            //Act
            var badRequestResult = _controller.DeleteExamPart(examId, examPartId).Result as BadRequestObjectResult;

            //Assert
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.TypeOf<ErrorModel>());
        }

        [Test]
        public void DeleteExamPart_ShouldReturnForbidResultForStudents()
        {
            //Arrange
            _controller = CreateControllerWithUserInContext(Role.Constants.Student);
            int examId = _random.NextPositive();
            int examPartId = _random.NextPositive();

            //Act
            var forbidResult = _controller.DeleteExamPart(examId, examPartId).Result as ForbidResult;

            //Assert
            Assert.That(forbidResult, Is.Not.Null);
        }

        [Test]
        public void DownloadExamScores_ShouldReturnFileStreamResultOfExamScores()
        {
            //Arrange
            int examId = _random.NextPositive();
            string examName = _random.NextString();
            string expectedFileName = $"{examName.ToValidFilePath()}.csv";

            var examMock = new Mock<IExam>();
            examMock.SetupGet(exam => exam.Name).Returns(examName);

            var returnedCsvObjects = new List<ExpandoObject>()
            {
                new ExpandoObject()
            };

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();
            _examServiceMock.Setup(service => service.CalculateExamScoresForCsvAsync(examId))
                .ReturnsAsync(returnedCsvObjects).Verifiable();

            //Act
            var fileStreamResult = _controller.DownloadExamScores(examId).Result as FileStreamResult;

            //Assert
            Assert.That(fileStreamResult, Is.Not.Null);
            _examServiceMock.Verify();
            Assert.That(fileStreamResult.FileDownloadName, Is.EqualTo(expectedFileName));
            Assert.That(fileStreamResult.ContentType, Is.EqualTo("text/csv"));
        }

        [Test]
        public void DownloadExamScores_ShouldReturnForbidResultForStudents()
        {
            //Arrange
            _controller = CreateControllerWithUserInContext(Role.Constants.Student);

            int examId = _random.NextPositive();

            //Act
            var forbidResult = _controller.DownloadExamScores(examId).Result as ForbidResult;

            //Assert
            Assert.That(forbidResult, Is.Not.Null);
        }

        [Test]
        public void DownloadExamScores_ShouldReturnNotFoundResultWhenExamDoesNotExist()
        {
            //Arrange
            _controller = CreateControllerWithUserInContext(Role.Constants.Lector);

            int examId = _random.NextPositive();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).Throws<DataNotFoundException>().Verifiable();

            //Act
            var notFoundResult = _controller.DownloadExamScores(examId).Result as NotFoundResult;

            //Assert
            Assert.That(notFoundResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        private ExamController CreateControllerWithUserInContext(string role)
        {
            _userId = _random.Next(1, int.MaxValue);
            return new ExamController(_examServiceMock.Object, _mapperMock.Object)
            {
                ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
            };
        }
    }
}