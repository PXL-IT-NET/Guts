using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services.Exam
{
    public interface IExamService
    {
        Task<IReadOnlyList<IExam>> GetExamsAsync(int? courseId);
        Task<IExam> CreateExamAsync(int courseId, string name);
        Task<IExam> GetExamAsync(int id);
        Task<IExamPart> CreateExamPartAsync(int examId, ExamPartDto examPartDto);
        Task DeleteExamPartAsync(IExam exam, int examPartId);
        Task<IList<ExpandoObject>> CalculateExamScoresForCsvAsync(int examId);
    }
}