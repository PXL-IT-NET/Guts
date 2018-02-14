using System;
using System.Collections.Generic;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
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
    public class CourseControllerTests
    {
        private CourseController _controller;
        private Mock<ICourseService> _courseServiceMock;
        private Mock<IChapterService> _chapterServiceMock;
        private Mock<ICourseConverter> _courseConverterMock;
        private Random _random;


        [SetUp]
        public void Setup()
        {
            _courseServiceMock = new Mock<ICourseService>();
            _chapterServiceMock = new Mock<IChapterService>();
            _courseConverterMock = new Mock<ICourseConverter>();
            _controller = new CourseController(_courseServiceMock.Object, _chapterServiceMock.Object, _courseConverterMock.Object);
            _random = new Random();
        }

        [Test]
        public void ShouldInheritFromControllerBase()
        {
            Assert.That(_controller, Is.InstanceOf<ControllerBase>());
        }

        [Test]
        public void GetCoursesShouldReturnTheCoursesReturnedByCourseService()
        {
            //Arrange
            var allCourses = new List<Course>();
            _courseServiceMock.Setup(service => service.GetAllCoursesAsync()).ReturnsAsync(allCourses);

            //Act
            var actionResult = _controller.GetCourses().Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _courseServiceMock.Verify(service => service.GetAllCoursesAsync(), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(allCourses));
        }

        [Test]
        public void GetCourseContentsShouldReturnBadRequestForInvalidCourseId()
        {
            //Arrange
            var courseId = -1;

            //Act
            var actionResult = _controller.GetCourseContents(courseId).Result as BadRequestResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
        }

        [Test]
        public void GetCourseContentsShouldGetTheChaptersFromTheRepositoryAndConvertThemToModels()
        {
            //Arrange
            var existingCourse = new CourseBuilder().WithId().Build();
            var existingChapters = new List<Chapter>
            {
                new ChapterBuilder().Build(),
                new ChapterBuilder().Build(),
            };
            var convertedCourse = new CourseContentsModel();

            _chapterServiceMock.Setup(service => service.GetChaptersOfCourseAsync(It.IsAny<int>())).ReturnsAsync(existingChapters);
            var courseId = _random.NextPositive();
            _courseServiceMock.Setup(service => service.GetCourseByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(existingCourse);
            _courseConverterMock
                .Setup(converter => converter.ToCourseContentsModel(It.IsAny<Course>(), It.IsAny<IList<Chapter>>()))
                .Returns(convertedCourse);

            //Act
            var actionResult = _controller.GetCourseContents(courseId).Result as OkObjectResult;

            //Assert
            Assert.That(actionResult, Is.Not.Null);
            _chapterServiceMock.Verify(service => service.GetChaptersOfCourseAsync(courseId), Times.Once);
            _courseConverterMock.Verify(converter => converter.ToCourseContentsModel(existingCourse, existingChapters), Times.Once);
            Assert.That(actionResult.Value, Is.EqualTo(convertedCourse));
        }

    }
}
