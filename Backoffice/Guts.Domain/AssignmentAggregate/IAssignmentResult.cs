namespace Guts.Domain.AssignmentAggregate;

public interface IAssignmentResult
{
    int AssignmentId { get; }
    int UserId { get; }
    int NumberOfPassingTests { get; }
}