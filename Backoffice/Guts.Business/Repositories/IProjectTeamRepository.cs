using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Business.Repositories
{
    public interface IProjectTeamRepository : IBasicRepository<IProjectTeam>
    {
        Task<IReadOnlyList<IProjectTeam>> GetByProjectWithUsersAsync(int projectId);
        Task<IReadOnlyList<IProjectTeam>> GetByUserAsync(int userId);
        Task AddUserToTeam(int teamId, int userId);
        Task RemoveUserFromTeam(int teamId, int userId);
        Task<IProjectTeam> LoadByIdAsync(int teamId);
    }
}