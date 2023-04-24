using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Repositories
{
    public interface IExamPartRepository : IBasicRepository<IExamPart>
    {
        Task<IExamPart> LoadWithAssignmentEvaluationsAsync(int examPartId);
    }
}