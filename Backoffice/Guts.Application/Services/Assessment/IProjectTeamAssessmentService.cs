using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Domain.AssessmentResultAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Application.Services.Assessment;

public interface IProjectTeamAssessmentService
{
    Task<IProjectTeamAssessment> GetOrCreateTeamAssessmentAsync(int projectAssessmentId, int projectTeamId);

    Task<ProjectTeamAssessmentStatusDto> GetStatusAsync(int projectAssessmentId, int teamId);
    Task<IReadOnlyList<IAssessmentResult>> GetResultsForTeacherAsync(int projectAssessmentId, int teamId);
    Task<IAssessmentResult> GetResultForStudent(int projectAssessmentId, int teamId, int userId);
    Task<IReadOnlyList<IPeerAssessment>> GetPeerAssessmentsOfUserAsync(int projectAssessmentId, int teamId, int userId);
    Task SavePeerAssessmentsOfUserAsync(int projectAssessmentId, int teamId, int userId, IReadOnlyList<PeerAssessmentDto> peerAssessments);
    Task SetPeerInclusionAsync(int projectAssessmentId, int teamId, int userId, bool isIncluded);
}