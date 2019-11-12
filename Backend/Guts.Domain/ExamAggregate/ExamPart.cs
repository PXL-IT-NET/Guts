using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Domain.ExamAggregate
{
    public class ExamPart : Entity
    {
        private readonly HashSet<AssignmentEvaluation> _assignmentEvaluations;

        public int ExamId { get; private set; }

        [Required]
        public string Name { get; private set; }

        public DateTime Deadline { get; private set; }

        public virtual IReadOnlyCollection<AssignmentEvaluation> AssignmentEvaluations => _assignmentEvaluations;

        private ExamPart() { } //Needed for EF Core

        internal ExamPart(int examId, string name, DateTime deadline)
        {
            Contracts.Require(examId >= 0, "The exam id cannot be negative.");
            Contracts.Require(!string.IsNullOrEmpty(name), "An exam part cannot have an empty name.");
            Contracts.Require(deadline.Kind == DateTimeKind.Utc, "The deadline must be an UTC time.");

            _assignmentEvaluations = new HashSet<AssignmentEvaluation>();
            ExamId = examId;
            Name = name;
            Deadline = deadline;
        }

        public AssignmentEvaluation AddAssignmentEvaluation(Assignment assignment, int maximumScore, int numberOfTestsAlreadyGreenAtStart)
        {
            var assignmentEvaluation = new AssignmentEvaluation(Id, assignment, maximumScore, numberOfTestsAlreadyGreenAtStart);
            _assignmentEvaluations.Add(assignmentEvaluation);
            return assignmentEvaluation;
        }
    }
}