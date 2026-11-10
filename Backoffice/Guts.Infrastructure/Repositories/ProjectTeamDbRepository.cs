using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Application.Repositories;
using Guts.Common;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

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
            ProjectTeamId = teamId,
            UserId = userId
        });
        await _context.SaveChangesAsync();

        List<ProjectTeamAssessment> teamAssessments = await _context.ProjectTeamAssessments
            .Where(pta => pta.Team.Id == teamId)
            .AsSplitQuery()
            .Include(pta => pta.Team.TeamUsers)
            .ThenInclude(tu => tu.User)
            .Include(pta => pta.ProjectAssessment)
            .Include(pta => pta.Peers)
            .Include(pta => pta.PeerAssessments)
            .ToListAsync();

        DateTime utcNow = DateTime.UtcNow;
        foreach (ProjectTeamAssessment teamAssessment in teamAssessments)
        {
            if (!teamAssessment.IsComplete && utcNow <= teamAssessment.ProjectAssessment.DeadlineUtc)
            {
                teamAssessment.AddPeer(userId);
            }
        }

        if (teamAssessments.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveUserFromTeam(int teamId, int userId)
    {
        ProjectTeamUser teamUser = await _context.ProjectTeamUsers.FirstOrDefaultAsync(tu => tu.ProjectTeamId == teamId && tu.UserId == userId);
        Contracts.Require(teamUser is not null, "Cannot remove user from team. The user is not a member of the team.");

        List<ProjectTeamAssessment> teamAssessments = await _context.ProjectTeamAssessments
            .Where(pta => pta.Team.Id == teamId)
            .AsSplitQuery()
            .Include(pta => pta.Team.TeamUsers)
            .ThenInclude(tu => tu.User)
            .Include(pta => pta.ProjectAssessment)
            .Include(pta => pta.Peers)
            .Include(pta => pta.PeerAssessments)
            .ThenInclude(pa => pa.User)
            .Include(pta => pta.PeerAssessments)
            .ThenInclude(pa => pa.Subject)
            .ToListAsync();

        foreach (ProjectTeamAssessment teamAssessment in teamAssessments.Where(ta => !ta.IsComplete))
        {
            IReadOnlyList<IPeerAssessment> peerAssessmentsToDelete = teamAssessment.RemovePeer(userId);
            foreach (IPeerAssessment peerAssessment in peerAssessmentsToDelete)
            {
                _context.Entry(peerAssessment).State = EntityState.Deleted;
            }
        }

        _context.Entry(teamUser).State = EntityState.Deleted;

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