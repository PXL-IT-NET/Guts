using System.ComponentModel;
using System.Linq;
using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class ProjectConverterTests
    {
        private ProjectConverter _converter;

        [SetUp]
        public void Setup()
        {
            _converter = new ProjectConverter();
        }

        [Test]
        public void ToProjectModel_ShouldCorrectlyCovertValidProject()
        {
            //Arrange
            var project = new ProjectBuilder().WithId().Build();

            //Act
            var model = _converter.ToProjectModel(project);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(project.Id));
            Assert.That(model.Code, Is.EqualTo(project.Code));
            Assert.That(model.Description, Is.EqualTo(project.Description));
        }

        [Test]
        public void ToProjectDetailModel_ShouldCorrectlyCovertValidProject()
        {
            //Arrange
            var project = new ProjectBuilder().WithId().WithAssignments(2).WithTeams(2).Build();

            //Act
            var model = _converter.ToProjectDetailModel(project);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(project.Id));
            Assert.That(model.Code, Is.EqualTo(project.Code));
            Assert.That(model.Description, Is.EqualTo(project.Description));

            Assert.That(model.Components.Count, Is.EqualTo(project.Assignments.Count));
            foreach (var assignment in project.Assignments)
            {
                Assert.That(
                    model.Components.Any(component =>
                        component.AssignmentId == assignment.Id && component.Code == assignment.Code), Is.True);
            }

            Assert.That(model.Teams.Count, Is.EqualTo(project.Teams.Count));
            foreach (var team in project.Teams)
            {
                Assert.That(
                    model.Teams.Any(t =>
                        t.Id == team.Id && t.Name == team.Name), Is.True);
            }
        }

        [Test]
        public void ToProjectDetailModel_ShouldThrowArgumentExceptionWhenAssignmentsAreNotLoaded()
        {
            //Arrange
            var project = new ProjectBuilder().WithId().WithoutAssignments().Build();

            //Act + Assert
            Assert.That(() => _converter.ToProjectDetailModel(project), Throws.ArgumentNullException);
        }

        [Test]
        public void ToProjectDetailModel_ShouldThrowArgumentExceptionWhenTeamsAreNotLoaded()
        {
            //Arrange
            var project = new ProjectBuilder().WithId().WithoutTeams().Build();

            //Act + Assert
            Assert.That(() => _converter.ToProjectDetailModel(project), Throws.ArgumentNullException);
        }
    }
}