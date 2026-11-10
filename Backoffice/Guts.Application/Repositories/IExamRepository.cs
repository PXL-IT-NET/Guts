using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Application.Repositories;

public interface IExamRepository : IBasicRepository<IExam>
{
    Task<IExam> LoadDeepAsync(int examId);

    Task<IReadOnlyList<IExam>> FindWithPartsAndEvaluationsAsync(int courseId, int periodId);
}