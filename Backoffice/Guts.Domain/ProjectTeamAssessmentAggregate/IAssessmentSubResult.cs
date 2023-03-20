using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IAssessmentSubResult
    {
        double TeamAverage { get; }
        double Average { get; }
        double SelfAverage { get; } 
        double PeerAverage { get; }
        AssessmentScore Score { get; }
        AssessmentScore SelfScore { get; }
        AssessmentScore PeerScore { get; }
    }
}