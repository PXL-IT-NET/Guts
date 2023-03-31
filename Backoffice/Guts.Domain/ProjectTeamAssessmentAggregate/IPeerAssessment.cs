using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IPeerAssessment
    {
        int ProjectTeamAssessmentId { get; }
        User Subject { get; }
        User User { get; }
        AssessmentScore ContributionScore { get; }
        AssessmentScore CooperationScore { get; }
        AssessmentScore EffortScore { get; }

        bool IsSelfAssessment { get; }

        string Explanation { get; }

        void SetScores(AssessmentScore cooperationScore, AssessmentScore contributionScore, AssessmentScore effortScore, string explanation);
    }
}