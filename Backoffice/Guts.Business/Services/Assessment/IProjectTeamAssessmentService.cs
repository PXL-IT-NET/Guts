using Guts.Domain.ProjectTeamAssessmentAggregate;
using System.Threading.Tasks;

namespace Guts.Business.Services.Assessment
{
    public interface IProjectTeamAssessmentService
    {
        Task<IProjectTeamAssessment> GetOrCreateTeamAssessmentAsync(int projectAssessmentId, int projectTeamId);
    }
}
