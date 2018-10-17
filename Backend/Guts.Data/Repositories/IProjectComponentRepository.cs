using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IProjectComponentRepository : IBasicRepository<ProjectComponent>
    {
        Task<ProjectComponent> GetSingleAsync(int projectId, string componentCode);
    }
}