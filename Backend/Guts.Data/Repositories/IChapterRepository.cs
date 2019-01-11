using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IChapterRepository : IBasicRepository<Chapter>
    {
        Task<Chapter> GetSingleAsync(string courseCode, string code, int periodId);
        Task<Chapter> LoadWithAssignmentsAsync(int courseId, string code, int periodId);
        Task<Chapter> LoadWithAssignmentsAndTestsAsync(int courseId, string code, int periodId);
        Task<IList<Chapter>> GetByCourseIdAsync(int courseId, int periodId);
    }
}