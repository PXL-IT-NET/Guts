using System;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    public interface IProjectAssessment : IEntity
    {
        string Description { get; }
        DateTime OpenOnUtc { get; }
        DateTime DeadlineUtc { get; }
        //  IReadOnlyCollection<IProjectTeamAssessment> TeamAssessments { get; }
    }
}