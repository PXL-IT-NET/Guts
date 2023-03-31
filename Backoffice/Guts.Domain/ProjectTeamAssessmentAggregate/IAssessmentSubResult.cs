using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IAssessmentSubResult
    {
        double Value { get; }
        double SelfValue { get; }
        double PeerValue { get; }

        AssessmentScore Score { get; }
        AssessmentScore SelfScore { get; }
        AssessmentScore PeerScore { get; }

        double AverageValue { get; }
        double AverageSelfValue { get; }
        double AveragePeerValue { get; }
    }
}