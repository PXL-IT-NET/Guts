using System.Linq;
using AutoMapper;
using Guts.Api.Models;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
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
            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<AssignmentModel>(It.IsAny<Assignment>()))
                .Returns<Assignment>(a => new AssignmentModel
                {
                    AssignmentId = a.Id,
                    Code = a.Code,
                    Description = a.Description
                });
            _converter = new ChapterConverter(userConverterMock.Object, mapperMock.Object);
        }

        [Test]
        public void ToTopicModel_ShouldCorrectlyCovertValidChapter()
        {
            //Arrange
            Chapter chapter = new ChapterBuilder().WithId().WithAssignments(2,0).Build();

            //Act
            TopicModel model = _converter.ToTopicModel(chapter);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Code, Is.EqualTo(chapter.Code));
            Assert.That(model.Description, Is.EqualTo(chapter.Description));
            Assert.That(model.Assignments.Count, Is.EqualTo(chapter.Assignments.Count));
            foreach (Assignment assignment in chapter.Assignments)
            {
                AssignmentModel assignmentModel = model.Assignments.FirstOrDefault(a => a.AssignmentId == assignment.Id);
                Assert.That(assignmentModel, Is.Not.Null);
                Assert.That(assignmentModel.Code, Is.EqualTo(assignment.Code));
                Assert.That(assignmentModel.Description, Is.EqualTo(assignment.Description));
            }
        }
    }
}
