using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ExamAggregate
{
    public class ExamScore : Entity, IExamScore
    {
        private readonly List<IExamPartScore> _examPartScores;
        public string FirstName { get; }
        public string LastName { get; }
        public double Score { get; private set; }
        public double NormalizedScore => Math.Round(Score * (NormalizedMaximumScore / MaximumScore), MidpointRounding.AwayFromZero);
        public double MaximumScore { get;}
        public double NormalizedMaximumScore { get; }
        public IReadOnlyList<IExamPartScore> ExamPartScores => _examPartScores;

        internal ExamScore(User user, Exam exam)
        {
            FirstName = user.FirstName?.Trim() ?? "";
            LastName = user.LastName?.Trim() ?? "";
            NormalizedMaximumScore = exam.MaximumScore;
            MaximumScore = exam.Parts.Sum(p => p.MaximumScore);
            _examPartScores = new List<IExamPartScore>();
            Score = 0.0;
        }

        internal void AddExamPartScore(IExamPartScore examPartScore)
        {
            _examPartScores.Add(examPartScore);
            Score += examPartScore.Score;
        }

        public ExpandoObject ToCsvRecord()
        {
            var result = new ExpandoObject();
            result.TryAdd("LastName", LastName);
            result.TryAdd("FirstName", FirstName);

            foreach (var examPartScore in ExamPartScores)
            {
                foreach (var assignmentEvaluationScore in examPartScore.AssignmentEvaluationScores)
                {
                    result.TryAdd($"{assignmentEvaluationScore.AssignmentDescription}_NbrPassed({assignmentEvaluationScore.NumberOfTests})", 
                        assignmentEvaluationScore.NumberOfPassedTests);
                    result.TryAdd($"{assignmentEvaluationScore.AssignmentDescription}_Score({assignmentEvaluationScore.MaximumScore})", 
                        assignmentEvaluationScore.Score);
                }
                result.TryAdd($"Total_{examPartScore.ExamPartDescription}({examPartScore.MaximumScore})", examPartScore.Score);
            }
            result.TryAdd($"Total({MaximumScore})", Score);
            result.TryAdd($"Total({NormalizedMaximumScore})", NormalizedScore);
            return result;
        }
    }
}