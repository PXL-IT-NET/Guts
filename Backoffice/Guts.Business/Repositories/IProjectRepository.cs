using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Repositories
{
    public interface IProjectRepository : IBasicRepository<IProject>
    {
        Task<IProject> GetSingleAsync(string courseCode, Code projectCode, int periodId);
        Task<IProject> GetSingleAsync(int courseId, Code projectCode, int periodId);
        Task<IReadOnlyList<IProject>> GetByCourseIdAsync(int courseId, int periodId);
        Task<IProject> LoadWithAssignmentsAndTeamsAsync(int courseId, Code projectCode, int periodId);
        Task<IProject> LoadWithAssignmentsAndTeamsOfUserAsync(int courseId, Code projectCode, int periodId, int userId);
    }
}