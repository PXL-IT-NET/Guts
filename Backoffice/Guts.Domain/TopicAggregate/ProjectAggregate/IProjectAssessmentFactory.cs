using System;

namespace Guts.Domain.TopicAggregate
{
    public interface IProjectAssessmentFactory
    {
        IProjectAssessment CreateNew(int projectId, string description, DateTime openOnUtc, DateTime deadlineUtc);
    }
}