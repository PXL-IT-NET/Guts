using System.Linq;
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
            var component = await _context.ProjectComponents
                .Where(c => c.ProjectId == projectId && c.Code == componentCode)
                .Include(c => c.TestCodeHashes)
                .FirstOrDefaultAsync();

            if (component == null)
            {
                throw new DataNotFoundException();
            }
            return component;
        }
    }
}