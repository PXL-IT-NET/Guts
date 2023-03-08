using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class ProjectTeamAssessment : AggregateRoot, IProjectTeamAssessment
    {
        private readonly HashSet<IPeerAssessment> _peerAssessments;

        public IProjectAssessment ProjectAssessment { get; private set; }

        public IProjectTeam Team { get; private set; }

        public IReadOnlyCollection<IPeerAssessment> PeerAssessments => _peerAssessments;

        public bool IsComplete => (int)Math.Pow(Team.TeamUsers.Count, 2) == _peerAssessments.Count;

        private ProjectTeamAssessment()
        {
            _peerAssessments = new HashSet<IPeerAssessment>();
        }

        private ProjectTeamAssessment(IProjectAssessment projectAssessment, IProjectTeam team) : this()
        {
            Contracts.Require(team != null, "A team must be provided.");
            Contracts.Require(team!.TeamUsers.Any(), "The users of team must be loaded.");
            Contracts.Require(team.TeamUsers.First().User != null, "The users of team must be loaded with user data.");
            Team = team;

            Contracts.Require(projectAssessment != null, "A project assessment must be provided to create a project team assessment.");
            Contracts.Require(projectAssessment!.Id > 0, "A project team assessment can only created for an existing (stored) project assessment.");
            ProjectAssessment = projectAssessment;
        }

        public class Factory : IProjectTeamAssessmentFactory
        {
            public IProjectTeamAssessment CreateNew(IProjectAssessment projectAssessment, IProjectTeam team)
            {
                Contracts.Require(projectAssessment.OpenOnUtc <= DateTime.UtcNow, "The project team assessment cannot be created because the project assessment is not opened yet.");
                return new ProjectTeamAssessment(projectAssessment, team);
            }
        }

        public void AddOrReplacePeerAssessment(int userId, int subjectId,
            AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore)
        {
            RequireUserToBeATeamMember(userId);
            RequireUserToBeATeamMember(subjectId);
            Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to change a peer assessment.");

            IPeerAssessment peerAssessment = _peerAssessments.SingleOrDefault(pa => pa.User.Id == userId && pa.Subject.Id == subjectId);
            if (peerAssessment == null)
            {
                User user = Team.TeamUsers.SingleOrDefault(tu => tu.UserId == userId)?.User;
                Contracts.Require(user != null, $"Cannot add a peer assessment for user with id '{userId}'. The user is not a member of the team.");

                User subject = Team.TeamUsers.SingleOrDefault(tu => tu.UserId == subjectId)?.User;
                Contracts.Require(user != null, $"Cannot add a peer assessment for subject with id '{subjectId}'. The subject is not a member of the team.");

                peerAssessment = new PeerAssessment(user, subject);

                _peerAssessments.Add(peerAssessment);
            }

            peerAssessment.SetScores(cooperationScore, contributionScore, effortScore);
        }

        public IReadOnlyList<User> GetPeersToEvaluateFor(int userId)
        {
            RequireUserToBeATeamMember(userId);

            var peers = new List<User>();
            foreach (User subject in Team.TeamUsers.Select(tu => tu.User))
            {
                if (!_peerAssessments.Any(pa => pa.User.Id == userId && pa.Subject.Id == subject.Id))
                {
                    peers.Add(subject);
                }
            }
            return peers;
        }

        public IAssessmentResult GetAssessmentResultFor(int userId)
        {
            RequireUserToBeATeamMember(userId);
            Contracts.Require(IsComplete, "The team assessment is not completed by all peers. Assessment results are only available when all members completed the assessment.");

            User subject = Team.TeamUsers.Single(tu => tu.UserId == userId).User;

            return new AssessmentResult(subject, PeerAssessments);
        }

        private void RequireUserToBeATeamMember(int userId)
        {
            Contracts.Require(Team.TeamUsers.Any(tu => tu.UserId == userId), $"The user with id '{userId}' is not a member of the team.");
        }
    }
}