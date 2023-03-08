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
            .Include(pta => pta.Team.TeamUsers)
            .ThenInclude(tu => tu.User)
            .Include(pta => pta.ProjectAssessment)
            .Include(pta => pta.PeerAssessments)
            .ThenInclude(pa => pa.User)
            .FirstOrDefaultAsync();

        if (assessement == null)
        {
            throw new DataNotFoundException();
        }
        return assessement;
    }
}