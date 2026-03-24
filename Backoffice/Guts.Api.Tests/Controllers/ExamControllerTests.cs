using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
        private Mock<IExamService> _examServiceMock;
        private Mock<IMapper> _mapperMock;
        private int _userId;

        [SetUp]
        public void Setup()
        {
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
            int courseId = Random.Shared.NextPositive();
            int periodId = Random.Shared.NextPositive();

            var retrievedExams = new List<Exam>
            {
                new ExamBuilder().Build()
            };

            _examServiceMock.Setup(service => service.GetExamsAsync(courseId, periodId))
                .ReturnsAsync(retrievedExams).Verifiable();

            _mapperMock.Setup(m => m.MapToExamOutputModel(It.IsAny<IExam>())).Returns(new ExamOutputModel());

            var actionResult = _controller.GetExams(courseId, periodId).Result as OkObjectResult;

            Assert.That(actionResult, Is.Not.Null);
            var results = actionResult.Value as IEnumerable<ExamOutputModel>;
            Assert.That(results.Count(), Is.EqualTo(retrievedExams.Count));
            _examServiceMock.Verify();
            _mapperMock.Verify(m => m.MapToExamOutputModel(It.IsAny<IExam>()), Times.Exactly(retrievedExams.Count));
        }

        [Test]
        public void GetExams_ShouldReturnBadRequestOnInvalidCourseId()
        {
            int invalidCourseId = Random.Shared.NextZeroOrNegative();

            var badRequestResult = _controller.GetExams(invalidCourseId).Result as BadRequestObjectResult;

            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.InstanceOf<ErrorModel>());
        }

        [Test]
        public void GetExam_ShouldReturnOkWhenExamExists()
        {
            var examId = Random.Shared.NextPositive();
            var existingExam = new ExamBuilder().WithId(examId).Build();
            var convertedExam = new ExamOutputModel();

            _examServiceMock.Setup(service => service.GetExamAsync(It.IsAny<int>()))
                .ReturnsAsync(existingExam);

            _mapperMock.Setup(m => m.MapToExamOutputModel(It.IsAny<IExam>())).Returns(convertedExam);

            var actionResult = _controller.GetExam(examId).Result as OkObjectResult;

            _examServiceMock.Verify(service => service.GetExamAsync(examId), Times.Once);
            _mapperMock.Verify(m => m.MapToExamOutputModel(existingExam), Times.Once);
            Assert.That(actionResult, Is.Not.Null);
            Assert.That(actionResult.Value, Is.SameAs(convertedExam));
        }

        [Test]
        public void GetExam_ShouldReturnNotFoundWhenExamDoesNotExist()
        {
            var examId = Random.Shared.NextPositive();

            _examServiceMock.Setup(service => service.GetExamAsync(It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var actionResult = _controller.GetExam(examId).Result as NotFoundResult;

            Assert.That(actionResult, Is.Not.Null);
            _examServiceMock.Verify(service => service.GetExamAsync(examId), Times.Once);
            _mapperMock.Verify(m => m.MapToExamOutputModel(It.IsAny<IExam>()), Times.Never);
        }

        [Test]
        public void PostExam_ShouldReturnCreatedWhenInputIsValid()
        {
            var inputModel = new ExamCreationModel
            {
                CourseId = Random.Shared.NextPositive(),
                Name = Random.Shared.NextString()
            };
            var createdExam = new ExamBuilder().WithId(Random.Shared.NextPositive()).Build();
            _examServiceMock.Setup(service => service.CreateExamAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(createdExam);
            var convertedExam = new ExamOutputModel { Id = createdExam.Id };
            _mapperMock.Setup(m => m.MapToExamOutputModel(It.IsAny<IExam>())).Returns(convertedExam);

            var actionResult = _controller.PostExam(inputModel).Result as CreatedAtActionResult;

            Assert.That(actionResult, Is.Not.Null);
            _examServiceMock.Verify(service => service.CreateExamAsync(inputModel.CourseId, inputModel.Name), Times.Once);
            _mapperMock.Verify(m => m.MapToExamOutputModel(createdExam), Times.Once);

            Assert.That(actionResult.Value, Is.SameAs(convertedExam));
            Assert.That(actionResult.ActionName, Is.EqualTo(nameof(ExamController.GetExam)));
            Assert.That(actionResult.RouteValues["id"], Is.EqualTo(convertedExam.Id));
        }

        [Test]
        public void PostExam_ShouldReturnBadRequestWhenInputIsValid()
        {
            var inputModel = new ExamCreationModel
            {
                CourseId = -1,
                Name = ""
            };

            var contractException = new ContractException(Random.Shared.NextString());
            _examServiceMock.Setup(service => service.CreateExamAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Throws(contractException);

            var actionResult = _controller.PostExam(inputModel).Result as BadRequestObjectResult;

            Assert.That(actionResult, Is.Not.Null);
            _examServiceMock.Verify(service => service.CreateExamAsync(inputModel.CourseId, inputModel.Name), Times.Once);
            _mapperMock.Verify(m => m.MapToExamOutputModel(It.IsAny<IExam>()), Times.Never);
            var errorModel = actionResult.Value as ErrorModel;
            Assert.That(errorModel, Is.Not.Null);
            Assert.That(errorModel.Message, Is.EqualTo(contractException.Message));
        }

        [Test]
        public void GetExamPart_ShouldReturnAnExamPartOutputModel()
        {
            int examId = Random.Shared.NextPositive();
            IExamPart examPart1 = new ExamPartBuilder().WithId().Build();
            IExamPart examPart2 = new ExamPartBuilder().WithId().Build();
            var examParts = new List<IExamPart> { examPart1, examPart2 };

            var examMock = new Mock<IExam>();
            examMock.SetupGet(exam => exam.Parts).Returns(examParts);

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();

            var convertedExamPart = new ExamPartOutputModel();
            _mapperMock.Setup(m => m.MapToExamPartOutputModel(examPart2)).Returns(convertedExamPart).Verifiable();

            var okObjectResult = _controller.GetExamPart(examId, examPart2.Id).Result as OkObjectResult;

            Assert.That(okObjectResult, Is.Not.Null);
            Assert.That(okObjectResult.Value, Is.SameAs(convertedExamPart));
            _examServiceMock.Verify();
            _mapperMock.Verify();
        }

        [Test]
        public void GetExamPart_ShouldReturnNotFoundResultWhenExamDoesNotExist()
        {
            int examId = Random.Shared.NextPositive();
            int examPartId = Random.Shared.NextPositive();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).Throws<DataNotFoundException>().Verifiable();

            var notFoundResult = _controller.GetExamPart(examId, examPartId).Result as NotFoundResult;

            Assert.That(notFoundResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        [Test]
        public void GetExamPart_ShouldReturnNotFoundResultWhenExamPartDoesNotExist()
        {
            int examId = Random.Shared.NextPositive();
            IExamPart examPart1 = new ExamPartBuilder().WithId().Build();
            IExamPart examPart2 = new ExamPartBuilder().WithId().Build();
            var examParts = new List<IExamPart> { examPart1, examPart2 };

            var examMock = new Mock<IExam>();
            examMock.SetupGet(exam => exam.Parts).Returns(examParts);

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();

            int nonExistingExamPartId = Math.Max(examPart1.Id, examPart2.Id) + 1;

            var notFoundResult = _controller.GetExamPart(examId, nonExistingExamPartId).Result as NotFoundResult;

            Assert.That(notFoundResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        [Test]
        public void PostExamPart_ShouldReturnCreatedWhenInputIsValid()
        {
            int examId = Random.Shared.NextPositive();
            var inputModel = new ExamPartDto();

            var createdExamPart = new ExamPartBuilder().WithId().Build();
            _examServiceMock.Setup(service => service.CreateExamPartAsync(examId, inputModel))
                .ReturnsAsync(createdExamPart);
            var convertedExamPart = new ExamPartOutputModel { Id = createdExamPart.Id };
            _mapperMock.Setup(m => m.MapToExamPartOutputModel(createdExamPart)).Returns(convertedExamPart);

            var actionResult = _controller.PostExamPart(examId, inputModel).Result as CreatedAtActionResult;

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
            int examId = Random.Shared.NextPositive();
            var inputModel = new ExamPartDto();

            _examServiceMock.Setup(service => service.CreateExamPartAsync(examId, inputModel))
                .Throws<ContractException>();

            var badRequestObjectResult = _controller.PostExamPart(examId, inputModel).Result as BadRequestObjectResult;

            Assert.That(badRequestObjectResult, Is.Not.Null);
            Assert.That(badRequestObjectResult.Value, Is.TypeOf<ErrorModel>());
            _examServiceMock.Verify();
        }

        [Test]
        public void DeleteExamPart_ShouldReturnOkWhenOperationSucceeds()
        {
            int examId = Random.Shared.NextPositive();
            int examPartId = Random.Shared.NextPositive();

            var examMock = new Mock<IExam>();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();
            _examServiceMock.Setup(service => service.DeleteExamPartAsync(examMock.Object, examPartId)).Verifiable();

            var okResult = _controller.DeleteExamPart(examId, examPartId).Result as OkResult;

            Assert.That(okResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        [Test]
        public void DeleteExamPart_ReturnBadRequestWhenExamPartDoesNotExist()
        {
            int examId = Random.Shared.NextPositive();
            int examPartId = Random.Shared.NextPositive();

            var examMock = new Mock<IExam>();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();
            _examServiceMock.Setup(service => service.DeleteExamPartAsync(examMock.Object, examPartId))
                .Throws<ContractException>();

            var badRequestResult = _controller.DeleteExamPart(examId, examPartId).Result as BadRequestObjectResult;

            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.TypeOf<ErrorModel>());
        }

        [Test]
        public void DownloadExamScores_ShouldReturnFileStreamResultOfExamScores()
        {
            int examId = Random.Shared.NextPositive();
            string examName = Random.Shared.NextString();
            string expectedFileName = $"{examName.ToValidFilePath()}.csv";

            var examMock = new Mock<IExam>();
            examMock.SetupGet(exam => exam.Name).Returns(examName);

            var returnedCsvObjects = new List<ExpandoObject>
            {
                new ExpandoObject()
            };

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).ReturnsAsync(examMock.Object).Verifiable();
            _examServiceMock.Setup(service => service.CalculateExamScoresForCsvAsync(examId))
                .ReturnsAsync(returnedCsvObjects).Verifiable();

            var fileStreamResult = _controller.DownloadExamScores(examId).Result as FileStreamResult;

            Assert.That(fileStreamResult, Is.Not.Null);
            _examServiceMock.Verify();
            Assert.That(fileStreamResult.FileDownloadName, Is.EqualTo(expectedFileName));
            Assert.That(fileStreamResult.ContentType, Is.EqualTo("text/csv"));
        }

        [Test]
        public void DownloadExamScores_ShouldReturnNotFoundResultWhenExamDoesNotExist()
        {
            _controller = CreateControllerWithUserInContext(Role.Constants.Lector);

            int examId = Random.Shared.NextPositive();

            _examServiceMock.Setup(service => service.GetExamAsync(examId)).Throws<DataNotFoundException>().Verifiable();

            var notFoundResult = _controller.DownloadExamScores(examId).Result as NotFoundResult;

            Assert.That(notFoundResult, Is.Not.Null);
            _examServiceMock.Verify();
        }

        private ExamController CreateControllerWithUserInContext(string role)
        {
            _userId = Random.Shared.Next(1, int.MaxValue);
            return new ExamController(_examServiceMock.Object, _mapperMock.Object)
            {
                ControllerContext = new ControllerContextBuilder().WithUser(_userId.ToString()).WithRole(role).Build()
            };
        }
    }
}
