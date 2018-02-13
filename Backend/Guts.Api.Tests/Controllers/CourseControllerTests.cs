using System;
using System.Collections.Generic;
using System.Xml.Serialization;
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
    public class CourseControllerTests
    {
        private Mock<ICourseService> _courseServiceMock;
        private CourseController _controller;

        [SetUp]
        public void Setup()
        {
            _courseServiceMock = new Mock<ICourseService>();
            _controller = new CourseController(_courseServiceMock.Object);
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
    
    }
}
