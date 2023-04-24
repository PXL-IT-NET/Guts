using System.Threading.Tasks;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Business.Repositories
{
    public interface IProjectTeamAssessmentRepository : IBasicRepository<IProjectTeamAssessment>
    {
        Task<IProjectTeamAssessment> LoadAsync(int projectAssessmentId, int teamId);
    }
}