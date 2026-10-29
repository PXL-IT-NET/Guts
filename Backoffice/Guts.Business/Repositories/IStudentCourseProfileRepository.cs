using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;

namespace Guts.Business.Repositories;

public interface IStudentCourseProfileRepository
{
    Task<StudentCourseProfile> GetByUserCourseAndPeriodAsync(int userId, int courseId, int periodId);
    Task<IReadOnlyList<StudentCourseProfile>> GetByCourseAndPeriodAsync(int courseId, int periodId);
    Task<IReadOnlyList<StudentCourseProfile>> GetForLeaderboardByCourseAndPeriodAsync(int courseId, int periodId);
    Task<IReadOnlyList<StudentCourseProfile>> GetForWeeklySprintLeaderboardAsync(int courseId, int periodId, System.DateTime startOfWeek);
    Task<int> CountAssignmentCompletionsAsync(int assignmentId, int courseId, int periodId);
    Task<bool> IsUserLectorAsync(int userId);
    Task<StudentCourseProfile> AddAsync(StudentCourseProfile entity);
    Task<StudentCourseProfile> UpdateAsync(StudentCourseProfile entity);
    Task UpdateRangeAsync(IEnumerable<StudentCourseProfile> entities);
}
