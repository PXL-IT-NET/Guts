using System;
using System.Collections.Generic;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Services;
using Guts.Common.Extensions;
using Guts.Data;
using Guts.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ControllerBase = Guts.Api.Controllers.ControllerBase;

namespace Guts.Api.Tests.Controllers
{
    [TestFixture]
    public class ChapterControllerTests
    {
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<IChapterConverter> _chapterConverterMock;
        private ChapterController _controller;
        private Random _random;

        [SetUp]
        public void Setup()
        {
            _chapterServiceMock = new Mock<IChapterService>();
            _chapterConverterMock = new Mock<IChapterConverter>();
            _controller = new ChapterController(_chapterServiceMock.Object, _chapterConverterMock.Object);
            _random = new Random();
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetChapterContentsShouldReturnContentsIfParamatersAreValid()
        {
            //Arrange
            var existingChapter = new Chapter();
            var chapterContents = new ChapterContentsModel();
            var exerciseResults = new List<ExerciseResultDto>();
            _chapterServiceMock.Setup(service => service.LoadChapterWithTestsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(existingChapter);
            _chapterServiceMock.Setup(service => service.GetResultsForUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(exerciseResults);
            _chapterConverterMock.Setup(converter => converter.ToChapterContentsModel(It.IsAny<Chapter>(), It.IsAny<IList<ExerciseResultDto>>()))
                .Returns(chapterContents);

            var courseId = _random.NextPositive();
            var chapter = _random.NextPositive();

            //Act
            var actionResult = _controller.GetChapterContents(courseId, chapter).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(courseId, chapter), Times.Once);
            _chapterConverterMock.Verify(converter => converter.ToChapterContentsModel(existingChapter, exerciseResults), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(chapterContents));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void GetChapterContentsShouldReturnBadRequestOnInvalidInput(int courseId, int chapterNumber)
        {
            //Act
            var actionResult = _controller.GetChapterContents(courseId, chapterNumber).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(courseId, chapterNumber), Times.Never);
        }

        [Test]
        public void GetChapterContentsShouldReturnNotFoundWhenServiceCannotFindChapter()
        {
            //Arrange
            _chapterServiceMock.Setup(service => service.LoadChapterWithTestsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Throws<DataNotFoundException>();

            var courseId = _random.NextPositive();
            var chapter = _random.NextPositive();

            //Act
            var actionResult = _controller.GetChapterContents(courseId, chapter).Result as NotFoundResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.LoadChapterWithTestsAsync(courseId, chapter), Times.Once);
        }
    }
}
