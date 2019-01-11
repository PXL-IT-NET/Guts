using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IProjectRepository : IBasicRepository<Project>
    {
        Task<Project> GetSingleAsync(string courseCode, string projectCode, int periodId);
        Task<IList<Project>> GetByCourseIdAsync(int courseId, int periodId);
    }
}