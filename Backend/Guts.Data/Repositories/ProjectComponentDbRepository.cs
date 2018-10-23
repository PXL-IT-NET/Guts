using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class ProjectComponentDbRepository : BaseDbRepository<ProjectComponent>, IProjectComponentRepository
    {
        public ProjectComponentDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<ProjectComponent> GetSingleAsync(int projectId, string componentCode)
        {
            var component = await _context.ProjectComponents.FirstOrDefaultAsync(c => c.ProjectId == projectId && c.Code == componentCode);
            if (component == null)
            {
                throw new DataNotFoundException();
            }
            return component;
        }
    }
}