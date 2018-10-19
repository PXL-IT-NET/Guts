using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class ProjectDbRepository : BaseDbRepository<Project>, IProjectRepository
    {
        public ProjectDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<Project> GetSingleAsync(string courseCode, string projectCode, int periodId)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Course.Code == courseCode && p.Code == projectCode && p.PeriodId == periodId);
            if (project == null)
            {
                throw new DataNotFoundException();
            }
            return project;
        }
    }
}