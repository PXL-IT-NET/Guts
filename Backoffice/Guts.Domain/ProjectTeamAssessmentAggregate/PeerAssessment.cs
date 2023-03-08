using Guts.Common;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal class PeerAssessment : Entity, IPeerAssessment
    {
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

        public string Explanation { get; set; }

        public bool IsSelfAssessment => User.Id == Subject.Id;

        private PeerAssessment() { } //Used by EF

        internal PeerAssessment(User user, User subject)
        {
            Contracts.Require(user != null, "A user must be provided.");
            Contracts.Require(user.Id > 0, "An existing user must be provided.");
            Contracts.Require(subject != null, "A subject must be provided.");
            Contracts.Require(subject.Id > 0, "An existing subject must be provided.");

            User = user;
            Subject = subject;

            CooperationScore = AssessmentScore.Average;
            ContributionScore = AssessmentScore.Average;
            EffortScore = AssessmentScore.Average;
        }

        public void SetScores(AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore)
        {
            CooperationScore = cooperationScore;
            ContributionScore = contributionScore;
            EffortScore = effortScore;
        }
    }
}