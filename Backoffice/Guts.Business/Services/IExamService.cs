using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services
{
    public interface IExamService
    {
        Task<IReadOnlyList<Exam>> GetExamsAsync(int? courseId);
        Task<Exam> CreateExamAsync(int courseId, string name);
        Task<Exam> GetExamAsync(int id);
        Task<ExamPart> CreateExamPartAsync(int examId, ExamPartDto examPartDto);
        Task<ExamPart> GetExamPartAsync(int examId, int examPartId);
        Task DeleteExamPartAsync(int id, int examPartId);
        Task<IList<dynamic>> CalculateExamScores(int examId);
    }
}