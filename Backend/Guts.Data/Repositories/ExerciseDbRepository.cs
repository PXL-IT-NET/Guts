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

        public async Task<Exercise> GetSingleAsync(int chapterId, string code)
        {
            var exercise = await _context.Exercises
                .Where(ex => ex.ChapterId == chapterId && ex.Code == code)
                .Include(ex => ex.TestCodeHashes)
                .FirstOrDefaultAsync();

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
    }
}