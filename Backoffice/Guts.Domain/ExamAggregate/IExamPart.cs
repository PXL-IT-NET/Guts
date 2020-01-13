using System;
using System.Collections.Generic;

namespace Guts.Domain.ExamAggregate
{
    public interface IExamPart
    {
        int Id { get; }
        int ExamId { get; }
        double MaximumScore { get; }
        IReadOnlyCollection<IAssignmentEvaluation> AssignmentEvaluations { get; }
        DateTime Deadline { get; }
        
        IExamPartScore CalculateScoreForUser(int userId, IExamPartTestResultCollection examPartTestResults);
    }
}