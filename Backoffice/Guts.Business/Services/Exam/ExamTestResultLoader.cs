using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services.Exam
{
    public class ExamTestResultLoader : IExamTestResultLoader
    {
        private readonly ITestResultRepository _testResultRepository;

        public ExamTestResultLoader(ITestResultRepository testResultRepository)
        {
            _testResultRepository = testResultRepository;
        }
        public async Task<ExamTestResultCollection> GetExamResultsAsync(IExam exam)
        {
            var examResults = new ExamTestResultCollection();
            foreach (var examPart in exam.Parts)
            {
                var assignmentIds = examPart.AssignmentEvaluations.Select(ae => ae.AssignmentId).ToArray();
                var examPartTestResults =
                    await _testResultRepository.GetLastTestResultsOfAssignmentsAsync(assignmentIds, examPart.Deadline);
                examResults.AddExamPartResults(examPart.Id,
                    ExamPartTestResultCollection.FromLastTestResults(examPartTestResults));
            }

            return examResults;
        }
    }
}