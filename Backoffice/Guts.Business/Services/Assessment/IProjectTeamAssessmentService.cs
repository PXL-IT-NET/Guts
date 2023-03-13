using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Business.Services.Assessment
{
    public interface IProjectTeamAssessmentService
    {
        Task<IProjectTeamAssessment> GetOrCreateTeamAssessmentAsync(int projectAssessmentId, int projectTeamId);

        Task<ProjectTeamAssessmentStatusDto> GetStatusAsync(int projectAssessmentId, int teamId);
    }
}