using System.Collections.Generic;
using System.Linq;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Domain.ExamAggregate
{
    public class ExamPartTestResultCollection : ValueObject<ExamPartTestResultCollection>, IExamPartTestResultCollection
    {
        private readonly Dictionary<int, IDictionary<int, AssignmentResult>> _userAssignmentDictionary;

        private ExamPartTestResultCollection()
        {
            _userAssignmentDictionary = new Dictionary<int, IDictionary<int, AssignmentResult>>();
        }

        public static ExamPartTestResultCollection FromLastTestResults(IEnumerable<TestResult> lastTestResults)
        {
            var collection = new ExamPartTestResultCollection();
            foreach (var assignmentGroup in lastTestResults.GroupBy(result => result.Test.AssignmentId))
            {
                var userResultDictionary = new Dictionary<int, AssignmentResult>();
                foreach (var userResultGroup in assignmentGroup.GroupBy(result => result.UserId))
                {
                    userResultDictionary.Add(userResultGroup.Key, new AssignmentResult(assignmentGroup.Key, userResultGroup.Key, userResultGroup));
                }
                collection._userAssignmentDictionary.Add(assignmentGroup.Key, userResultDictionary);
            }

            return collection;
        }

        public IAssignmentResult GetAssignmentResultFor(int userId, int assignmentId)
        {
            if (_userAssignmentDictionary.TryGetValue(assignmentId,
                out IDictionary<int, AssignmentResult> assignmentResultDictionary))
            {
                if (assignmentResultDictionary.TryGetValue(userId, out AssignmentResult assignmentResult))
                {
                    return assignmentResult;
                }
            }
            return new AssignmentResult(assignmentId, userId);
        }


        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return _userAssignmentDictionary;
        }
    }
}