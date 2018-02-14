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

        [SetUp]
        public void Setup()
        {
            _chapterConverter = new Mock<IChapterConverter>();
            _converter = new CourseConverter(_chapterConverter.Object);
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

            //Act
            var model = _converter.ToCourseContentsModel(course, chapters);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(course.Id));
            Assert.That(model.Code, Is.EqualTo(course.Code));
            Assert.That(model.Name, Is.EqualTo(course.Name));

            Assert.That(model.Chapters, Is.Not.Null);
            Assert.That(model.Chapters, Has.Count.EqualTo(chapters.Count));

            foreach (var chapter in chapters)
            {
                _chapterConverter.Verify(converter => converter.ToChapterModel(chapter), Times.Once);
            }
        }

        [Test]
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenCourseIsMissing()
        {
            //Arrange
            var chapters = new List<Chapter>();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(null, chapters), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToChapterContentsModel_ShouldThrowArgumentExceptionWhenChaptersAreMissing()
        {
            //Arrange
            var course = new CourseBuilder().WithId().Build();

            //Act + Assert
            Assert.That(() => _converter.ToCourseContentsModel(course, null), Throws.InstanceOf<ArgumentException>());
        }
    }
}
