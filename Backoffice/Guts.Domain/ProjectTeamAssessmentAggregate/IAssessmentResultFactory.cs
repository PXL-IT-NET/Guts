using System.Collections.Generic;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal interface IAssessmentResultFactory
    {
        IAssessmentResult Create(User subject, IReadOnlyCollection<IPeerAssessment> allPeerAssessments);
    }
}