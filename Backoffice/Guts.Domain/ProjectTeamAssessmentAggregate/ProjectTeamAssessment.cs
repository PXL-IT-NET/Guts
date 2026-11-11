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
    public int Id { get; set; }

    private readonly HashSet<User> _peers;
    private readonly HashSet<IPeerAssessment> _peerAssessments;

    public IProjectAssessment ProjectAssessment { get; private set; }

    public IProjectTeam Team { get; private set; }

    public IReadOnlyCollection<User> Peers => _peers;

    public IReadOnlyCollection<IPeerAssessment> PeerAssessments => _peerAssessments;

    public bool IsComplete => _peers.Count >= 2 && (int)Math.Pow(_peers.Count, 2) == _peerAssessments.Count;

    private ProjectTeamAssessment()
    {
        _peers = new HashSet<User>();
        _peerAssessments = new HashSet<IPeerAssessment>();
    }

    private ProjectTeamAssessment(IProjectAssessment projectAssessment, IProjectTeam team) : this()
    {
        Contracts.Require(team != null, "A team must be provided.");
        Contracts.Require(team.TeamUsers.Count > 0, "The team has no members.");
        Contracts.Require(team.TeamUsers.First().User != null, "The users of team must be loaded with user data.");
        Team = team;

        foreach (IProjectTeamUser teamUser in team.TeamUsers)
        {
            _peers.Add(teamUser.User);
        }

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

    public User GetPeer(int userId)
    {
        User peer = _peers.SingleOrDefault(p => p.Id == userId);
        Contracts.Require(peer != null, $"The user with id '{userId}' is not a peer in this team assessment.");
        return peer;
    }

    public void AddPeer(int userId)
    {
        Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to add or remove peers.");
        User user = Team.GetTeamUser(userId);
        if (_peers.All(p => p.Id != userId))
        {
            _peers.Add(user);
        }
    }

    public IReadOnlyList<IPeerAssessment> RemovePeer(int userId)
    {
        Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to add or remove peers.");
        _peers.RemoveWhere(p => p.Id == userId);

        List<IPeerAssessment> removedAssessments = _peerAssessments
            .Where(pa => pa.User.Id == userId || pa.Subject.Id == userId)
            .ToList();

        foreach (IPeerAssessment removedAssessment in removedAssessments)
        {
            _peerAssessments.Remove(removedAssessment);
        }

        return removedAssessments;
    }

    public void SetPeerInclusion(int userId, bool isIncluded)
    {
        Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to add or remove peers.");
        Team.GetTeamUser(userId);
        if (isIncluded)
        {
            AddPeer(userId);
        }
        else
        {
            RemovePeer(userId);
        }
    }

    public IPeerAssessment AddOrUpdatePeerAssessment(int userId, int subjectId,
        AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore,
        string explanation)
    {
        User user = GetPeer(userId);
        User subject = GetPeer(subjectId);
        Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to change a peer assessment.");
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
            peerAssessment = new PeerAssessment(Id, user, subject);
            _peerAssessments.Add(peerAssessment);
        }

        peerAssessment.SetScores(cooperationScore, contributionScore, effortScore, explanation);
        return peerAssessment;
    }

    public IReadOnlyList<IPeerAssessment> GetPeerAssessmentsOf(int userId)
    {
        GetPeer(userId);

        return _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
    }

    public IReadOnlyList<IPeerAssessment> GetMissingPeerAssessmentsOf(int userId)
    {
        User user = GetPeer(userId);
        List<IPeerAssessment> storedAssessments = _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
        List<IPeerAssessment> missingAssessments = new List<IPeerAssessment>();

        foreach (User peer in _peers.Where(peer => storedAssessments.All(a => a.Subject.Id != peer.Id)))
        {
            missingAssessments.Add(new PeerAssessment(Id, user, peer));
        }
        return missingAssessments;
    }

    public IReadOnlyList<User> GetPeersThatNeedToEvaluateOthers()
    {
        var peers = new List<User>();
        foreach (User user in _peers)
        {
            if (_peerAssessments.Count(pa => pa.User.Id == user.Id) < _peers.Count)
            {
                peers.Add(user);
            }
        }
        return peers;
    }

    public void ValidateAssessmentsOf(int userId)
    {
        List<IPeerAssessment> allAssessmentsOfUser = _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
        if (allAssessmentsOfUser.Count == _peers.Count)
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

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}