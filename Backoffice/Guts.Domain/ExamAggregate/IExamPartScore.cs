using System.Collections.Generic;

namespace Guts.Domain.ExamAggregate
{
    public interface IExamPartScore
    {
        string ExamPartDescription { get; }
        double Score { get; }
        double MaximumScore { get; }
        IReadOnlyList<IAssignmentEvaluationScore> AssignmentEvaluationScores { get; }
        void AddAssignmentScore(IAssignmentEvaluationScore assignmentEvaluationScore);
    }
}