using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Business.Repositories
{
    public interface IProjectRepository : IBasicRepository<IProject>
    {
        Task<IProject> GetSingleAsync(string courseCode, string projectCode, int periodId);
        Task<IProject> GetSingleAsync(int courseId, string projectCode, int periodId);
        Task<IReadOnlyList<IProject>> GetByCourseIdAsync(int courseId, int periodId);
        Task<IProject> LoadWithAssignmentsAndTeamsAsync(int courseId, string projectCode, int periodId);
        Task<IProject> LoadWithAssignmentsAndTeamsOfUserAsync(int courseId, string projectCode, int periodId, int userId);
    }
}