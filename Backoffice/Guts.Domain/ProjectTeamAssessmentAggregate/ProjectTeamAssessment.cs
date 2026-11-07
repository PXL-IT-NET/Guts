using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate;

internal class ProjectTeamAssessment : AggregateRoot, IProjectTeamAssessment
{
    private readonly HashSet<IPeerAssessment> _peerAssessments;
    private readonly HashSet<int> _includedUserIds;
    private readonly HashSet<int> _excludedUserIds;

    public IProjectAssessment ProjectAssessment { get; private set; }

    public IProjectTeam Team { get; private set; }

    public IReadOnlyCollection<IPeerAssessment> PeerAssessments => _peerAssessments;

    public DateTime? CompletedOnUtc { get; private set; }

    public bool IsComplete => CompletedOnUtc.HasValue || HasCompletedEvaluationMatrixForEligiblePeers();

    private ProjectTeamAssessment()
    {
        _peerAssessments = new HashSet<IPeerAssessment>();
        _includedUserIds = new HashSet<int>();
        _excludedUserIds = new HashSet<int>();
    }

    private ProjectTeamAssessment(IProjectAssessment projectAssessment, IProjectTeam team) : this()
    {
        Contracts.Require(team != null, "A team must be provided.");
        Contracts.Require(team.TeamUsers.Count > 0, "The team has no members.");
        Contracts.Require(team.TeamUsers.First().User != null, "The users of team must be loaded with user data.");
        Team = team;

        Contracts.Require(projectAssessment.Id > 0, "A project team assessment can only created for an existing (stored) project assessment.");
        ProjectAssessment = projectAssessment;
    }

    public class Factory : IProjectTeamAssessmentFactory
    {
        public IProjectTeamAssessment CreateNew(IProjectAssessment projectAssessment, IProjectTeam team)
        {
            Contracts.Require(projectAssessment != null, "A project assessment must be provided to create a project team assessment.");
            Contracts.Require(projectAssessment.OpenOnUtc <= DateTime.UtcNow, "The project team assessment cannot be created because the project assessment is not opened yet.");
            return new ProjectTeamAssessment(projectAssessment, team);
        }
    }

    public IReadOnlyList<User> GetPeers()
    {
        if (IsComplete)
        {
            return _peerAssessments.Select(pa => pa.Subject).DistinctBy(u => u.Id).ToList();
        }

        return GetEligiblePeersFromTeam();
    }

    public User GetPeer(int userId)
    {
        User peer = GetPeers().SingleOrDefault(u => u.Id == userId);
        Contracts.Require(peer != null, $"The user with id '{userId}' is not a peer in the team assessment.");
        return peer;
    }

    public void SetPeerInclusion(int userId, bool isIncluded)
    {
        Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to change peer inclusion.");
        IProjectTeamUser teamUser = Team.TeamUsers.SingleOrDefault(tu => tu.UserId == userId);
        Contracts.Require(teamUser != null, $"The user with id '{userId}' is not a member of the team.");
        Contracts.Require(teamUser.JoinedOnUtc > ProjectAssessment.OpenOnUtc,
            "Only members who joined the team after the peer assessment opened can be included or excluded.");

        if (isIncluded)
        {
            _excludedUserIds.Remove(userId);
            _includedUserIds.Add(userId);
        }
        else
        {
            _includedUserIds.Remove(userId);
            _excludedUserIds.Add(userId);
            RemovePeerAssessmentsInvolvingUser(userId);
            UpdateCompletionTimestampIfComplete();
        }
    }

    public IReadOnlyList<IPeerAssessment> RemovePeer(int userId)
    {
        if (IsComplete)
        {
            LockCompletionIfNeeded();
            return Array.Empty<IPeerAssessment>();
        }

        _includedUserIds.Remove(userId);
        _excludedUserIds.Remove(userId);

        List<IPeerAssessment> removed = RemovePeerAssessmentsInvolvingUser(userId);
        UpdateCompletionTimestampIfComplete(excludedTeamUserId: userId);
        return removed;
    }

    public IPeerAssessment AddOrUpdatePeerAssessment(int userId, int subjectId,
        AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore,
        string explanation)
    {
        Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to change a peer assessment.");
        RequireUserToBeAPeer(userId);
        RequireUserToBeAPeer(subjectId);
        Contracts.Require(cooperationScore != AssessmentScore.NullScore, "An empty score for 'cooperation' is not allowed.");
        Contracts.Require(contributionScore != AssessmentScore.NullScore, "An empty score for 'contribution' is not allowed.");
        Contracts.Require(effortScore != AssessmentScore.NullScore, "An empty score for 'effort' is not allowed.");

        //require that explanation is not empty when any of the scores is not 'Average'
        if (cooperationScore != AssessmentScore.Average || contributionScore != AssessmentScore.Average ||
            effortScore != AssessmentScore.Average)
        {
            Contracts.Require(!string.IsNullOrWhiteSpace(explanation), "An explanation is required when a score is not 'Equal to group average' (3).");
        }

        IPeerAssessment peerAssessment = _peerAssessments.SingleOrDefault(pa => pa.User.Id == userId && pa.Subject.Id == subjectId);
        if (peerAssessment == null)
        {
            IReadOnlyList<User> peers = GetEligiblePeersFromTeam();
            User user = peers.Single(u => u.Id == userId);
            User subject = peers.Single(u => u.Id == subjectId);
            peerAssessment = new PeerAssessment(Id, user, subject);
            _peerAssessments.Add(peerAssessment);
        }

        peerAssessment.SetScores(cooperationScore, contributionScore, effortScore, explanation);
        UpdateCompletionTimestampIfComplete();
        return peerAssessment;
    }

    public IReadOnlyList<IPeerAssessment> GetPeerAssessmentsOf(int userId)
    {
        RequireUserToBeAPeer(userId);

        return _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
    }

    public IReadOnlyList<IPeerAssessment> GetMissingPeerAssessmentsOf(int userId)
    {
        RequireUserToBeAPeer(userId);
        IReadOnlyList<User> peers = GetPeers();
        User user = peers.Single(u => u.Id == userId);
        List<IPeerAssessment> storedAssessments = _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
        List<IPeerAssessment> missingAssessments = new List<IPeerAssessment>();

        foreach (User peer in peers.Where(p => storedAssessments.All(a => a.Subject.Id != p.Id)))
        {
            missingAssessments.Add(new PeerAssessment(Id, user, peer));
        }
        return missingAssessments;
    }

    public IReadOnlyList<User> GetPeersThatNeedToEvaluateOthers()
    {
        if (IsComplete)
        {
            return Array.Empty<User>();
        }

        IReadOnlyList<User> eligiblePeers = GetEligiblePeersFromTeam();
        HashSet<int> eligibleIds = eligiblePeers.Select(u => u.Id).ToHashSet();
        var peers = new List<User>();
        foreach (User user in eligiblePeers)
        {
            if (_peerAssessments.Count(pa => pa.User.Id == user.Id && eligibleIds.Contains(pa.Subject.Id)) < eligiblePeers.Count)
            {
                peers.Add(user);
            }
        }
        return peers;
    }

    public void ValidateAssessmentsOf(int userId)
    {
        IReadOnlyList<User> peers = GetPeers();
        List<IPeerAssessment> allAssessmentsOfUser = _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
        if (allAssessmentsOfUser.Count == peers.Count)
        {
            Contracts.Require(!allAssessmentsOfUser.All(pa => pa.ContributionScore > AssessmentScore.Average),
                "It is not possible that everyone has an above average contribution score. Maybe you mean that everyone's score should be 'Average'?");
            Contracts.Require(!allAssessmentsOfUser.All(pa => pa.CooperationScore > AssessmentScore.Average),
                "It is not possible that everyone has an above average cooperation score. Maybe you mean that everyone's score should be 'Average'?");
            Contracts.Require(!allAssessmentsOfUser.All(pa => pa.EffortScore > AssessmentScore.Average),
                "It is not possible that everyone has an above average effort score. Maybe you mean that everyone's score should be 'Average'?");

            Contracts.Require(!allAssessmentsOfUser.All(pa => pa.ContributionScore < AssessmentScore.Average),
                "It is not possible that everyone has a below average contribution score. Maybe you mean that everyone's score should be 'Average'?");
            Contracts.Require(!allAssessmentsOfUser.All(pa => pa.CooperationScore < AssessmentScore.Average),
                "It is not possible that everyone has a below average cooperation score. Maybe you mean that everyone's score should be 'Average'?");
            Contracts.Require(!allAssessmentsOfUser.All(pa => pa.EffortScore < AssessmentScore.Average),
                "It is not possible that everyone has a below average effort score. Maybe you mean that everyone's score should be 'Average'?");
        }
    }

    private IReadOnlyList<User> GetEligiblePeersFromTeam(int? excludedTeamUserId = null)
    {
        return Team.TeamUsers
            .Where(tu => (!excludedTeamUserId.HasValue || tu.UserId != excludedTeamUserId.Value) && IsUserEligiblePeer(tu))
            .Select(tu => tu.User)
            .ToList();
    }

    private bool IsUserEligiblePeer(IProjectTeamUser teamUser)
    {
        if (ProjectAssessment == null || teamUser.JoinedOnUtc <= ProjectAssessment.OpenOnUtc)
        {
            return true;
        }

        if (_excludedUserIds.Contains(teamUser.UserId))
        {
            return false;
        }

        if (_includedUserIds.Contains(teamUser.UserId))
        {
            return true;
        }

        return teamUser.JoinedOnUtc <= ProjectAssessment.DeadlineUtc;
    }

    private bool HasCompletedEvaluationMatrixForEligiblePeers(int? excludedTeamUserId = null)
    {
        if (_peerAssessments.Count < 4)
        {
            return false;
        }

        HashSet<int> subjectIds = _peerAssessments.Select(pa => pa.Subject.Id).ToHashSet();
        if (subjectIds.Count < 2 || _peerAssessments.Count != subjectIds.Count * subjectIds.Count)
        {
            return false;
        }

        HashSet<int> evaluatorIds = _peerAssessments.Select(pa => pa.User.Id).ToHashSet();
        if (!evaluatorIds.SetEquals(subjectIds))
        {
            return false;
        }

        IReadOnlyList<User> eligiblePeers = GetEligiblePeersFromTeam(excludedTeamUserId);
        return eligiblePeers.All(u => subjectIds.Contains(u.Id));
    }

    private void UpdateCompletionTimestampIfComplete(int? excludedTeamUserId = null)
    {
        if (!CompletedOnUtc.HasValue && HasCompletedEvaluationMatrixForEligiblePeers(excludedTeamUserId))
        {
            CompletedOnUtc = DateTime.UtcNow;
        }
    }

    private void LockCompletionIfNeeded()
    {
        if (!CompletedOnUtc.HasValue)
        {
            CompletedOnUtc = DateTime.UtcNow;
        }
    }

    private List<IPeerAssessment> RemovePeerAssessmentsInvolvingUser(int userId)
    {
        List<IPeerAssessment> toRemove = _peerAssessments
            .Where(pa => pa.User.Id == userId || pa.Subject.Id == userId)
            .ToList();

        foreach (IPeerAssessment peerAssessment in toRemove)
        {
            _peerAssessments.Remove(peerAssessment);
        }

        return toRemove;
    }

    private void RequireUserToBeAPeer(int userId)
    {
        IReadOnlyList<User> peers = GetPeers();
        Contracts.Require(peers.Any(u => u.Id == userId), $"The user with id '{userId}' is not a member of the team.");
    }
}