using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Business.Repositories
{
    public interface IProjectTeamRepository : IBasicRepository<ProjectTeam>
    {
        Task<IList<ProjectTeam>> GetByProjectWithUsersAsync(int projectId);
        Task AddUserToTeam(int teamId, int userId);
        Task<ProjectTeam> LoadByIdAsync(int teamId);
    }
}