using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services.Exam
{
    public interface IExamTestResultLoader
    {
        Task<ExamTestResultCollection> GetExamResultsAsync(IExam exam);
    }
}