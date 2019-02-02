using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class ChapterConverterTests
    {
        private ChapterConverter _converter;

        [SetUp]
        public void Setup()
        {
            var userConverterMock = new Mock<IUserConverter>();
            _converter = new ChapterConverter(userConverterMock.Object);
        }

        [Test]
        public void ToTopicModel_ShouldCorrectlyCovertValidChapter()
        {
            //Arrange
            var chapter = new ChapterBuilder().WithId().Build();

            //Act
            var model = _converter.ToTopicModel(chapter);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Code, Is.EqualTo(chapter.Code));
            Assert.That(model.Description, Is.EqualTo(chapter.Description));
        }
    }
}
