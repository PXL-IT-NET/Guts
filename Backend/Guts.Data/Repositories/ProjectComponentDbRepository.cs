using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public class ProjectComponentDbRepository : BaseDbRepository<ProjectComponent>, IProjectComponentRepository
    {
        public ProjectComponentDbRepository(GutsContext context) : base(context)
        {
        }

        public Task<ProjectComponent> GetSingleAsync(int projectId, string componentCode)
        {
            throw new System.NotImplementedException();
        }
    }
}