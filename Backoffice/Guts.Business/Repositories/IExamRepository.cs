using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;
using Guts.Domain.PeriodAggregate;

namespace Guts.Business.Repositories
{
    public interface IExamRepository : IBasicRepository<IExam>
    {
        Task<IExam> LoadDeepAsync(int examId);

        Task<IReadOnlyList<IExam>> FindWithPartsAndEvaluationsAsync(int courseId, int periodId);
    }
}