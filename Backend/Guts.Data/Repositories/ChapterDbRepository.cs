using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class ChapterDbRepository : BaseDbRepository<Chapter>, IChapterRepository
    {
        public ChapterDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<Chapter> GetSingleAsync(string courseCode, int number, int periodId)
        {
            var chapter = await _context.Chapters.FirstOrDefaultAsync(ch => ch.Course.Code == courseCode && ch.Number == number && ch.PeriodId == periodId);
            if (chapter == null)
            {
                throw new DataNotFoundException();
            }
            return chapter;
        }

        public async Task<Chapter> LoadWithExercisesAndTestsAsync(int courseId, int number, int periodId)
        {
            var query = _context.Chapters.Where(ch =>
                ch.CourseId == courseId && ch.Number == number && ch.PeriodId == periodId);

            query = query.Include(ch => ch.Exercises).ThenInclude(ex => ex.Tests);

            var chapter = await query.AsNoTracking().FirstOrDefaultAsync();
            if (chapter == null)
            {
                throw new DataNotFoundException();
            }
            return chapter;
        }

        public async Task<IList<Chapter>> GetByCourseIdAsync(int courseId, int periodId)
        {
            var query = _context.Chapters.Where(ch => ch.CourseId == courseId && ch.PeriodId == periodId);

            return await query.ToListAsync();
        }
    }
}