using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Application.Repositories;

public interface IProjectAssessmentRepository : IBasicRepository<IProjectAssessment>
{
    Task<IReadOnlyList<IProjectAssessment>> FindByProjectIdAsync(int projectId);
}