using System.Collections.Generic;

namespace Guts.Domain.ExamAggregate
{
    public class ExamPartScore: Entity, IExamPartScore
    {
        private readonly List<IAssignmentEvaluationScore> _assignmentScores;

        public string ExamPartDescription { get;}
        public double Score { get; private set; }

        public double MaximumScore { get; }

        public IReadOnlyList<IAssignmentEvaluationScore> AssignmentEvaluationScores => _assignmentScores;

        public ExamPartScore(ExamPart examPart)
        {
            ExamPartDescription = examPart.Name;
            MaximumScore = examPart.MaximumScore;
            _assignmentScores = new List<IAssignmentEvaluationScore>();
            Score = 0.0;
        }

        public void AddAssignmentScore(IAssignmentEvaluationScore assignmentEvaluationScore)
        {
            _assignmentScores.Add(assignmentEvaluationScore);
            Score += assignmentEvaluationScore.Score;

        }
    }
}