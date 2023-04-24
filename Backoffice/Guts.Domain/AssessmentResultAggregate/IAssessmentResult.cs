using System.Collections.Generic;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.AssessmentResultAggregate
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
    }
}