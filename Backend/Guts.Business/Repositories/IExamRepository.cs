using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Repositories
{
    public interface IExamRepository : IBasicRepository<Exam>
    {
        Task<Exam> LoadWithPartsAndEvaluations(int examId);
    }
}