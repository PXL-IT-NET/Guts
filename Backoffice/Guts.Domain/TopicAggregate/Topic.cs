using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.TopicAggregate;

public abstract class Topic : AggregateRoot, ITopic
{
    private string _code;

    public Code Code
    {
        get => _code;
        set
        {
            _code = value;
            if (string.IsNullOrEmpty(Description))
            {
                Description = value;
            }
        }
    }

    public string Description { get; set; }

    public Course Course { get; set; }
    public int CourseId { get; set; }

    public IPeriod Period { get; set; }
    public int PeriodId { get; set; }

    public ICollection<Assignment> Assignments { get; set; } = new HashSet<Assignment>();

    protected Topic()
    {
    }

    protected Topic(int id) : base(id)
    {
            
    }

    public Assignment AddAssignment(Assignment assignment)
    {
        Contracts.Require(assignment != null, "Assignment cannot be null.");
        Assignments ??= new HashSet<Assignment>();
        assignment.TopicId = Id;
        assignment.Topic = this;
        Assignments.Add(assignment);
        return assignment;
    }

    public void LoadAssignments(IEnumerable<Assignment> assignments)
    {
        Contracts.Require(assignments != null, "Assignments cannot be null.");
        Assignments = [.. assignments];
    }
}