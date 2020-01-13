using System;
using System.Collections.Generic;

namespace Guts.Domain.ExamAggregate
{
    public class AssignmentEvaluationScore : ValueObject<AssignmentEvaluationScore>, IAssignmentEvaluationScore
    {
        private readonly double _scorePerTest;
        private readonly int _numberOfTestsAlreadyGreenAtStart;

        public int AssignmentEvaluationId { get; }
        public string AssignmentDescription { get; }
        public double Score => Math.Max(NumberOfPassedTests - _numberOfTestsAlreadyGreenAtStart, 0) * _scorePerTest;
        public double MaximumScore { get; }
        public int NumberOfTests { get;}
        public int NumberOfPassedTests { get; set; }

        internal AssignmentEvaluationScore(AssignmentEvaluation assignmentEvaluation)
        {
            AssignmentEvaluationId = assignmentEvaluation.Id;
            var assignment = assignmentEvaluation.Assignment;
            AssignmentDescription = assignment.Description;
            MaximumScore = assignmentEvaluation.MaximumScore;
            NumberOfTests = assignment.Tests.Count;
            NumberOfPassedTests = 0;

            _numberOfTestsAlreadyGreenAtStart = assignmentEvaluation.NumberOfTestsAlreadyGreenAtStart;
            _scorePerTest = MaximumScore /
                            Convert.ToDouble(NumberOfTests - assignmentEvaluation.NumberOfTestsAlreadyGreenAtStart);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return AssignmentEvaluationId;
            yield return NumberOfPassedTests;
        }
    }
}