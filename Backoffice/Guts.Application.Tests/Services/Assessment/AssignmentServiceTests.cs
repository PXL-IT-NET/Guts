using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Services.Assessment;
using Guts.Application.Tests.Builders;
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

namespace Guts.Application.Tests.Services.Assessment;

[TestFixture]
public class ProjectTeamAssessmentServiceTests
{
    private ProjectTeamAssessmentService _service;
    private readonly Random _random = new();
        
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
        var existingTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().Build();
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
        var existingTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().Build();
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(existingTeamAssessment);

        //Act
        ProjectTeamAssessmentStatusDto result = _service.GetStatusAsync(existingTeamAssessment.ProjectAssessment.Id, existingTeamAssessment.Team.Id).Result;

        //Assert
        Assert.That(result.Id, Is.EqualTo(existingTeamAssessment.Id));
        Assert.That(result.TeamId, Is.EqualTo(existingTeamAssessment.Team.Id));
        Assert.That(result.IsComplete, Is.EqualTo(existingTeamAssessment.IsComplete));
        Assert.That(result.PeersThatNeedToEvaluateOthers.Count, Is.EqualTo(existingTeamAssessment.Peers.Count));
        Assert.That(result.PeerIds, Is.EquivalentTo(existingTeamAssessment.Peers.Select(u => u.Id)));
    }

    [Test]
    public void GetResultsForTeacherAsync_RetrievesTeamAssessmentAndCreatesAResultForEachPeer()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();
        var peer1 = new UserBuilder().WithId().Build();
        var peer2 = new UserBuilder().WithId().Build();
        List<User> peers = [peer1, peer2];

        var existingTeamAssessmentMock = new Mock<IProjectTeamAssessment>();
        existingTeamAssessmentMock.SetupGet(ta => ta.Peers).Returns(peers);
        IProjectTeamAssessment existingTeamAssessment = existingTeamAssessmentMock.Object;

        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(projectAssessmentId, teamId))
            .ReturnsAsync(existingTeamAssessment);

        //Act
        IReadOnlyList<IAssessmentResult> results = _service.GetResultsForTeacherAsync(projectAssessmentId, teamId).Result;

        //Assert
        Assert.That(results.Count, Is.EqualTo(peers.Count));
        _assessmentResultFactoryMock.Verify(factory =>
            factory.Create(It.IsIn(peers.Select(p => p.Id)),
                existingTeamAssessment, true), Times.Exactly(peers.Count));
    }

    [Test]
    public void SetPeerInclusionAsync_RetrievesTeamAssessment_SetsPeerInclusion_UpdatesTeamAssessment()
    {
        //Arrange
        int projectAssessmentId = _random.NextPositive();
        int teamId = _random.NextPositive();
        int userId = _random.NextPositive();
        bool isIncluded = _random.NextBool();

        var existingTeamAssessmentMock = new Mock<IProjectTeamAssessment>();
        IProjectTeamAssessment existingTeamAssessment = existingTeamAssessmentMock.Object;
        _projectTeamAssessmentRepositoryMock.Setup(repo => repo.LoadAsync(projectAssessmentId, teamId))
            .ReturnsAsync(existingTeamAssessment);

        //Act
        _service.SetPeerInclusionAsync(projectAssessmentId, teamId, userId, isIncluded).Wait();

        //Assert
        existingTeamAssessmentMock.Verify(ta => ta.SetPeerInclusion(userId, isIncluded), Times.Once);
        _projectTeamAssessmentRepositoryMock.Verify(repo => repo.UpdateAsync(existingTeamAssessment), Times.Once);
    }

    [Test]
    public void GetResultForStudent_RetrievesTeamAssessmentAndCreatesAResultWithoutPeerAssessments()
    {
        //Arrange
        var existingTeamAssessment = new ProjectTeamAssessmentBuilder()
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

        List<IPeerAssessment> storedPeerAssessments = [new PeerAssessmentBuilder().Build()];
        List<IPeerAssessment> missingPeerAssessments = [new PeerAssessmentBuilder().Build()];
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
        List<PeerAssessmentDto> peerAssessmentDtos =
        [
            new PeerAssessmentDtoBuilder().WithUserId(userId).Build(),
            new PeerAssessmentDtoBuilder().WithUserId(userId).Build()
        ];

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
        List<PeerAssessmentDto> peerAssessmentDtos =
        [
            new PeerAssessmentDtoBuilder().WithUserId(userId).Build(),
            new PeerAssessmentDtoBuilder().WithUserId(otherUserId).Build()
        ];

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