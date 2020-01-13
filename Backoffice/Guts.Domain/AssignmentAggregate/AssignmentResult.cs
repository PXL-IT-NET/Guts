using System.Collections.Generic;
using System.Linq;
using Guts.Domain.TestRunAggregate;

namespace Guts.Domain.AssignmentAggregate
{
    public class AssignmentResult : ValueObject<AssignmentResult>, IAssignmentResult
    {
        private readonly List<TestResult> _lastTestResults;
        public int AssignmentId { get; set; }
        public int UserId { get; set; }

        public IReadOnlyList<TestResult> LastTestResults => _lastTestResults;

        public int NumberOfPassingTests
        {
            get { return LastTestResults.Count(r => r.Passed); }
        }

        internal AssignmentResult(int assignmentId, int userId, IEnumerable<TestResult> lastTestResults = null)
        {
            AssignmentId = assignmentId;
            UserId = userId;
            _lastTestResults = new List<TestResult>();
            if (lastTestResults != null)
            {
                _lastTestResults.AddRange(lastTestResults);
            }
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return AssignmentId;
            yield return UserId;
            yield return NumberOfPassingTests;
        }
    }
}