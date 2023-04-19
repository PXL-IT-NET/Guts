using System.Collections.Generic;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    public interface IAssessmentResultFactory
    {
        IAssessmentResult Create(User subject, IReadOnlyCollection<IPeerAssessment> allPeerAssessments);
    }
}