using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IProjectRepository : IBasicRepository<Project>
    {
        Task<Project> GetSingleAsync(string courseCode, string projectCode, int periodId);
        Task<Project> GetSingleAsync(int courseId, string projectCode, int periodId);
        Task<IList<Project>> GetByCourseIdAsync(int courseId, int periodId);
        Task<Project> LoadWithAssignmentsAndTeamsAsync(int courseId, string projectCode, int periodId);
        Task<Project> LoadWithAssignmentsAndTeamsOfUserAsync(int courseId, string projectCode, int periodId, int userId);
    }
}