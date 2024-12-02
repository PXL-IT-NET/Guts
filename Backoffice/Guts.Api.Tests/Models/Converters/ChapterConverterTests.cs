using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Guts.Api.Models;
using Guts.Api.Models.AssignmentModels;
using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class ChapterConverterTests
    {
        private ChapterConverter _converter = null!;
        private Mock<IUserConverter> _userConverterMock = null!;

        [SetUp]
        public void Setup()
        {
            _userConverterMock = new Mock<IUserConverter>();
            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<AssignmentModel>(It.IsAny<Assignment>()))
                .Returns<Assignment>(a => new AssignmentModel
                {
                    AssignmentId = a.Id,
                    Code = a.Code,
                    Description = a.Description
                });
            _converter = new ChapterConverter(_userConverterMock.Object, mapperMock.Object);
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
            Assert.That(model.Code, Is.EqualTo(chapter.Code.Value));
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

        [Test]
        public void ToChapterDetailModel_ShouldCorrectlyCovertValidChapter()
        {
            //Arrange
            Chapter chapter = new ChapterBuilder().WithId().WithAssignments(2, 2).Build();
            var chapterUsers = new List<User>
            {
                new UserBuilder().WithId().Build(),
                new UserBuilder().WithId().Build(),
            };

            //Act
            ChapterDetailModel model = _converter.ToChapterDetailModel(chapter, chapterUsers);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(chapter.Id));
            Assert.That(model.Code, Is.EqualTo(chapter.Code.Value));
            Assert.That(model.Description, Is.EqualTo(chapter.Description));
            Assert.That(model.Exercises.Count, Is.EqualTo(chapter.Assignments.Count));
            foreach (Assignment assignment in chapter.Assignments)
            {
                AssignmentModel assignmentModel = model.Exercises.FirstOrDefault(a => a.AssignmentId == assignment.Id);
                Assert.That(assignmentModel, Is.Not.Null);
                Assert.That(assignmentModel.Code, Is.EqualTo(assignment.Code));
                Assert.That(assignmentModel.Description, Is.EqualTo(assignment.Description));
                Assert.That(assignmentModel.Tests.Count, Is.EqualTo(assignment.Tests.Count));
                foreach (Test test in assignment.Tests)
                {
                    TestModel testModel = assignmentModel.Tests.FirstOrDefault(t => t.Id == test.Id);
                    Assert.That(testModel, Is.Not.Null);
                    Assert.That(testModel.TestName, Is.EqualTo(test.TestName));
                }
            }
            Assert.That(model.Users.Count, Is.EqualTo(chapterUsers.Count));
            foreach (User user in chapterUsers)
            {
                _userConverterMock.Verify(uc => uc.FromUser(user), Times.Once);
            }
        }
    }
}
