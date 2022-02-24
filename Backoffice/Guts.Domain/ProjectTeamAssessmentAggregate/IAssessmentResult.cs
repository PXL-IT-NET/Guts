using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using System.Collections.Generic;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IAssessmentResult
    {
        User Subject { get; }

        IPeerAssessment SelfAssessment { get; }
        IReadOnlyList<IPeerAssessment> PeerAssessments { get; }

        AssessmentScore SelfAssessmentScore { get; }
        AssessmentScore PeerAssessmentScore { get; }
        double SelfAssessmentFactor { get; }
        double PeerAssessmentFactor { get; }   
    }
}