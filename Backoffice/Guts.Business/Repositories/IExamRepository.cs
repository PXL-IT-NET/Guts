using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Business.Repositories
{
    public interface IExamRepository : IBasicRepository<Exam>
    {
        Task<Exam> LoadDeep(int examId);

        Task<IReadOnlyList<Exam>> FindWithPartsAndEvaluationsAsync(int periodId, int? courseId);
    }
}