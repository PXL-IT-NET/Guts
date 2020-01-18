using System.Collections.Generic;

namespace Guts.Domain.ExamAggregate
{
    public class ExamTestResultCollection : ValueObject<ExamTestResultCollection>, IExamTestResultCollection
    {
        private readonly Dictionary<int, IExamPartTestResultCollection> _examPartTestResults;

        public ExamTestResultCollection()
        {
            _examPartTestResults = new Dictionary<int, IExamPartTestResultCollection>();
        }

        public void AddExamPartResults(int examPartId, IExamPartTestResultCollection examPartTestResults)
        {
            _examPartTestResults.Add(examPartId, examPartTestResults);
        }

        public IExamPartTestResultCollection GetExamPartResults(int examPartId)
        {
            return _examPartTestResults[examPartId];
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var key in _examPartTestResults.Keys)
            {
                yield return key;
            }

            foreach (var value in _examPartTestResults.Values)
            {
                yield return value;
            }
        }
    }
}