using System.Linq;
using Guts.Common;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Domain.ExamAggregate
{
    public class AssignmentEvaluation : Entity
    {
        public int AssignmentId { get; private set; }

        public int ExamPartId { get; private set; }

        public int MaximumScore { get; private set; }

        public int NumberOfTestsAlreadyGreenAtStart { get; private set; }

        private AssignmentEvaluation() { }

        internal AssignmentEvaluation(int examPartId, 
            Assignment assignment, int maximumScore, int numberOfTestsAlreadyGreenAtStart)
        {
            Contracts.Require(examPartId >= 0, "The exam part id must be greater than or equal to zero.");
            Contracts.Require(assignment.Id > 0, "The assignment id must be greater than zero.");
            Contracts.Require(maximumScore > 0, "The maximum score must be greater than zero.");
            Contracts.Require(numberOfTestsAlreadyGreenAtStart >= 0, "The number of tests that are already green cannot be negative.");
            Contracts.Require(assignment.Tests.Any(), "The assignment must have at least one test");
            Contracts.Require(numberOfTestsAlreadyGreenAtStart < assignment.Tests.Count,
                $"The number of tests that are green at start ({numberOfTestsAlreadyGreenAtStart}) " +
                $"must be smaller than the total number of tests ({assignment.Tests.Count}).");

            ExamPartId = examPartId;
            AssignmentId = assignment.Id;
            MaximumScore = maximumScore;
            NumberOfTestsAlreadyGreenAtStart = numberOfTestsAlreadyGreenAtStart;
        }
    }
}