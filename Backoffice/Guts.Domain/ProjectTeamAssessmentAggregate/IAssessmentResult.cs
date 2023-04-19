using System.Collections.Generic;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IAssessmentResult
    {
        User Subject { get; }

        IPeerAssessment SelfAssessment { get; }
        IReadOnlyList<IPeerAssessment> PeerAssessments { get; }
        IAssessmentSubResult AverageResult { get; }
        IAssessmentSubResult EffortResult { get; }
        IAssessmentSubResult CooperationResult { get; }
        IAssessmentSubResult ContributionResult { get; }

        void ClearPeerAssessments();
    }
}