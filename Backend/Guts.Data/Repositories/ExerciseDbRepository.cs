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
    }
}