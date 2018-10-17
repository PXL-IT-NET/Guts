using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IProjectService
    {
        Task<Project> GetOrCreateProjectAsync(string courseCode, string projectCode);
    }
}