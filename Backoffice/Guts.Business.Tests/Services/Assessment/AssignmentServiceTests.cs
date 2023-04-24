using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Guts.Business.Services.Assessment;
using Guts.Business.Tests.Builders;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Services.Assessment;

[TestFixture]
public class ProjectTeamAssessmentServiceTests
{
    private ProjectTeamAssessmentService _service;
    private Random _random = new Random();
        
    private Mock<IProjectTeamAssessmentRepository> _projectTeamAssessmentRepositoryMock;
    private Mock<IProjectAssessmentRepository> _projectAssessmentRepositoryMock;
    private Mock<IProjectTeamRepository> _projectTeamRepositoryMock;
    private Mock<IProjectTeamAssessmentFactory> _projectTeamAssessmentFactoryMock;
    private Mock<IAssessmentResultFactory> _assessmentResultFactoryMock;

    [SetUp]
    public void Setup()
    {
        _projectTeamAssessmentRepositoryMock = new Mock<IProjectTeamAssessmentRepository>();
        _projectTeamAssessmentFactoryMock = new Mock<IProjectTeamAssessmentFactory>();
        _projectAssessmentRepositoryMock = new Mock<IProjectAssessmentRepository>();
        _projectTeamRepositoryMock = new Mock<IProjectTeamRepository>();
        _assessmentResultFactoryMock = new Mock<IAssessmentResultFactory>();

        _service = new ProjectTeamAssessmentService(
            _projectTeamAssessmentRepositoryMock.Object,
            _projectTeamAssessmentFactoryMock.Object,
            _projectAssessmentRepositoryMock.Object,
            _projectTeamRepositoryMock.Object,
            _assessmentResultFactoryMock.Object
        );
    }

    [Test]
    public void GetOrCreateTeamAssessmentAsync_ExistingTeamAssessment_ShouldRetrieveTheTeamAssessment()
    {
        //Arrange
        IProjectTeamAssessment existingTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().Build();
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        //Act
        var result = _service.GetOrCreateTeamAssessmentAsync(existingTeamAssessment.ProjectAssessment.Id, existingTeamAssessment.Team.Id).Result;


        //Assert
        Assert.That(result, Is.SameAs(existingTeamAssessment));
    }

    [Test]
    public void GetOrCreateTeamAssessmentAsync_TeamAssessmentDoesNotExist_ShouldCreateAndSaveTheTeamAssessment()
    {
        //Arrange
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Throws<DataNotFoundException>();

        IProjectAssessment projectAssessment = new ProjectAssessmentMockBuilder().WithId().Build().Object;
        _projectAssessmentRepositoryMock.Setup(repo => repo.GetByIdAsync(projectAssessment.Id)).ReturnsAsync(projectAssessment);

        IProjectTeam projectTeam = new ProjectTeamMockBuilder().WithId().Build().Object;
        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(projectTeam.Id)).ReturnsAsync(projectTeam);

        IProjectTeamAssessment createdTeamAssessment = new ProjectTeamAssessmentBuilder().Build();
        _projectTeamAssessmentFactoryMock.Setup(factory => factory.CreateNew(projectAssessment, projectTeam))
            .Returns(createdTeamAssessment);

        //Act
        IProjectTeamAssessment result = _service.GetOrCreateTeamAssessmentAsync(projectAssessment.Id, projectTeam.Id).Result;

        //Assert
        Assert.That(result, Is.SameAs(createdTeamAssessment));
        _projectTeamAssessmentRepositoryMock.Verify(repo => repo.AddAsync(createdTeamAssessment), Times.Once);
    }

    [Test]
    public void GetOrCreateTeamAssessmentAsync_TeamAssessmentDoesNotExist_ProjectAssessmentDoesNotExist_ShouldThrowContractException()
    {
        //Arrange
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Throws<DataNotFoundException>();

        _projectAssessmentRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .Throws<DataNotFoundException>();

        IProjectTeam projectTeam = new ProjectTeamMockBuilder().WithId().Build().Object;
        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(projectTeam.Id)).ReturnsAsync(projectTeam);

        int nonExistingProjectAssessmentId = _random.NextPositive();

        //Act + Assert
        Assert.That(() => _service.GetOrCreateTeamAssessmentAsync(nonExistingProjectAssessmentId, projectTeam.Id),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void GetOrCreateTeamAssessmentAsync_TeamAssessmentDoesNotExist_TeamDoesNotExist_ShouldThrowContractException()
    {
        //Arrange
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Throws<DataNotFoundException>();

        IProjectAssessment projectAssessment = new ProjectAssessmentMockBuilder().WithId().Build().Object;
        _projectAssessmentRepositoryMock.Setup(repo => repo.GetByIdAsync(projectAssessment.Id)).ReturnsAsync(projectAssessment);

        _projectTeamRepositoryMock.Setup(repo => repo.LoadByIdAsync(It.IsAny<int>())).Throws<DataNotFoundException>();

        int nonExistingTeamId = _random.NextPositive();

        //Act + Assert
        Assert.That(() => _service.GetOrCreateTeamAssessmentAsync(projectAssessment.Id, nonExistingTeamId),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void GetStatusAsync_RetrievesTeamAssessmentAndMapsToStatusDto()
    {
        //Arrange
        IProjectTeamAssessment existingTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().Build();
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        //Act
        ProjectTeamAssessmentStatusDto result = _service.GetStatusAsync(existingTeamAssessment.ProjectAssessment.Id, existingTeamAssessment.Team.Id).Result;

        //Assert
        Assert.That(result.Id, Is.EqualTo(existingTeamAssessment.Id));
        Assert.That(result.TeamId, Is.EqualTo(existingTeamAssessment.Team.Id));
        Assert.That(result.IsComplete, Is.EqualTo(existingTeamAssessment.IsComplete));
        Assert.That(result.PeersThatNeedToEvaluateOthers.Count, Is.EqualTo(existingTeamAssessment.Team.TeamUsers.Count));
    }

    [Test]
    public void GetResultsForLectorAsync_RetrievesTeamAssessmentAndCreatesAResultForEachTeamUser()
    {
        //Arrange
        IProjectTeamAssessment existingTeamAssessment = new ProjectTeamAssessmentBuilder()
            .WithId()
            .WithAllPeerAssessmentsAdded()
            .Build();
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        //Act
        IReadOnlyList<IAssessmentResult> results = _service.GetResultsForLectorAsync(existingTeamAssessment.ProjectAssessment.Id, existingTeamAssessment.Team.Id).Result;

        //Assert
        Assert.That(results.Count, Is.EqualTo(existingTeamAssessment.Team.TeamUsers.Count));
        _assessmentResultFactoryMock.Verify(factory =>
            factory.Create(It.IsIn(existingTeamAssessment.Team.TeamUsers.Select(tu => tu.UserId)),
                existingTeamAssessment, true), Times.Exactly(existingTeamAssessment.Team.TeamUsers.Count));
    }

    [Test]
    public void GetResultForStudent_RetrievesTeamAssessmentAndCreatesAResultWithoutPeerAssessments()
    {
        //Arrange
        IProjectTeamAssessment existingTeamAssessment = new ProjectTeamAssessmentBuilder()
            .WithId()
            .WithAllPeerAssessmentsAdded()
            .Build();
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        User student = existingTeamAssessment.Team.TeamUsers.NextRandomItem().User;

        IAssessmentResult createdAssessmentResult = new Mock<IAssessmentResult>().Object;
        _assessmentResultFactoryMock.Setup(factory => factory.Create(student.Id, existingTeamAssessment, false))
            .Returns(createdAssessmentResult);

        //Act
        IAssessmentResult result = _service.GetResultForStudent(existingTeamAssessment.ProjectAssessment.Id,
            existingTeamAssessment.Team.Id, student.Id).Result;

        //Assert
        Assert.That(result, Is.SameAs(createdAssessmentResult));
    }

    [Test]
    public void GetPeerAssessmentsOfUserAsync_RetrievesTeamAssessmentAndReturnedStoredPeerAssessmentsAndNewInstancesForMissingPeerAssessments()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();
        int userId = _random.NextPositive();

        var existingTeamAssessmentMock = new Mock<IProjectTeamAssessment>();
        IProjectTeamAssessment existingTeamAssessment = existingTeamAssessmentMock.Object;
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        var storedPeerAssessments = new List<IPeerAssessment>
        {
            new PeerAssessmentBuilder().Build()
        };
        var missingPeerAssessments = new List<IPeerAssessment>
        {
            new PeerAssessmentBuilder().Build()
        };
        existingTeamAssessmentMock.Setup(ta => ta.GetPeerAssessmentsOf(userId)).Returns(storedPeerAssessments);
        existingTeamAssessmentMock.Setup(ta => ta.GetMissingPeerAssessmentsOf(userId)).Returns(missingPeerAssessments);

        //Act
        IReadOnlyList<IPeerAssessment> results = _service.GetPeerAssessmentsOfUserAsync(projectAssessmentId,teamId, userId).Result;

        //Assert
        Assert.That(storedPeerAssessments, Is.SubsetOf(results));
        Assert.That(missingPeerAssessments, Is.SubsetOf(results));
    }

    [Test]
    public void SavePeerAssessmentsOfUserAsync_RetrievesTeamAssessment_AddOrUpdatesAPeerAssessmentForEachDto_ValidatesTheTeamAssessment_SavesTheTeamAssessment()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();
        int userId = _random.NextPositive();
        var peerAssessmentDtos = new List<PeerAssessmentDto>
        {
            new PeerAssessmentDtoBuilder().WithUserId(userId).Build(),
            new PeerAssessmentDtoBuilder().WithUserId(userId).Build(),
        };

        var existingTeamAssessmentMock = new Mock<IProjectTeamAssessment>();
        IProjectTeamAssessment existingTeamAssessment = existingTeamAssessmentMock.Object;
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        //Act
        _service.SavePeerAssessmentsOfUserAsync(projectAssessmentId, teamId, userId, peerAssessmentDtos).Wait();

        //Assert
        foreach (PeerAssessmentDto dto in peerAssessmentDtos)
        {
            existingTeamAssessmentMock.Verify(
                ta => ta.AddOrUpdatePeerAssessment(dto.UserId, dto.SubjectId, dto.CooperationScore,
                    dto.ContributionScore, dto.EffortScore, dto.Explanation), Times.Once);
        }
        existingTeamAssessmentMock.Verify(ta => ta.ValidateAssessmentsOf(userId), Times.Once);
        _projectTeamAssessmentRepositoryMock.Verify(repo => repo.UpdateAsync(existingTeamAssessment), Times.Once);
    }

    [Test]
    public void SavePeerAssessmentsOfUserAsync_NotAllDtosOfSameUser_ShouldThrowContractException()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();
        int userId = _random.NextPositive();
        int otherUserId = _random.NextPositive();
        var peerAssessmentDtos = new List<PeerAssessmentDto>
        {
            new PeerAssessmentDtoBuilder().WithUserId(userId).Build(),
            new PeerAssessmentDtoBuilder().WithUserId(otherUserId).Build(),
        };

        var existingTeamAssessmentMock = new Mock<IProjectTeamAssessment>();
        IProjectTeamAssessment existingTeamAssessment = existingTeamAssessmentMock.Object;
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        //Act + Assert
        Assert.That(
            () => _service.SavePeerAssessmentsOfUserAsync(projectAssessmentId, teamId, userId, peerAssessmentDtos),
            Throws.InstanceOf<ContractException>());
    }
}