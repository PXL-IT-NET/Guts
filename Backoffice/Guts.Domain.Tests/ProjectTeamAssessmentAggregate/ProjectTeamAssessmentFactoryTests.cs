using NUnit.Framework;
using System;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Moq;

namespace Guts.Domain.Tests.ProjectTeamAssessmentAggregate
{
    public class ProjectTeamAssessmentFactoryTests
    {
        private ProjectTeamAssessment.Factory _factory;
      
        private ProjectAssessmentMockBuilder _projectAssessmentMockBuilder;
        private Mock<IProjectAssessment> _projectAssessmentMock;

        private ProjectTeamMockBuilder _projectTeamMockBuilder;
        private Mock<IProjectTeam> _projectTeamMock;
        
        [SetUp]
        public void BeforeEachTest()
        {
            _projectAssessmentMockBuilder = new ProjectAssessmentMockBuilder();
            _projectAssessmentMock = _projectAssessmentMockBuilder.Build();

            _projectTeamMockBuilder = new ProjectTeamMockBuilder();
            _projectTeamMock = _projectTeamMockBuilder.Build();

            _factory = new ProjectTeamAssessment.Factory(null);
        }

        [Test]
        public void CreateNew_ValidInput_ShouldInitializeCorrectly()
        {
            //Arrange
            DateTime openOnUtc = DateTime.UtcNow.AddDays(-1);
            _projectAssessmentMockBuilder.WithId().WithOpenOn(openOnUtc);

            _projectTeamMockBuilder.WithMembers(4);

            //Act
            IProjectTeamAssessment result = _factory.CreateNew(_projectAssessmentMock.Object, _projectTeamMock.Object);

            //Assert
            Assert.That(result.ProjectAssessment, Is.EqualTo(_projectAssessmentMock.Object));
            Assert.That(result.Team, Is.EqualTo(_projectTeamMock.Object));
            Assert.That(result.PeerAssessments, Has.Count.Zero);
        }

        [Test]
        public void CreateNew_ProjectAssessmentNotOpenYet_ShouldThrowContractException()
        {
            //Arrange
            DateTime openOnUtc = DateTime.UtcNow.AddDays(1);
            _projectAssessmentMockBuilder.WithId().WithOpenOn(openOnUtc);

            _projectTeamMockBuilder.WithMembers(4);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(_projectAssessmentMock.Object, _projectTeamMock.Object),
                Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_NoTeamProvided_ShouldThrowContractException()
        {
            //Arrange
            DateTime openOnUtc = DateTime.UtcNow.AddDays(-1);
            _projectAssessmentMockBuilder.WithId().WithOpenOn(openOnUtc);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(_projectAssessmentMock.Object, null),
                Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_TeamHasNoMembers_ShouldThrowContractException()
        {
            //Arrange
            DateTime openOnUtc = DateTime.UtcNow.AddDays(-1);
            _projectAssessmentMockBuilder.WithId().WithOpenOn(openOnUtc);

            _projectTeamMockBuilder.WithMembers(0);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(_projectAssessmentMock.Object, _projectTeamMock.Object),
                Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_NoProjectAssessmentProvided_ShouldThrowContractException()
        {
            //Arrange
            _projectTeamMockBuilder.WithMembers(4);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(null, _projectTeamMock.Object),
                Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_NewUnsavedProjectAssessment_ShouldThrowContractException()
        {
            //Arrange
            DateTime openOnUtc = DateTime.UtcNow.AddDays(-1);
            _projectAssessmentMockBuilder.WithOpenOn(openOnUtc);

            _projectTeamMockBuilder.WithMembers(4);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(_projectAssessmentMock.Object, _projectTeamMock.Object),
                Throws.InstanceOf<ContractException>());
        }
    }
}
