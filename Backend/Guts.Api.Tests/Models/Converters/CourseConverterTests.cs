using System;
using System.Collections.Generic;
using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using Guts.Domain;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class CourseConverterTests
    {
        private CourseConverter _converter;
        private Mock<IChapterConverter> _chapterConverter;
        private Mock<IProjectConverter> _projectConverter;

        [SetUp]
        public void Setup()
        {
            _chapterConverter = new Mock<IChapterConverter>();
            _projectConverter = new Mock<IProjectConverter>();
            _converter = new CourseConverter(_chapterConverter.Object, _projectConverter.Object);
        }

        [Test]
        public void ToCourseContentsModel_ShouldCorrectlyConvertValidData()
        {
            //Arrange
            var course = new CourseBuilder().WithId().Build();
            var chapters = new List<Chapter>
            {
                new ChapterBuilder().Build(),
                new ChapterBuilder().Build()
            };

            var projects = new List<Project>
            {
                new ProjectBuilder().Build(),
                new ProjectBuilder().Build()
            };

            //Act
            var model = _converter.ToCourseContentsModel(course, chapters, projects);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(course.Id));
            Assert.That(model.Code, Is.EqualTo(course.Code));
            Assert.That(model.Name, Is.EqualTo(course.Name));

            Assert.That(model.Chapters, Is.Not.Null);
            Assert.That(model.Chapters, Has.Count.EqualTo(chapters.Count));
            foreach (var chapter in chapters)
            {
                _chapterConverter.Verify(converter => converter.ToTopicModel(chapter), Times.Once);
            }

            Assert.That(model.Projects, Is.Not.Null);
            Assert.That(model.Projects, Has.Count.EqualTo(projects.Count));
            foreach (var project in projects)
            {
                _projectConverter.Verify(converter => converter.ToTopicModel(project), Times.Once);
            }
        }

        [Test]
        public void ToChapterSummaryModel_ShouldThrowArgumentExceptionWhenCourseIsMissing()
        {
            //Arrange
            var chapters = new List<Chapter>();
            var projects = new List<Project>();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(null, chapters, projects), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterSummaryModel_ShouldThrowArgumentExceptionWhenChaptersAreMissing()
        {
            //Arrange
            var course = new CourseBuilder().WithId().Build();
            var projects = new List<Project>();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(course, null, projects), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterSummaryModel_ShouldThrowArgumentExceptionWhenProjectsAreMissing()
        {
            //Arrange
            var course = new CourseBuilder().WithId().Build();
            var chapters = new List<Chapter>();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(course, chapters, null), Throws.InstanceOf<ArgumentException>());
        }
    }
}
