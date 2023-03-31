using Guts.Common;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class PeerAssessment : Entity, IPeerAssessment
    {
        public int ProjectTeamAssessmentId { get; private set; }

        public User User { get; private set; }

        public User Subject { get; private set; }

        /// <summary>
        /// Score on communication, cooperation and honoring agreements
        /// </summary>
        public AssessmentScore CooperationScore { get; private set; }

        /// <summary>
        /// Score on the actual technical contribution to the project
        /// </summary>
        public AssessmentScore ContributionScore { get; private set; }

        /// <summary>
        /// Score on the effort, time investment
        /// </summary>
        public AssessmentScore EffortScore { get; private set; }

        public string Explanation { get; private set; }

        public bool IsSelfAssessment => User.Id == Subject.Id;

        private PeerAssessment() { } //Used by EF

        internal PeerAssessment(int projectTeamAssessmentId, User user, User subject)
        {
            Contracts.Require(projectTeamAssessmentId > 0, "An existing project team assessment identifier must be provided.");
            Contracts.Require(user != null, "A user must be provided.");
            Contracts.Require(user.Id > 0, "An existing user must be provided.");
            Contracts.Require(subject != null, "A subject must be provided.");
            Contracts.Require(subject.Id > 0, "An existing subject must be provided.");

            ProjectTeamAssessmentId = projectTeamAssessmentId;
            User = user;
            Subject = subject;

            CooperationScore = AssessmentScore.NoAddedValue;
            ContributionScore = AssessmentScore.NoAddedValue;
            EffortScore = AssessmentScore.NoAddedValue;
        }

        public void SetScores(AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore, string explanation)
        {
            CooperationScore = cooperationScore;
            ContributionScore = contributionScore;
            EffortScore = effortScore;
            Explanation = explanation;
        }
    }
}