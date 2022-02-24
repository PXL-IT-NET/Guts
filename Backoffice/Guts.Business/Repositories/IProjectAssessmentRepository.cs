using Guts.Domain.TopicAggregate;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Guts.Business.Repositories
{
    public interface IProjectAssessmentRepository : IBasicRepository<IProjectAssessment>
    {
        Task<IReadOnlyList<IProjectAssessment>> FindByProjectIdAsync(int projectId);
    }
}