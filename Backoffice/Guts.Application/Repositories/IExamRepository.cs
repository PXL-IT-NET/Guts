using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.ExamAggregate;

namespace Guts.Application.Repositories;

public interface IExamRepository : IBasicRepository<Exam>
{
    Task<Exam> GetByIdWithQuestionsAsync(int examId);
    Task<IReadOnlyList<Exam>> GetByCourseAndPeriodAsync(int courseId, int periodId);
}
