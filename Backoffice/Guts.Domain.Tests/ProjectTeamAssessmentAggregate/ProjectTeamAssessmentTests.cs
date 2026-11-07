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
    public void IsComplete_SingleMemberTeamWithSelfAssessment_ShouldReturnFalse()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithMemberCount(1).WithId().WithAllPeerAssessmentsAdded();
        _projectTeamAssessment = builder.Build();

        //Act + Assert
        Assert.That(_projectTeamAssessment.IsComplete, Is.False);
    }

    [Test]
    public void CompletedAssessment_WhenMemberJoinsOrLeavesTeamLater_ShouldRemainCompleteAndPreserveOriginalPeers()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithId();
        _projectTeamAssessment = builder.Build();
        List<User> originalMembers = _projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User).ToList();

        foreach (User evaluator in originalMembers)
        {
            foreach (User subject in originalMembers)
            {
                _projectTeamAssessment.AddOrUpdatePeerAssessment(evaluator.Id, subject.Id,
                    AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);
            }
        }

        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
        Assert.That(_projectTeamAssessment.CompletedOnUtc, Is.Not.Null);

        //Act — remove one original member and add a new member to the underlying team
        User removedMember = originalMembers.First();
        builder.RemoveTeamMember(removedMember.Id);
        IReadOnlyList<IPeerAssessment> removedAssessments = _projectTeamAssessment.RemovePeer(removedMember.Id);
        User addedMember = builder.AddTeamMember(_projectTeamAssessment.ProjectAssessment.OpenOnUtc.AddHours(1));

        //Assert
        Assert.That(removedAssessments, Is.Empty);
        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
        Assert.That(_projectTeamAssessment.PeerAssessments, Has.Count.EqualTo(originalMembers.Count * originalMembers.Count));
        IReadOnlyList<User> peers = _projectTeamAssessment.GetPeers();
        Assert.That(peers, Is.EquivalentTo(originalMembers));
        Assert.That(peers, Does.Not.Contain(addedMember));
        Assert.That(_projectTeamAssessment.GetPeer(removedMember.Id), Is.SameAs(removedMember));
        Assert.That(() => _projectTeamAssessment.GetPeer(addedMember.Id), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void RemovePeer_IncompleteAssessmentWithThreeMembersWhereOtherTwoAlreadyEvaluatedAll_ShouldDeleteUserAssessmentsAndAutoComplete()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithMemberCount(3).WithId();
        _projectTeamAssessment = builder.Build();
        List<User> members = _projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User).ToList();
        User member1 = members[0];
        User member2 = members[1];
        User leavingMember = members[2];

        foreach (User evaluator in new[] { member1, member2 })
        {
            foreach (User subject in members)
            {
                _projectTeamAssessment.AddOrUpdatePeerAssessment(evaluator.Id, subject.Id,
                    AssessmentScore.AboveAverage, AssessmentScore.AboveAverage, AssessmentScore.AboveAverage, "Explanation");
            }
        }

        Assert.That(_projectTeamAssessment.IsComplete, Is.False);

        //Act
        builder.RemoveTeamMember(leavingMember.Id);
        IReadOnlyList<IPeerAssessment> deleted = _projectTeamAssessment.RemovePeer(leavingMember.Id);

        //Assert
        Assert.That(deleted, Has.Count.EqualTo(2));
        Assert.That(_projectTeamAssessment.PeerAssessments, Has.Count.EqualTo(4));
        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
        Assert.That(_projectTeamAssessment.CompletedOnUtc, Is.Not.Null);
        Assert.That(_projectTeamAssessment.GetPeers(), Is.EquivalentTo(new[] { member1, member2 }));
    }

    [Test]
    public void RemovePeer_IncompleteAssessmentWithTwoMembersWhereFirstMemberEvaluatedBoth_ShouldNotCompleteWithOneRemainingPeer()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithMemberCount(2).WithId();
        _projectTeamAssessment = builder.Build();
        List<User> members = _projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User).ToList();
        User remainingMember = members[0];
        User leavingMember = members[1];

        foreach (User subject in members)
        {
            _projectTeamAssessment.AddOrUpdatePeerAssessment(remainingMember.Id, subject.Id,
                AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);
        }

        //Act
        builder.RemoveTeamMember(leavingMember.Id);
        IReadOnlyList<IPeerAssessment> deleted = _projectTeamAssessment.RemovePeer(leavingMember.Id);

        //Assert
        Assert.That(deleted, Has.Count.EqualTo(1));
        Assert.That(_projectTeamAssessment.PeerAssessments, Has.Count.EqualTo(1));
        Assert.That(_projectTeamAssessment.IsComplete, Is.False);
        Assert.That(_projectTeamAssessment.CompletedOnUtc, Is.Null);
    }

    [Test]
    public void GetPeers_LateJoiningMemberBeforeDeadline_ShouldBeIncludedByDefault_AndAfterDeadline_ShouldBeExcludedByDefault()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithMemberCount(2).WithId();
        _projectTeamAssessment = builder.Build();
        DateTime openOnUtc = _projectTeamAssessment.ProjectAssessment.OpenOnUtc;
        DateTime deadlineUtc = _projectTeamAssessment.ProjectAssessment.DeadlineUtc;

        User joinedBeforeDeadline = builder.AddTeamMember(openOnUtc.AddHours(2));
        User joinedAfterDeadline = builder.AddTeamMember(deadlineUtc.AddHours(1));

        //Act
        IReadOnlyList<User> peers = _projectTeamAssessment.GetPeers();

        //Assert
        Assert.That(peers, Does.Contain(joinedBeforeDeadline));
        Assert.That(peers, Does.Not.Contain(joinedAfterDeadline));
        Assert.That(() => _projectTeamAssessment.GetPeerAssessmentsOf(joinedAfterDeadline.Id), Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void SetPeerInclusion_ExcludeLateJoiningMember_ShouldRemoveTheirAssessmentsAndAutoCompleteIfRemainingPeersAreDone()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithMemberCount(2).WithId();
        _projectTeamAssessment = builder.Build();
        List<User> originalMembers = _projectTeamAssessment.Team.TeamUsers.Select(tu => tu.User).ToList();
        User lateMember = builder.AddTeamMember(_projectTeamAssessment.ProjectAssessment.OpenOnUtc.AddHours(1));

        // Both original members evaluate everyone (including lateMember), lateMember has not evaluated yet
        foreach (User evaluator in originalMembers)
        {
            foreach (User subject in _projectTeamAssessment.GetPeers())
            {
                _projectTeamAssessment.AddOrUpdatePeerAssessment(evaluator.Id, subject.Id,
                    AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);
            }
        }

        Assert.That(_projectTeamAssessment.IsComplete, Is.False);

        //Act — Teacher excludes lateMember
        _projectTeamAssessment.SetPeerInclusion(lateMember.Id, false);

        //Assert
        Assert.That(_projectTeamAssessment.GetPeers(), Is.EquivalentTo(originalMembers));
        Assert.That(_projectTeamAssessment.PeerAssessments.Any(pa => pa.User.Id == lateMember.Id || pa.Subject.Id == lateMember.Id), Is.False);
        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
    }

    [Test]
    public void SetPeerInclusion_IncludeMemberWhoJoinedAfterDeadline_ShouldMakeThemEligiblePeer()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithMemberCount(2).WithId();
        _projectTeamAssessment = builder.Build();
        User joinedAfterDeadline = builder.AddTeamMember(_projectTeamAssessment.ProjectAssessment.DeadlineUtc.AddHours(2));

        Assert.That(_projectTeamAssessment.GetPeers(), Does.Not.Contain(joinedAfterDeadline));

        //Act — Teacher includes joinedAfterDeadline
        _projectTeamAssessment.SetPeerInclusion(joinedAfterDeadline.Id, true);

        //Assert
        Assert.That(_projectTeamAssessment.GetPeers(), Does.Contain(joinedAfterDeadline));
        Assert.That(_projectTeamAssessment.GetPeersThatNeedToEvaluateOthers(), Does.Contain(joinedAfterDeadline));
    }

    [Test]
    public void SetPeerInclusion_WhenAssessmentCompleteOrMemberJoinedBeforeOpenOn_ShouldThrowContractException()
    {
        //Arrange
        var builder = new ProjectTeamAssessmentBuilder().WithMemberCount(2).WithId();
        _projectTeamAssessment = builder.Build();
        User originalMember = _projectTeamAssessment.Team.TeamUsers.First().User;
        User lateMember = builder.AddTeamMember(_projectTeamAssessment.ProjectAssessment.OpenOnUtc.AddHours(1));

        // Member joined before OpenOnUtc -> cannot toggle inclusion
        Assert.That(() => _projectTeamAssessment.SetPeerInclusion(originalMember.Id, false), Throws.InstanceOf<ContractException>());

        // Unknown user -> cannot toggle inclusion
        Assert.That(() => _projectTeamAssessment.SetPeerInclusion(999999, false), Throws.InstanceOf<ContractException>());

        // Complete the assessment
        foreach (User evaluator in _projectTeamAssessment.GetPeers())
        {
            foreach (User subject in _projectTeamAssessment.GetPeers())
            {
                _projectTeamAssessment.AddOrUpdatePeerAssessment(evaluator.Id, subject.Id,
                    AssessmentScore.Average, AssessmentScore.Average, AssessmentScore.Average, string.Empty);
            }
        }

        Assert.That(_projectTeamAssessment.IsComplete, Is.True);
        Assert.That(() => _projectTeamAssessment.SetPeerInclusion(lateMember.Id, false), Throws.InstanceOf<ContractException>());
    }
}