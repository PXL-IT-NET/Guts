using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IExerciseRepository : IBasicRepository<Exercise>
    {
        Task<Exercise> GetSingleAsync(int chapterId, int number);
        Task<Exercise> GetSingleWithChapterAndCourseAsync(int exerciseId);
    }
}