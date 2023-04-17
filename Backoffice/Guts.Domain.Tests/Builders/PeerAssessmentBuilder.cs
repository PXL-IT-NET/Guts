using Guts.Common.Extensions;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Moq;

namespace Guts.Domain.Tests.Builders
{
    internal class PeerAssessmentBuilder : BaseBuilder<PeerAssessment>
    {
        public PeerAssessmentBuilder()
        {
            Item = new PeerAssessment(Random.NextPositive(), 
                new UserBuilder().WithId().Build(),
                new UserBuilder().WithId().Build());

            SetProperty(pa => pa.ContributionScore, AssessmentScore.Average);
            SetProperty(pa => pa.EffortScore, AssessmentScore.Average);
            SetProperty(pa => pa.CooperationScore, AssessmentScore.Average);
            SetProperty(pa => pa.Explanation, Random.NextString());
        }

        public PeerAssessmentBuilder WithProjectTeamAssessmentId(int id)
        {
            SetProperty(pa => pa.ProjectTeamAssessmentId, id);
            return this;
        }

        public PeerAssessmentBuilder WithUserAndSubject(User user, User subject)
        {
            SetProperty(pa => pa.User, user);
            SetProperty(pa => pa.Subject, subject);
            return this;
        }

        public PeerAssessmentBuilder WithScores(AssessmentScore contributionScore, AssessmentScore effortScore, AssessmentScore cooperationScore)
        {
            SetProperty(pa => pa.ContributionScore, contributionScore);
            SetProperty(pa => pa.EffortScore, effortScore);
            SetProperty(pa => pa.CooperationScore, cooperationScore);
            return this;
        }
    }
}