using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IProjectService
    {
        Task<Project> GetProjectAsync(string courseCode, string projectCode);
        Task<Project> GetOrCreateProjectAsync(string courseCode, string projectCode);
    }
}