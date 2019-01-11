using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IAssignmentService
    {
        Task<Assignment> GetOrCreateExerciseAsync(AssignmentDto assignmentDto);
        Task<Assignment> GetOrCreateProjectComponentAsync(AssignmentDto assignmentDto);

        Task LoadTestsForAssignmentAsync(Assignment assignment);
        Task LoadOrCreateTestsForAssignmentAsync(Assignment assignment, IEnumerable<string> testNames);

        Task<AssignmentResultDto> GetResultsForUserAsync(int assignmentId, int userId, DateTime? dateUtc);
        Task<AssignmentTestRunInfoDto> GetUserTestRunInfoForAssignment(int assignmentId, int userId, DateTime? dateUtc);

        Task<IList<AssignmentSourceDto>> GetAllSourceCodes(int assignmentId);

        Task<bool> ValidateTestCodeHashAsync(string testCodeHash, Assignment assignment, bool isLector);
    }
}