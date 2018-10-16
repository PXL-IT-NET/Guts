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

        public async Task<Chapter> LoadWithExercisesAsync(int courseId, int number, int periodId)
        {
            var query = GetChapterQuery(courseId, number, periodId);

            query = query.Include(ch => ch.Exercises);

            return await ExecuteChapterQuery(query);
        }

        public async Task<Chapter> LoadWithExercisesAndTestsAsync(int courseId, int number, int periodId)
        {
            var query = GetChapterQuery(courseId, number, periodId);

            query = query.Include(ch => ch.Exercises).ThenInclude(ex => ex.Tests);

            return await ExecuteChapterQuery(query);
        }

        public async Task<IList<Chapter>> GetByCourseIdAsync(int courseId, int periodId)
        {
            var query = _context.Chapters.Where(ch => ch.CourseId == courseId && ch.PeriodId == periodId);

            return await query.ToListAsync();
        }

        public async Task<IList<User>> GetUsersOfChapterAsync(int chapterId)
        {
            var query = from chapter in _context.Chapters
                from exercise in chapter.Exercises
                from testRun in exercise.TestRuns
                where chapter.Id == chapterId
                group testRun by testRun.User
                into userGroups
                select userGroups.Key;

            return await query.OrderBy(user => user.FirstName).ThenBy(user => user.LastName).ToListAsync();
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

        private IQueryable<Chapter> GetChapterQuery(int courseId, int number, int periodId)
        {
            var query = _context.Chapters.Where(ch =>
                ch.CourseId == courseId && ch.Number == number && ch.PeriodId == periodId);
            return query;
        }
    }
}