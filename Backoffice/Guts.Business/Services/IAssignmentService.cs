using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Dtos;
using Guts.Domain.AssignmentAggregate;

namespace Guts.Business.Services
{
    public interface IAssignmentService
    {
        Task<Assignment> GetAssignmentOfCurrentPeriodAsync(AssignmentDto assignmentDto);

        Task<Assignment> GetOrCreateAssignmentAsync(int topicId, string assignmentCode);

        Task LoadTestsForAssignmentAsync(Assignment assignment);
        Task LoadOrCreateTestsForAssignmentAsync(Assignment assignment, IReadOnlyList<string> testNames);

        Task<AssignmentResultDto> GetResultsForUserAsync(int assignmentId, int userId, DateTime? dateUtc);
        Task<AssignmentTestRunInfoDto> GetUserTestRunInfoForAssignment(int assignmentId, int userId, DateTime? dateUtc);

        Task<AssignmentResultDto> GetResultsForTeamAsync(int projectId, int assignmentId, int teamId, DateTime? dateUtc);
        Task<AssignmentTestRunInfoDto> GetTeamTestRunInfoForAssignment(int assignmentId, int teamId, DateTime? dateUtc);

        Task<IReadOnlyList<SolutionDto>> GetAllSolutions(int assignmentId);

        Task<bool> ValidateTestCodeHashAsync(string testCodeHash, Assignment assignment, bool isLector);

        Task<AssignmentStatisticsDto> GetAssignmentUserStatisticsAsync(int assignmentId, DateTime? dateUtc);
        Task<AssignmentStatisticsDto> GetAssignmentTeamStatisticsAsync(int projectId, int assignmentId, DateTime? dateUtc);
    }
}