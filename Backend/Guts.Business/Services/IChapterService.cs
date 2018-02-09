using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IChapterService
    {
        Task<Chapter> GetOrCreateChapterAsync(string courseCode, int chapterNumber);
        Task<Chapter> LoadChapterWithTestsAsync(int courseId, int chapterNumber);
        Task<IList<ExerciseResultDto>> GetResultsForUserAsync(int chapterId, int userId);
    }
}