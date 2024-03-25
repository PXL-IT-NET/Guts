using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
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
                .AsNoTracking()
                .Include(pt => pt.TeamUsers)
                .ThenInclude(tu => tu.User).ToListAsync();

            return teams;
        }

        public async Task<IReadOnlyList<IProjectTeam>> GetByUserAsync(int userId)
        {
            var teams = await _context.ProjectTeams
                .Where(pt => pt.TeamUsers.Any(tu => tu.UserId == userId))
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

        public async Task RemoveUserFromTeam(int teamId, int userId)
        {
            ProjectTeamUser teamUser = await _context.ProjectTeamUsers.FirstOrDefaultAsync(tu => tu.ProjectTeamId == teamId && tu.UserId == userId);
            Contracts.Require(teamUser is not null, "Cannot remove user from team. The user is not a member of the team.");

            var entityEntry = _context.Entry(teamUser);
            entityEntry.State = EntityState.Deleted;

            var query = from teamAssessment in _context.ProjectTeamAssessments
                from peerAssessment in teamAssessment.PeerAssessments
                where teamAssessment.Team.Id == teamId && (peerAssessment.User.Id == userId || peerAssessment.Subject.Id == userId) 
                select peerAssessment;

            List<IPeerAssessment> peerAssessmentsToDelete = await query.ToListAsync();

            foreach (IPeerAssessment peerAssessment in peerAssessmentsToDelete)
            {
                _context.Entry(peerAssessment).State = EntityState.Deleted;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IProjectTeam> LoadByIdAsync(int teamId)
        {
            Contracts.Require(teamId > 0, "Cannot load team. Team id must be a positive number");
            var team = await _context.ProjectTeams.Where(pt => pt.Id == teamId)
                .Include(pt => pt.Project)
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