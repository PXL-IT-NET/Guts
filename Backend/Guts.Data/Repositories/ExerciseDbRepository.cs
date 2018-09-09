using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class ExerciseDbRepository : BaseDbRepository<Exercise>, IExerciseRepository
    {
        public ExerciseDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<Exercise> GetSingleAsync(int chapterId, int number)
        {
            var exercise = await _context.Exercises.FirstOrDefaultAsync(ex => ex.ChapterId == chapterId && ex.Number == number);
            if (exercise == null)
            {
                throw new DataNotFoundException();
            }
            return exercise;
        }

        public async Task<Exercise> GetSingleWithTestsAndCourseAsync(int exerciseId)
        {
            var exercise = await _context.Exercises.Where(ex => ex.Id == exerciseId)
                .Include(ex => ex.Chapter.Course)
                .Include(ex => ex.Tests)
                .FirstOrDefaultAsync();
            if (exercise == null)
            {
                throw new DataNotFoundException();
            }
            return exercise;
        }

        public async Task<IList<User>> GetExerciseUsersAsync(int exerciseId)
        {
            var query = from testRun in _context.TestResults
                where testRun.Test.ExerciseId == exerciseId
                group testRun by testRun.User
                into userGroups
                select userGroups.Key;
            return await query.OrderBy(user => user.FirstName).ThenBy(user => user.LastName).ToListAsync();
        }
    }
}