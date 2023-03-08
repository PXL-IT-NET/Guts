using System.Threading.Tasks;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Business.Services.Assessment
{
    public interface IProjectTeamAssessmentService
    {
        Task<IProjectTeamAssessment> GetOrCreateTeamAssessmentAsync(int projectAssessmentId, int projectTeamId);
    }
}