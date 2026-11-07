using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Moq;
using NUnit.Framework;

namespace Guts.Domain.Tests.ProjectTeamAssessmentAggregate;

public class ProjectTeamAssessmentTests
{
    private IProjectTeamAssessment _projectTeamAssessment;

    [SetUp]
    public void BeforeEachTest()
    {
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().Build();
    }

    [Test]
    public void IsComplete_EachMemberHasEvaluatedSelfAndOthers_ShouldReturnTrue()
    {
        //Arrange
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded().Build();

        //Act + Assert
        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
    }

    [Test]
    public void IsComplete_PeerAssessmentMissing_ShouldReturnFalse()
    {
        //Arrange
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllButOnePeerAssessmentsAdded().Build();

        //Act + Assert
        Assert.That(_projectTeamAssessment.IsComplete, Is.False);
    }

    [Test]
    public void AddOrUpdatePeerAssessment_ValidNonExistingPeerAssessment_ShouldAddTheAssessment()
    {
        //Arrange
        User user = _projectTeamAssessment.Team.TeamUsers.First().User;
        User subject = _projectTeamAssessment.Team.TeamUsers.Last().User;

        AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
        AssessmentScore contributionScore = AssessmentScore.Average;
        AssessmentScore effortScore = AssessmentScore.AboveAverage;
        string explanation = Random.Shared.NextString();

        //Act
        IPeerAssessment result = _projectTeamAssessment.AddOrUpdatePeerAssessment(user.Id, subject.Id, cooperationScore, contributionScore, effortScore, explanation);

        //Assert
        Assert.That(result.User, Is.SameAs(user));
        Assert.That(result.Subject, Is.SameAs(subject));
        Assert.That(result.ProjectTeamAssessmentId, Is.EqualTo(_projectTeamAssessment.Id));

        Assert.That(result.ContributionScore, Is.EqualTo(contributionScore));
        Assert.That(result.EffortScore, Is.EqualTo(effortScore));
        Assert.That(result.CooperationScore, Is.EqualTo(cooperationScore));
        Assert.That(result.Explanation, Is.EqualTo(explanation));

        Assert.That(_projectTeamAssessment.PeerAssessments, Has.One.SameAs(result));
    }

    [Test]
    public void AddOrUpdatePeerAssessment_ValidExistingPeerAssessment_ShouldUpdateTheAssessment()
    {
        //Arrange
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().WithAllButOnePeerAssessmentsAdded().Build();

        IPeerAssessment peerAssessmentToUpdate = _projectTeamAssessment.PeerAssessments.NextRandomItem();

        AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
        AssessmentScore contributionScore = AssessmentScore.Average;
        AssessmentScore effortScore = AssessmentScore.AboveAverage;
        string explanation = Random.Shared.NextString();

        //Act
        IPeerAssessment result = _projectTeamAssessment.AddOrUpdatePeerAssessment(peerAssessmentToUpdate.User.Id,
            peerAssessmentToUpdate.Subject.Id, cooperationScore, contributionScore, effortScore, explanation);

        //Assert
        Assert.That(peerAssessmentToUpdate, Is.SameAs(result));

        Assert.That(result.ContributionScore, Is.EqualTo(contributionScore));
        Assert.That(result.EffortScore, Is.EqualTo(effortScore));
        Assert.That(result.CooperationScore, Is.EqualTo(cooperationScore));
        Assert.That(result.Explanation, Is.EqualTo(explanation));
    }

    [Test]
    public void AddOrUpdatePeerAssessment_TeamAssessmentAlreadyCompleted_ShouldThrowContractException()
    {
        //Arrange
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithId().WithAllPeerAssessmentsAdded().Build();

        IPeerAssessment peerAssessmentToUpdate = _projectTeamAssessment.PeerAssessments.NextRandomItem();

        AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
        AssessmentScore contributionScore = AssessmentScore.Average;
        AssessmentScore effortScore = AssessmentScore.AboveAverage;
        string explanation = Random.Shared.NextString();

        //Act + Assert
        Assert.That(
            () => _projectTeamAssessment.AddOrUpdatePeerAssessment(peerAssessmentToUpdate.User.Id,
                peerAssessmentToUpdate.Subject.Id,
                cooperationScore, contributionScore, effortScore, explanation),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void AddOrUpdatePeerAssessment_UserNotPartOfTheTeam_ShouldThrowContractException()
    {
        //Arrange
        User invalidUser = new UserBuilder().WithId().Build();
        User subject = _projectTeamAssessment.Team.TeamUsers.Last().User;

        AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
        AssessmentScore contributionScore = AssessmentScore.Average;
        AssessmentScore effortScore = AssessmentScore.AboveAverage;
        string explanation = Random.Shared.NextString();

        //Act + Assert
        Assert.That(
            () => _projectTeamAssessment.AddOrUpdatePeerAssessment(invalidUser.Id, subject.Id,
                cooperationScore, contributionScore, effortScore, explanation),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void AddOrUpdatePeerAssessment_SubjectNotPartOfTheTeam_ShouldThrowContractException()
    {
        //Arrange
        User user = _projectTeamAssessment.Team.TeamUsers.Last().User;
        User invalidSubject = new UserBuilder().WithId().Build();

        AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
        AssessmentScore contributionScore = AssessmentScore.Average;
        AssessmentScore effortScore = AssessmentScore.AboveAverage;
        string explanation = Random.Shared.NextString();

        //Act + Assert
        Assert.That(
            () => _projectTeamAssessment.AddOrUpdatePeerAssessment(user.Id, invalidSubject.Id,
                cooperationScore, contributionScore, effortScore, explanation),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void AddOrUpdatePeerAssessment_ScoresNotAllAverage_NoExplanation_ShouldThrowContractException()
    {
        //Arrange
        User user = _projectTeamAssessment.Team.TeamUsers.Last().User;
        User subject = _projectTeamAssessment.Team.TeamUsers.Last().User;

         
        AssessmentScore cooperationScore = AssessmentScore.BelowAverage;
        AssessmentScore contributionScore = AssessmentScore.Average;
        AssessmentScore effortScore = AssessmentScore.WayAboveAverage;
        string explanation = string.Empty;

        //Act + Assert
        Assert.That(
            () => _projectTeamAssessment.AddOrUpdatePeerAssessment(user.Id, subject.Id,
                cooperationScore, contributionScore, effortScore, explanation),
            Throws.InstanceOf<ContractException>());
    }

    [TestCase(-1, 3 ,3)]
    [TestCase(3, -1, 3)]
    [TestCase(3, 3, -1)]
    public void AddOrUpdatePeerAssessment_NullScorePassedIn_ShouldThrowContractException(
        int cooperationScore, int contributionScore, int effortScore)
    {
        //Arrange
        User user = _projectTeamAssessment.Team.TeamUsers.First().User;
        User subject = _projectTeamAssessment.Team.TeamUsers.Last().User;

        string explanation = Random.Shared.NextString();

        //Act + Assert
        Assert.That(
            () => _projectTeamAssessment.AddOrUpdatePeerAssessment(user.Id, subject.Id,
                cooperationScore, contributionScore, effortScore, explanation),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void GetMissingPeerAssessmentsOf_UserNotATeamMember_ShouldThrowContractException()
    {
        //Arrange
        User nonTeamUser = new UserBuilder().Build();

        //Act + Assert
        Assert.That(
            () => _projectTeamAssessment.GetMissingPeerAssessmentsOf(nonTeamUser.Id),
            Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void GetMissingPeerAssessmentsOf_NoPeerAssessmentsStoredYet_ShouldReturnANewPeerAssessmentForEachTeamMember()
    {
        //Arrange
        User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User; ;

        //Act
        IReadOnlyList<IPeerAssessment> results = _projectTeamAssessment.GetMissingPeerAssessmentsOf(user.Id);

        //Assert
        Assert.That(results, Has.Count.EqualTo(_projectTeamAssessment.Team.TeamUsers.Count));
        Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.ContributionScore == AssessmentScore.NullScore));
        Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.EffortScore == AssessmentScore.NullScore));
        Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.CooperationScore == AssessmentScore.NullScore));
        Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.User.Id == user.Id));
        foreach (IProjectTeamUser teamUser in _projectTeamAssessment.Team.TeamUsers)
        {
            Assert.That(results, Has.One.Matches<IPeerAssessment>(pa => pa.Subject.Id == teamUser.UserId));
        }
    }

    [Test]
    public void GetMissingPeerAssessmentsOf_NoMissingPeerAssessments_ShouldReturnEmptyList()
    {
        //Arrange
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded().Build();
        User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User; ;

        //Act
        IReadOnlyList<IPeerAssessment> results = _projectTeamAssessment.GetMissingPeerAssessmentsOf(user.Id);

        //Assert
        Assert.That(results, Has.Count.Zero);
    }

    [Test]
    [TestCase(4, 3, 3)]
    [TestCase(3, 5, 3)]
    [TestCase(3, 3, 4)]
    [TestCase(1, 3, 3)]
    [TestCase(3, 2, 3)]
    [TestCase(3, 3, 2)]
    public void ValidateAssessmentsOf_AllScoresAboveOrBelowAverage_ShouldThrowContractException(int contributionScore, int effortScore, int cooperationScore)
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
        _projectTeamAssessment = builder.Build();
        User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

        foreach (IProjectTeamUser teamUser in _projectTeamAssessment.Team.TeamUsers)
        {
            builder.WithPeerAssessment(user, teamUser.User, contributionScore, effortScore, cooperationScore);
        }

        //Act + Assert
        Assert.That(() => _projectTeamAssessment.ValidateAssessmentsOf(user.Id), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void ValidateAssessmentsOf_NotAllPeersAssessed_ShouldDoNothing()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
        _projectTeamAssessment = builder.Build();
        User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

        IEnumerable<IProjectTeamUser> subTeamUsers = _projectTeamAssessment.Team.TeamUsers.Take(2);

        foreach (IProjectTeamUser teamUser in subTeamUsers)
        {
            builder.WithPeerAssessment(user, teamUser.User, AssessmentScore.NoAddedValue, AssessmentScore.NoAddedValue, AssessmentScore.NoAddedValue);
        }

        //Act
        _projectTeamAssessment.ValidateAssessmentsOf(user.Id);
    }

    [Test]
    public void ValidateAssessmentsOf_AllPeersAssessedWithValidBalancedScores_ShouldNotThrow()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
        _projectTeamAssessment = builder.Build();
        User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

        //Act + Assert
        Assert.That(() => _projectTeamAssessment.ValidateAssessmentsOf(user.Id), Throws.Nothing);
    }

    [Test]
    public void GetPeersThatNeedToEvaluateOthers_AllPeerAssessmentsFilledIn_ShouldReturnEmptyList()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
        _projectTeamAssessment = builder.Build();

        //Act
        IReadOnlyList<User> results = _projectTeamAssessment.GetPeersThatNeedToEvaluateOthers();

        //Assert
        Assert.That(results, Has.Count.Zero);
    }

    [Test]
    public void GetPeersThatNeedToEvaluateOthers_NoPeerAssessmentsFilledIn_ShouldReturnAllTeamUsers()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder();
        _projectTeamAssessment = builder.Build();

        //Act
        IReadOnlyList<User> results = _projectTeamAssessment.GetPeersThatNeedToEvaluateOthers();

        //Assert
        Assert.That(results, Has.Count.EqualTo(_projectTeamAssessment.Team.TeamUsers.Count));
        Assert.That(results, Has.All.Matches<User>(peer => _projectTeamAssessment.Team.TeamUsers.Any(tu => tu.UserId == peer.Id)));
    }

    [Test]
    public void GetPeersThatNeedToEvaluateOthers_PeerAssessmentsOfOneUserMissing_ShouldReturnThatUser()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder();
        _projectTeamAssessment = builder.Build();
        User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;
        builder.WithAllAssessmentsAddedExceptAssessmentsOf(user.Id);

        //Act
        IReadOnlyList<User> results = _projectTeamAssessment.GetPeersThatNeedToEvaluateOthers();

        //Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results, Has.One.EqualTo(user));
    }

    [Test]
    public void GetPeerAssessmentsOf_ShouldReturnAllPeerAssessmentsOfUser()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
        _projectTeamAssessment = builder.Build();
        User user = _projectTeamAssessment.Team.TeamUsers.NextRandomItem().User;

        //Act
        IReadOnlyList<IPeerAssessment> results = _projectTeamAssessment.GetPeerAssessmentsOf(user.Id);

        //Assert
        Assert.That(results, Has.Count.EqualTo(_projectTeamAssessment.Team.TeamUsers.Count));
        Assert.That(results, Has.All.Matches<IPeerAssessment>(pa => pa.User.Id == user.Id));
    }

    [Test]
    public void GetPeerAssessmentsOf_UserNotInTeam_ShouldThrowContractException()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithAllPeerAssessmentsAdded();
        _projectTeamAssessment = builder.Build();
        User nonTeamUser = new UserBuilder().Build();

        //Act + Assert
        Assert.That(() => _projectTeamAssessment.GetPeerAssessmentsOf(nonTeamUser.Id), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void CreateNew_ShouldInitializePeersFromTeamUsers()
    {
        //Act + Assert
        Assert.That(_projectTeamAssessment.Peers, Is.EquivalentTo(_projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User)));
        foreach (IProjectTeamUser teamUser in _projectTeamAssessment.Team.TeamUsers)
        {
            Assert.That(_projectTeamAssessment.GetPeer(teamUser.UserId), Is.SameAs(teamUser.User));
        }
    }

    [Test]
    public void IsComplete_SinglePeerTeamWithSelfAssessment_ShouldReturnFalse()
    {
        //Arrange
        Mock<IProjectTeam> singleMemberTeamMock = new ProjectTeamMockBuilder().WithId().WithMembers(1).Build();
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder()
            .WithTeam(singleMemberTeamMock.Object)
            .WithId()
            .WithAllPeerAssessmentsAdded()
            .Build();

        //Act + Assert
        Assert.That(_projectTeamAssessment.IsComplete, Is.False);
    }

    [Test]
    public void CompletedAssessment_WhenTeamUsersChange_ShouldKeepPeersAndRemainCompleteAndRejectPeerChanges()
    {
        //Arrange
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder()
            .WithId()
            .WithAllPeerAssessmentsAdded()
            .Build();
        var originalPeers = _projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User).ToList();
        var teamUsers = (IList<IProjectTeamUser>)_projectTeamAssessment.Team.TeamUsers;

        // Simulate a member leaving and a new member joining the team after completion
        User removedUser = teamUsers.First().User;
        teamUsers.RemoveAt(0);

        User newUser = new UserBuilder().Build();
        var newMemberMock = new Mock<IProjectTeamUser>();
        newMemberMock.SetupGet(m => m.UserId).Returns(newUser.Id);
        newMemberMock.SetupGet(m => m.User).Returns(newUser);
        teamUsers.Add(newMemberMock.Object);

        //Act + Assert
        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
        Assert.That(_projectTeamAssessment.Peers, Is.EquivalentTo(originalPeers));
        Assert.That(() => _projectTeamAssessment.AddPeer(newUser.Id), Throws.InstanceOf<ContractException>());
        Assert.That(() => _projectTeamAssessment.RemovePeer(removedUser.Id), Throws.InstanceOf<ContractException>());
        Assert.That(() => _projectTeamAssessment.SetPeerInclusion(newUser.Id, true), Throws.InstanceOf<ContractException>());
        Assert.That(() => _projectTeamAssessment.SetPeerInclusion(originalPeers.Last().Id, false), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void AddPeer_And_SetPeerInclusionTrue_IncompleteAssessment_ShouldAddTeamMemberToPeers()
    {
        //Arrange
        var teamUsers = (IList<IProjectTeamUser>)_projectTeamAssessment.Team.TeamUsers;
        User newUser = new UserBuilder().Build();
        var newMemberMock = new Mock<IProjectTeamUser>();
        newMemberMock.SetupGet(m => m.UserId).Returns(newUser.Id);
        newMemberMock.SetupGet(m => m.User).Returns(newUser);
        teamUsers.Add(newMemberMock.Object);

        Assert.That(_projectTeamAssessment.Peers, Does.Not.Contain(newUser));

        //Act
        _projectTeamAssessment.SetPeerInclusion(newUser.Id, true);

        //Assert
        Assert.That(_projectTeamAssessment.Peers, Does.Contain(newUser));
        Assert.That(_projectTeamAssessment.GetPeer(newUser.Id), Is.SameAs(newUser));
        Assert.That(_projectTeamAssessment.GetPeersThatNeedToEvaluateOthers(), Does.Contain(newUser));
    }

    [Test]
    public void AddPeer_UserNotInTeam_ShouldThrowContractException()
    {
        //Arrange
        User nonTeamUser = new UserBuilder().Build();

        //Act + Assert
        Assert.That(() => _projectTeamAssessment.AddPeer(nonTeamUser.Id), Throws.InstanceOf<ContractException>());
        Assert.That(() => _projectTeamAssessment.SetPeerInclusion(nonTeamUser.Id, true), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void RemovePeer_And_SetPeerInclusionFalse_IncompleteAssessment_ShouldRemoveFromPeersAndDeleteEvaluationsAndAutoCompleteWhenRemainingPeersComplete()
    {
        //Arrange
        Mock<IProjectTeam> teamMock = new ProjectTeamMockBuilder().WithId().WithMembers(3).Build();
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder()
            .WithTeam(teamMock.Object)
            .WithId()
            .Build();
        var users = _projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User).ToList();
        User user1 = users[0];
        User user2 = users[1];
        User user3 = users[2];

        // User 1 and User 2 evaluate everyone (including User 3); User 3 only evaluates User 1
        foreach (User evaluator in new[] { user1, user2 })
        {
            foreach (User subject in users)
            {
                _projectTeamAssessment.AddOrUpdatePeerAssessment(evaluator.Id, subject.Id,
                    AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);
            }
        }
        _projectTeamAssessment.AddOrUpdatePeerAssessment(user3.Id, user1.Id,
            AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);

        Assert.That(_projectTeamAssessment.IsComplete, Is.False);

        //Act
        _projectTeamAssessment.SetPeerInclusion(user3.Id, false);

        //Assert
        Assert.That(_projectTeamAssessment.Peers, Is.EquivalentTo(new[] { user1, user2 }));
        Assert.That(_projectTeamAssessment.PeerAssessments, Has.Count.EqualTo(4));
        Assert.That(_projectTeamAssessment.PeerAssessments.Any(pa => pa.User.Id == user3.Id || pa.Subject.Id == user3.Id), Is.False);
        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
        Assert.That(() => _projectTeamAssessment.GetPeer(user3.Id), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void RemovePeer_TwoPeerTeam_WhenOnePeerRemoved_ShouldDeleteEvaluationsAndKeepIsCompleteFalse()
    {
        //Arrange
        Mock<IProjectTeam> teamMock = new ProjectTeamMockBuilder().WithId().WithMembers(2).Build();
        _projectTeamAssessment = new ProjectTeamAssessmentBuilder()
            .WithTeam(teamMock.Object)
            .WithId()
            .Build();
        var users = _projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User).ToList();
        User user1 = users[0];
        User user2 = users[1];

        _projectTeamAssessment.AddOrUpdatePeerAssessment(user1.Id, user1.Id,
            AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);
        _projectTeamAssessment.AddOrUpdatePeerAssessment(user1.Id, user2.Id,
            AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);

        //Act
        IReadOnlyList<IPeerAssessment> removedAssessments = _projectTeamAssessment.RemovePeer(user2.Id);

        //Assert
        Assert.That(removedAssessments, Has.Count.EqualTo(1));
        Assert.That(_projectTeamAssessment.Peers, Is.EquivalentTo(new[] { user1 }));
        Assert.That(_projectTeamAssessment.PeerAssessments, Has.Count.EqualTo(1));
        Assert.That(_projectTeamAssessment.IsComplete, Is.False);
    }
}