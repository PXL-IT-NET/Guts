using System.Threading.Tasks;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Application.Repositories;

public interface IProjectTeamAssessmentRepository : IBasicRepository<IProjectTeamAssessment>
{
    Task<IProjectTeamAssessment> LoadAsync(int projectAssessmentId, int teamId);
}