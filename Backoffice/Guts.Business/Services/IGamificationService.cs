using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Dtos.Gamification;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Services
{
    public interface IGamificationService
    {
        Task ProcessTestRunAsync(TestRun testRun);
        Task<StudentGamificationProfileDto> GetStudentCourseProfileAsync(int userId, int courseId, int? periodId = null);
        Task<StudentGamificationProfileDto> EnrollStudentAsync(int userId, int courseId, EnrollStudentDto enrollDto, int? periodId = null);
        Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklySprintLeaderboardAsync(int courseId, int currentUserId, int? periodId = null);
        Task<IReadOnlyList<LeaderboardEntryDto>> GetHallOfFameLeaderboardAsync(int courseId, int currentUserId, int? periodId = null);
        Task<IReadOnlyList<ClassGroupLeaderboardEntryDto>> GetClassGroupLeaderboardAsync(int courseId, int? periodId = null);
        Task<CourseGamificationConfigDto> GetCourseConfigAsync(int courseId);
        Task<CourseGamificationConfigDto> UpdateCourseConfigAsync(int courseId, CourseGamificationConfigDto configDto);
        Task<IReadOnlyList<CourseClassGroupDto>> GetCourseClassGroupsAsync(int courseId, int? periodId = null);
        Task<CourseClassGroupDto> AddCourseClassGroupAsync(int courseId, string name, int? periodId = null);
        Task DeleteCourseClassGroupAsync(int courseId, int classGroupId);
    }
}
