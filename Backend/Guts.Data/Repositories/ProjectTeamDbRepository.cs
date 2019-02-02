using Guts.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guts.Data.Repositories
{
    public class ProjectTeamDbRepository : BaseDbRepository<ProjectTeam>, IProjectTeamRepository
    {
        public ProjectTeamDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IList<ProjectTeam>> GetByProjectWithUsersAsync(int projectId)
        {
            var teams = await _context.ProjectTeams
                .Where(pt => pt.ProjectId == projectId)
                .Include(pt => pt.TeamUsers)
                .ThenInclude(tu => tu.User).ToListAsync();

            return teams;
        }

        public async Task AddUserToTeam(int teamId, int userId)
        {
            _context.ProjectTeamUsers.Add(new ProjectTeamUser
            {
                ProjectTeamId = teamId, UserId = userId
            });
            await _context.SaveChangesAsync();
        }
    }
}