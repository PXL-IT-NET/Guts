using NUnit.Framework;
using System;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Domain.Tests.TopicAggregate.ProjectAggregate;

public class ProjectAssessmentFactoryTests
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
        int projectId = Random.Shared.NextPositive();
        string description = Random.Shared.NextString();
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
        int invalidProjectId = -1 * Random.Shared.NextPositive();
        string description = Random.Shared.NextString();
        DateTime openOnUtc = DateTime.UtcNow;
        DateTime deadlineUtc = openOnUtc.AddDays(15);

        //Act + Assert
        Assert.That(() => _factory.CreateNew(invalidProjectId, description, openOnUtc, deadlineUtc), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_EmptyDescription_ShouldThrowContractException()
    {
        //Arrange
        int projectId = Random.Shared.NextPositive();
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
        int projectId = Random.Shared.NextPositive();
        string description = Random.Shared.NextString();
        DateTime openOn = DateTime.Now;
        DateTime deadlineUtc = DateTime.UtcNow.AddDays(15);

        //Act + Assert
        Assert.That(() => _factory.CreateNew(projectId, description, openOn, deadlineUtc), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_DeadlineIsNotUtc_ShouldThrowContractException()
    {
        //Arrange
        int projectId = Random.Shared.NextPositive();
        string description = Random.Shared.NextString();
        DateTime openOnUtc = DateTime.UtcNow;
        DateTime deadline = DateTime.Now.AddDays(15);

        //Act + Assert
        Assert.That(() => _factory.CreateNew(projectId, description, openOnUtc, deadline), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_DeadlineNotInFuture_ShouldThrowContractException()
    {
        //Arrange
        int projectId = Random.Shared.NextPositive();
        string description = Random.Shared.NextString();
        DateTime openOnUtc = DateTime.UtcNow;
        DateTime deadlineInPastUtc = DateTime.UtcNow.AddMinutes(-1);

        //Act + Assert
        Assert.That(() => _factory.CreateNew(projectId, description, openOnUtc, deadlineInPastUtc), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_DeadlineIsBeforeOpenOn_ShouldThrowContractException()
    {
        //Arrange
        int projectId = Random.Shared.NextPositive();
        string description = Random.Shared.NextString();
        DateTime openOnUtc = DateTime.UtcNow.AddDays(1);
        DateTime deadlineUtc = openOnUtc.AddMinutes(-1);

        //Act + Assert
        Assert.That(() => _factory.CreateNew(projectId, description, openOnUtc, deadlineUtc), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void Update_ValidInput_ShouldUpdatePropertiesCorrectly()
    {
        //Arrange
        IProjectAssessment assessment = _factory.CreateNew(Random.Shared.NextPositive(), Random.Shared.NextString(), DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
        string newDescription = Random.Shared.NextString();
        DateTime newOpenOnUtc = DateTime.UtcNow.AddDays(2);
        DateTime newDeadlineUtc = newOpenOnUtc.AddDays(14);

        //Act
        assessment.Update(newDescription, newOpenOnUtc, newDeadlineUtc);

        //Assert
        Assert.That(assessment.Description, Is.EqualTo(newDescription));
        Assert.That(assessment.OpenOnUtc, Is.EqualTo(newOpenOnUtc));
        Assert.That(assessment.DeadlineUtc, Is.EqualTo(newDeadlineUtc));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Update_EmptyDescription_ShouldThrowContractException(string invalidDescription)
    {
        //Arrange
        IProjectAssessment assessment = _factory.CreateNew(Random.Shared.NextPositive(), Random.Shared.NextString(), DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
        DateTime newOpenOnUtc = DateTime.UtcNow.AddDays(1);
        DateTime newDeadlineUtc = newOpenOnUtc.AddDays(5);

        //Act + Assert
        Assert.That(() => assessment.Update(invalidDescription, newOpenOnUtc, newDeadlineUtc), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void Update_OpenOnUtcIsNotUtc_ShouldThrowContractException()
    {
        //Arrange
        IProjectAssessment assessment = _factory.CreateNew(Random.Shared.NextPositive(), Random.Shared.NextString(), DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
        DateTime localOpenOn = DateTime.Now.AddDays(1);
        DateTime newDeadlineUtc = DateTime.UtcNow.AddDays(5);

        //Act + Assert
        Assert.That(() => assessment.Update(Random.Shared.NextString(), localOpenOn, newDeadlineUtc), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void Update_DeadlineUtcIsNotUtc_ShouldThrowContractException()
    {
        //Arrange
        IProjectAssessment assessment = _factory.CreateNew(Random.Shared.NextPositive(), Random.Shared.NextString(), DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
        DateTime newOpenOnUtc = DateTime.UtcNow.AddDays(1);
        DateTime localDeadline = DateTime.Now.AddDays(5);

        //Act + Assert
        Assert.That(() => assessment.Update(Random.Shared.NextString(), newOpenOnUtc, localDeadline), Throws.InstanceOf<ContractException>());
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Update_OpenOnUtcGreaterOrEqualToDeadlineUtc_ShouldThrowContractException(int minutesOffset)
    {
        //Arrange
        IProjectAssessment assessment = _factory.CreateNew(Random.Shared.NextPositive(), Random.Shared.NextString(), DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
        DateTime newOpenOnUtc = DateTime.UtcNow.AddDays(1);
        DateTime newDeadlineUtc = newOpenOnUtc.AddMinutes(minutesOffset);

        //Act + Assert
        Assert.That(() => assessment.Update(Random.Shared.NextString(), newOpenOnUtc, newDeadlineUtc), Throws.InstanceOf<ContractException>());
    }
}