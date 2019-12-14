using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class ChapterDbRepository : BaseDbRepository<Chapter>, IChapterRepository
    {
        public ChapterDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<Chapter> GetSingleAsync(string courseCode, string code, int periodId)
        {
            var chapter = await _context.Chapters.FirstOrDefaultAsync(ch => ch.Course.Code == courseCode && ch.Code == code && ch.PeriodId == periodId);
            if (chapter == null)
            {
                throw new DataNotFoundException();
            }
            return chapter;
        }

        public async Task<Chapter> LoadWithAssignmentsAsync(int courseId, string code, int periodId)
        {
            var query = GetChapterQuery(courseId, code, periodId);

            query = query.Include(ch => ch.Assignments);

            return await ExecuteChapterQuery(query);
        }

        public async Task<Chapter> LoadWithAssignmentsAndTestsAsync(int courseId, string code, int periodId)
        {
            var query = GetChapterQuery(courseId, code, periodId);

            query = query.Include(ch => ch.Assignments).ThenInclude(ex => ex.Tests);

            return await ExecuteChapterQuery(query);
        }

        public async Task<IList<Chapter>> GetByCourseIdAsync(int courseId, int periodId)
        {
            var query = _context.Chapters.Where(ch => ch.CourseId == courseId && ch.PeriodId == periodId);

            return await query.ToListAsync();
        }

        private async Task<Chapter> ExecuteChapterQuery(IQueryable<Chapter> query)
        {
            var chapter = await query.FirstOrDefaultAsync();
            if (chapter == null)
            {
                throw new DataNotFoundException();
            }

            return chapter;
        }

        private IQueryable<Chapter> GetChapterQuery(int courseId, string code, int periodId)
        {
            var query = _context.Chapters.Where(ch =>
                ch.CourseId == courseId && ch.Code == code && ch.PeriodId == periodId);
            return query;
        }
    }
}