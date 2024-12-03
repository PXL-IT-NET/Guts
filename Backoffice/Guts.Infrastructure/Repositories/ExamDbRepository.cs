using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class ExamDbRepository : BaseDbRepository<IExam, Exam>, IExamRepository
    {
        public ExamDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IExam> LoadDeepAsync(int examId)
        {
            var exam = await _context.Exams.Where(e => e.Id == examId)
                .Include($"{nameof(Exam.Parts)}.{nameof(ExamPart.AssignmentEvaluations)}.{nameof(AssignmentEvaluation.Assignment)}.{nameof(Assignment.Tests)}")
                .Include($"{nameof(Exam.Parts)}.{nameof(ExamPart.AssignmentEvaluations)}.{nameof(AssignmentEvaluation.Assignment)}.{nameof(Assignment.Topic)}")
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new DataNotFoundException();
            }

            return exam;
        }

        public async Task<IReadOnlyList<IExam>> FindWithPartsAndEvaluationsAsync(int courseId, int periodId)
        {
            List<Exam> exams = await _context.Exams.Where(e => e.CourseId == courseId && e.PeriodId == periodId)
                .Include(e => e.Parts)
                .ThenInclude(ep => ep.AssignmentEvaluations)
                .ToListAsync();

            return exams;
        }
    }
}