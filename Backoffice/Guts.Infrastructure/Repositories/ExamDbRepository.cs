using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using ExamEntity = Guts.Domain.ExamAggregate.Exam;

namespace Guts.Infrastructure.Repositories;

internal class ExamDbRepository(GutsContext context)
    : BaseDbRepository<ExamEntity, ExamEntity>(context), IExamRepository
{
    public async Task<ExamEntity> GetByIdWithQuestionsAsync(int examId)
    {
        var exam = await _context.Exams
            .Include(e => e.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(q => q.Files)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null)
        {
            throw new DataNotFoundException();
        }

        return exam;
    }

    public async Task<IReadOnlyList<ExamEntity>> GetByCourseAndPeriodAsync(int courseId, int periodId)
    {
        return await _context.Exams
            .Include(e => e.Questions)
            .Where(e => e.CourseId == courseId && e.PeriodId == periodId)
            .OrderByDescending(e => e.UpdatedAtUtc)
            .ToListAsync();
    }
}
