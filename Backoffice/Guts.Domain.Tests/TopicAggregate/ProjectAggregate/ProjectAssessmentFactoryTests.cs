using NUnit.Framework;
using System;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Domain.Tests.TopicAggregate.ProjectAggregate
{
    public class ProjectAssessmentFactoryTests : DomainTestBase
    {
        private ProjectAssessment.Factory _factory;

        [SetUp]
        public void BeforeEachTest()
        {
            _factory = new ProjectAssessment.Factory();
        }

        [Test]
        public void CreateNew_ValidInput_ShouldInitializeCorrectly()
        {
            //Arrange
            int projectId = Random.NextPositive();
            string description = Random.NextString();
            DateTime openOnUtc = DateTime.UtcNow;
            DateTime deadlineUtc = openOnUtc.AddDays(15);

            //Act
            IProjectAssessment result = _factory.CreateNew(projectId, description, openOnUtc, deadlineUtc);

            //Assert
            Assert.That(result.ProjectId, Is.EqualTo(projectId));
            Assert.That(result.Description, Is.EqualTo(description));
            Assert.That(result.OpenOnUtc, Is.EqualTo(openOnUtc));
            Assert.That(result.DeadlineUtc, Is.EqualTo(deadlineUtc));
        }

        [Test]
        public void CreateNew_ProjectIdNotPositive_ShouldThrowContractException()
        {
            //Arrange
            int invalidProjectId = -1 * Random.NextPositive();
            string description = Random.NextString();
            DateTime openOnUtc = DateTime.UtcNow;
            DateTime deadlineUtc = openOnUtc.AddDays(15);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(invalidProjectId, description, openOnUtc, deadlineUtc), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_EmptyDescription_ShouldThrowContractException()
        {
            //Arrange
            int projectId = Random.NextPositive();
            string emptyDescription = string.Empty;
            DateTime openOnUtc = DateTime.UtcNow;
            DateTime deadlineUtc = openOnUtc.AddDays(15);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(projectId, emptyDescription, openOnUtc, deadlineUtc), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_OpenOnIsNotUtc_ShouldThrowContractException()
        {
            //Arrange
            int projectId = Random.NextPositive();
            string description = Random.NextString();
            DateTime openOn = DateTime.Now;
            DateTime deadlineUtc = DateTime.UtcNow.AddDays(15);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(projectId, description, openOn, deadlineUtc), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_DeadlineIsNotUtc_ShouldThrowContractException()
        {
            //Arrange
            int projectId = Random.NextPositive();
            string description = Random.NextString();
            DateTime openOnUtc = DateTime.UtcNow;
            DateTime deadline = DateTime.Now.AddDays(15);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(projectId, description, openOnUtc, deadline), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_DeadlineNotInFuture_ShouldThrowContractException()
        {
            //Arrange
            int projectId = Random.NextPositive();
            string description = Random.NextString();
            DateTime openOnUtc = DateTime.UtcNow;
            DateTime deadlineInPastUtc = DateTime.UtcNow.AddMinutes(-1);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(projectId, description, openOnUtc, deadlineInPastUtc), Throws.InstanceOf<ContractException>());
        }

        [Test]
        public void CreateNew_DeadlineIsBeforeOpenOn_ShouldThrowContractException()
        {
            //Arrange
            int projectId = Random.NextPositive();
            string description = Random.NextString();
            DateTime openOnUtc = DateTime.UtcNow.AddDays(1);
            DateTime deadlineUtc = openOnUtc.AddMinutes(-1);

            //Act + Assert
            Assert.That(() => _factory.CreateNew(projectId, description, openOnUtc, deadlineUtc), Throws.InstanceOf<ContractException>());
        }
    }
}
