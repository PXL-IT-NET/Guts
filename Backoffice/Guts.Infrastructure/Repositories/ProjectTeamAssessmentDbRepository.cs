using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class ProjectTeamAssessmentDbRepository : BaseDbRepository<IProjectTeamAssessment, ProjectTeamAssessment>, IProjectTeamAssessmentRepository
{
    public ProjectTeamAssessmentDbRepository(GutsContext context) : base(context)
    {
    }

    public async Task<IProjectTeamAssessment> LoadAsync(int projectAssessmentId, int teamId)
    {
        IProjectTeamAssessment assessement = await _context.Set<ProjectTeamAssessment>()
            .Where(pta => pta.ProjectAssessment.Id == projectAssessmentId && pta.Team.Id == teamId)
            .AsSplitQuery()
            .Include(pta => pta.Team.TeamUsers)
            .ThenInclude(tu => tu.User)
            .Include(pta => pta.ProjectAssessment)
            .Include(pta => pta.Peers)
            .Include(pta => pta.PeerAssessments)
            .ThenInclude(pa => pa.User)
            .Include(pta => pta.PeerAssessments)
            .ThenInclude(pa => pa.Subject)
            .FirstOrDefaultAsync();

        if (assessement == null)
        {
            throw new DataNotFoundException();
        }
        return assessement;
    }

    public override async Task<IProjectTeamAssessment> UpdateAsync(IProjectTeamAssessment existingEntity)
    {
        if (existingEntity.Id <= 0)
        {
            throw new ArgumentException("Cannot update a non-existing entity (Id <= 0).");
        }

        var currentPeerAssessmentIds = existingEntity.PeerAssessments
            .OfType<PeerAssessment>()
            .Where(pa => pa.Id > 0)
            .Select(pa => pa.Id)
            .ToHashSet();

        var orphanedPeerAssessments = await _context.PeerAssessments
            .Where(pa => pa.ProjectTeamAssessmentId == existingEntity.Id && !currentPeerAssessmentIds.Contains(pa.Id))
            .ToListAsync();

        if (orphanedPeerAssessments.Count > 0)
        {
            _context.PeerAssessments.RemoveRange(orphanedPeerAssessments);
        }

        var entry = _context.Set<ProjectTeamAssessment>().Update((ProjectTeamAssessment)existingEntity);
        await _context.SaveChangesAsync();

        return entry.Entity;
    }
}