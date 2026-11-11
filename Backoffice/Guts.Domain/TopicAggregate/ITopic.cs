using System.Collections.Generic;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.TopicAggregate;

public interface ITopic : IEntity
{
    int Id { get; }
    ICollection<Assignment> Assignments { get; set; }
    Code Code { get; }
    Course Course { get; }
    int CourseId { get; }
    string Description { get; }
    IPeriod Period { get; }
    int PeriodId { get; }
    Assignment AddAssignment(Assignment assignment);
    void LoadAssignments(IEnumerable<Assignment> assignments);
}