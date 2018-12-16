using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IAssignmentService
    {
        Task<Exercise> GetOrCreateExerciseAsync(ExerciseDto exerciseDto);
        Task<ProjectComponent> GetOrCreateProjectComponentAsync(ProjectComponentDto componentDto);

        Task LoadTestsForAssignmentAsync(Assignment assignment);
        Task LoadOrCreateTestsForAssignmentAsync(Assignment assignment, IEnumerable<string> testNames);

        Task<AssignmentResultDto> GetResultsForUserAsync(int exerciseId, int userId, DateTime? dateUtc);
        Task<ExerciseTestRunInfoDto> GetUserTestRunInfoForExercise(int exerciseId, int userId, DateTime? dateUtc);

        Task<IList<ExerciseSourceDto>> GetAllSourceCodes(int exerciseId);

        Task<bool> ValidateTestCodeHashAsync(string testCodeHash, Assignment assignment, bool isLector);
    }
}