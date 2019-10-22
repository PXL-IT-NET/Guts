using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Services
{
    public interface IExamService
    {
        Task<Exam> CreateExamAsync(int courseId, string name);
        Task<Exam> GetExamAsync(int id);
        Task<ExamPart> CreateExamPartAsync(int examId, ExamPartDto examPartDto);
        Task<ExamPart> GetExamPartAsync(int examId, int examPartId);
    }
}