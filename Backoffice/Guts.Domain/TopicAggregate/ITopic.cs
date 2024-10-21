using System.Collections.Generic;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.TopicAggregate
{
    public interface ITopic : IEntity
    {
        ICollection<Assignment> Assignments { get; set; }
        Code Code { get; }
        Course Course { get; }
        int CourseId { get; }
        string Description { get; }
        Period Period { get; }
        int PeriodId { get; }
    }
}