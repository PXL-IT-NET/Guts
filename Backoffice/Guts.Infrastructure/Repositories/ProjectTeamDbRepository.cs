using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.ProjectTeamAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class ProjectTeamDbRepository : BaseDbRepository<IProjectTeam, ProjectTeam>, IProjectTeamRepository
    {
        public ProjectTeamDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<IProjectTeam>> GetByProjectWithUsersAsync(int projectId)
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

        public async Task<IProjectTeam> LoadByIdAsync(int teamId)
        {
            var team = await _context.ProjectTeams.Where(pt => pt.Id == teamId)
                .Include(pt => pt.TeamUsers)
                .ThenInclude(tu => tu.User)
                .FirstOrDefaultAsync();
            if (team == null)
            {
                throw new DataNotFoundException();
            }
            return team;
        }
    }
}