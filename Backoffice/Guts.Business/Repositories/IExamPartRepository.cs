using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Repositories
{
    public interface IExamPartRepository : IBasicRepository<ExamPart>
    {
        Task<ExamPart> LoadWithAssignmentEvaluationsAsync(int examPartId);
    }
}