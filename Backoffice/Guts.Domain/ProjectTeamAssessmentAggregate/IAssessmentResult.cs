using System.Collections.Generic;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IAssessmentResult
    {
        User Subject { get; }

        IPeerAssessment SelfAssessment { get; }
        IReadOnlyList<IPeerAssessment> PeerAssessments { get; }

        double TeamAverage { get; }
        AssessmentScore SelfAssessmentScore { get; }
        AssessmentScore PeerAssessmentScore { get; }
        double SelfAssessmentFactor { get; }
        double PeerAssessmentFactor { get; }
    }
}