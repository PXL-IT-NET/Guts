using System;
using System.Collections.Generic;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Domain.ExamAggregate
{
    public interface IExamPart : IEntity
    {
        int ExamId { get; }
        string Name { get; }
        double MaximumScore { get; }
        IReadOnlyCollection<IAssignmentEvaluation> AssignmentEvaluations { get; }
        DateTime Deadline { get; }

        AssignmentEvaluation AddAssignmentEvaluation(Assignment assignment, int maximumScore,
            int numberOfTestsAlreadyGreenAtStart);
        IExamPartScore CalculateScoreForUser(int userId, IExamPartTestResultCollection examPartTestResults);
    }
}