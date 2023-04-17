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
        private readonly IAssessmentResultFactory _assessmentResultFactory;
        private readonly HashSet<IPeerAssessment> _peerAssessments;

        public IProjectAssessment ProjectAssessment { get; private set; }

        public IProjectTeam Team { get; private set; }

        public IReadOnlyCollection<IPeerAssessment> PeerAssessments => _peerAssessments;

        public bool IsComplete => (int)Math.Pow(Team.TeamUsers.Count, 2) == _peerAssessments.Count;

        private ProjectTeamAssessment(IAssessmentResultFactory assessmentResultFactory)
        {
            _assessmentResultFactory = assessmentResultFactory;
            _peerAssessments = new HashSet<IPeerAssessment>();
        }

        private ProjectTeamAssessment(IProjectAssessment projectAssessment, IProjectTeam team, IAssessmentResultFactory assessmentResultFactory) : this(assessmentResultFactory)
        {
            Contracts.Require(team != null, "A team must be provided.");
            Contracts.Require(team!.TeamUsers.Any(), "The team has no members.");
            Contracts.Require(team.TeamUsers.First().User != null, "The users of team must be loaded with user data.");
            Team = team;

            Contracts.Require(projectAssessment!.Id > 0, "A project team assessment can only created for an existing (stored) project assessment.");
            ProjectAssessment = projectAssessment;
            _assessmentResultFactory = assessmentResultFactory; 
        }

        public class Factory : IProjectTeamAssessmentFactory
        {
            private readonly IAssessmentResultFactory _assessmentResultFactory;

            public Factory(IAssessmentResultFactory assessmentResultFactory)
            {
                _assessmentResultFactory = assessmentResultFactory;
            }

            public IProjectTeamAssessment CreateNew(IProjectAssessment projectAssessment, IProjectTeam team)
            {
                Contracts.Require(projectAssessment != null, "A project assessment must be provided to create a project team assessment.");
                Contracts.Require(projectAssessment.OpenOnUtc <= DateTime.UtcNow, "The project team assessment cannot be created because the project assessment is not opened yet.");
                return new ProjectTeamAssessment(projectAssessment, team, _assessmentResultFactory);
            }
        }

        public IPeerAssessment AddOrUpdatePeerAssessment(int userId, int subjectId,
            AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore,
            string explanation)
        {
            RequireUserToBeATeamMember(userId);
            RequireUserToBeATeamMember(subjectId);
            Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to change a peer assessment.");

            IPeerAssessment peerAssessment = _peerAssessments.SingleOrDefault(pa => pa.User.Id == userId && pa.Subject.Id == subjectId);
            if (peerAssessment == null)
            {
                User user = Team.TeamUsers.Single(tu => tu.UserId == userId).User;
                User subject = Team.TeamUsers.Single(tu => tu.UserId == subjectId).User;
                peerAssessment = new PeerAssessment(Id, user, subject);
                _peerAssessments.Add(peerAssessment);
            }

            peerAssessment.SetScores(cooperationScore, contributionScore, effortScore, explanation);
            return peerAssessment;
        }

        public IReadOnlyList<IPeerAssessment> GetPeerAssessmentsOf(int userId)
        {
            RequireUserToBeATeamMember(userId);

            return _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
        }

        public IReadOnlyList<IPeerAssessment> GetMissingPeerAssessmentsOf(int userId)
        {
            RequireUserToBeATeamMember(userId);
            User user = Team.TeamUsers.Single(tu => tu.UserId == userId).User;
            List<IPeerAssessment> storedAssessments = _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
            List<IPeerAssessment> missingAssessments = new List<IPeerAssessment>();

            foreach (IProjectTeamUser teamUser in Team.TeamUsers)
            {
                if (storedAssessments.All(a => a.Subject.Id != teamUser.UserId))
                {
                    missingAssessments.Add(new PeerAssessment(Id, user, teamUser.User));
                }
            }
            return missingAssessments;
        }

        public IReadOnlyList<User> GetPeersThatNeedToEvaluateOthers()
        {
            var peers = new List<User>();
            foreach (User user in Team.TeamUsers.Select(tu => tu.User))
            {
                if (_peerAssessments.Count(pa => pa.User.Id == user.Id) < Team.TeamUsers.Count)
                {
                    peers.Add(user);
                }
            }
            return peers;
        }

        public IAssessmentResult GetAssessmentResultFor(int userId)
        {
            RequireUserToBeATeamMember(userId);
            Contracts.Require(IsComplete, "The team assessment is not completed by all peers. Assessment results are only available when all members completed the assessment.");

            User subject = Team.TeamUsers.Single(tu => tu.UserId == userId).User;

            return _assessmentResultFactory.Create(subject, PeerAssessments);
        }

        public void ValidateAssessmentsOf(int userId)
        {
            List<IPeerAssessment> allAssessmentsOfUser = _peerAssessments.Where(pa => pa.User.Id == userId).ToList();
            if (allAssessmentsOfUser.Count == Team.TeamUsers.Count)
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

        private void RequireUserToBeATeamMember(int userId)
        {
            Contracts.Require(Team.TeamUsers.Any(tu => tu.UserId == userId), $"The user with id '{userId}' is not a member of the team.");
        }
    }
}