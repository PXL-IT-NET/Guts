using System;
using System.Collections.Generic;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Domain.AssessmentResultAggregate
{
    internal interface IAssessmentSubResultFactory
    {
        IAssessmentSubResult Create(int subjectId, IReadOnlyCollection<IPeerAssessment> allPeerAssessments,
            Func<IPeerAssessment, double> calculateScore);
    }
}