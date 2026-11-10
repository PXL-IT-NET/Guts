using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Application.Services.Exam;

public interface IExamTestResultLoader
{
    Task<ExamTestResultCollection> GetExamResultsAsync(IExam exam);
}