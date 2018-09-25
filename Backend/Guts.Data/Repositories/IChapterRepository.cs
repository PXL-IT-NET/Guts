using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IChapterRepository : IBasicRepository<Chapter>
    {
        Task<Chapter> GetSingleAsync(string courseCode, int number, int periodId);
        Task<Chapter> LoadWithExercisesAsync(int courseId, int number, int periodId);
        Task<Chapter> LoadWithExercisesAndTestsAsync(int courseId, int number, int periodId);
        Task<IList<Chapter>> GetByCourseIdAsync(int courseId, int periodId);
        Task<IList<User>> GetUsersOfChapterAsync(int chapterId);
    }
}