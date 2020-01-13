using Guts.Domain.AssignmentAggregate;

namespace Guts.Domain.ExamAggregate
{
    public interface IExamPartTestResultCollection
    {
        IAssignmentResult GetAssignmentResultFor(int userId, int assignmentId);
    }
}