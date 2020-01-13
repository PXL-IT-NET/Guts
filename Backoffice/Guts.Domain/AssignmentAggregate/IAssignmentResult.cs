namespace Guts.Domain.AssignmentAggregate
{
    public interface IAssignmentResult
    {
        int AssignmentId { get; set; }
        int UserId { get; set; }
        int NumberOfPassingTests { get; }
    }
}