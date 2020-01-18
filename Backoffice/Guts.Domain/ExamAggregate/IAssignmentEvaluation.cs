using Guts.Domain.AssignmentAggregate;

namespace Guts.Domain.ExamAggregate
{
    public interface IAssignmentEvaluation : IEntity
    {
        Assignment Assignment { get; }
        int AssignmentId { get; }
        int ExamPartId { get; }
        int MaximumScore { get; }
        int NumberOfTestsAlreadyGreenAtStart { get; }
        IAssignmentEvaluationScore CalculateScore(IAssignmentResult assignmentResult);
    }
}