using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Domain.ExamAggregate
{
    public class ExamPart : Entity, IExamPart
    {
        private readonly HashSet<IAssignmentEvaluation> _assignmentEvaluations;

        public int ExamId { get; private set; }

        [Required]
        public string Name { get; private set; }

        public DateTime Deadline { get; private set; }

        public IReadOnlyCollection<IAssignmentEvaluation> AssignmentEvaluations => _assignmentEvaluations;

        public double MaximumScore
        {
            get { return AssignmentEvaluations.Sum(ae => ae.MaximumScore); }
        }

        private ExamPart() { } //Needed for EF Core

        internal ExamPart(int examId, string name, DateTime deadline)
        {
            Contracts.Require(examId >= 0, "The exam id cannot be negative.");
            Contracts.Require(!string.IsNullOrEmpty(name), "An exam part cannot have an empty name.");
            Contracts.Require(deadline.Kind == DateTimeKind.Utc, "The deadline must be an UTC time.");

            _assignmentEvaluations = new HashSet<IAssignmentEvaluation>();
            ExamId = examId;
            Name = name;
            Deadline = deadline;
        }

        public AssignmentEvaluation AddAssignmentEvaluation(Assignment assignment, int maximumScore, int numberOfTestsAlreadyGreenAtStart)
        {
            bool isAssignmentAlreadyAdded = _assignmentEvaluations.Any(ae => ae.AssignmentId == assignment.Id);
            Contracts.Require(!isAssignmentAlreadyAdded, $"An evaluation for assignment '{assignment.Code}' can only be added once.");

            var assignmentEvaluation = new AssignmentEvaluation(Id, assignment, maximumScore, numberOfTestsAlreadyGreenAtStart);
            _assignmentEvaluations.Add(assignmentEvaluation);
            return assignmentEvaluation;
        }

        public IExamPartScore CalculateScoreForUser(int userId, IExamPartTestResultCollection examPartTestResults)
        {
            var examPartScore = new ExamPartScore(this);
            foreach (var assignmentEvaluation in AssignmentEvaluations)
            {
                var assignmentResult = examPartTestResults.GetAssignmentResultFor(userId, assignmentEvaluation.AssignmentId);
                var assignmentEvaluationScore = assignmentEvaluation.CalculateScore(assignmentResult);
                examPartScore.AddAssignmentScore(assignmentEvaluationScore);
            }
            return examPartScore;
        }
    }
}