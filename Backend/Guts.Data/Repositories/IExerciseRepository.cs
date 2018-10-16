using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IExerciseRepository : IBasicRepository<Exercise>
    {
        Task<Exercise> GetSingleAsync(int chapterId, string code);
        Task<Exercise> GetSingleWithTestsAndCourseAsync(int exerciseId);
    }
}