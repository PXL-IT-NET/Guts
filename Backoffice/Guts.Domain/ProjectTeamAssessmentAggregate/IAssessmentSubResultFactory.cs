using System;
using System.Collections.Generic;

namespace Guts.Domain.ProjectTeamAssessmentAggregate
{
    internal interface IAssessmentSubResultFactory
    {
        IAssessmentSubResult Create(int subjectId, IReadOnlyCollection<IPeerAssessment> allPeerAssessments,
            Func<IPeerAssessment, double> calculateScore);
    }
}