using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Business;
using Guts.Business.Services;
using Guts.Business.Services.Exam;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
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

        [SetUp]
        public void Setup()
        {
            _random = new Random();
            _examServiceMock = new Mock<IExamService>();
            _mapperMock = new Mock<IMapper>();
  
            _controller = new ExamController(_examServiceMock.Object, _mapperMock.Object);
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
            var convertedExam = new ExamOutputModel{ Id = createdExam.Id};
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
    }
}