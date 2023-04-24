using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class ProjectAssessmentDbRepository : BaseDbRepository<IProjectAssessment, ProjectAssessment>, IProjectAssessmentRepository
{
    public ProjectAssessmentDbRepository(GutsContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<IProjectAssessment>> FindByProjectIdAsync(int projectId)
    {
        return await _context.Set<ProjectAssessment>().Where(pa => pa.ProjectId == projectId).ToListAsync();
    }
}