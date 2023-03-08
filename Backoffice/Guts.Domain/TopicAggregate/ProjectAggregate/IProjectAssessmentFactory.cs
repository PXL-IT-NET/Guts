using System;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    public interface IProjectAssessmentFactory
    {
        IProjectAssessment CreateNew(int projectId, string description, DateTime openOnUtc, DateTime deadlineUtc);
    }
}