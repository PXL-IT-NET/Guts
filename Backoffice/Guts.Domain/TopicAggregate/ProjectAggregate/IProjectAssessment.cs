using Guts.Domain.ProjectTeamAssessmentAggregate;
using System;
using System.Collections.Generic;

namespace Guts.Domain.TopicAggregate
{
    public interface IProjectAssessment : IEntity
    {
        string Description { get; }
        DateTime OpenOnUtc { get; }
        DateTime DeadlineUtc { get; }
      //  IReadOnlyCollection<IProjectTeamAssessment> TeamAssessments { get; }
    }
}