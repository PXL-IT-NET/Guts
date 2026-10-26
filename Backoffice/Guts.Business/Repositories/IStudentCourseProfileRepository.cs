using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;

namespace Guts.Business.Repositories;

public interface IStudentCourseProfileRepository
{
    Task<StudentCourseProfile> GetByUserCourseAndPeriodAsync(int userId, int courseId, int periodId);
    Task<IReadOnlyList<StudentCourseProfile>> GetForLeaderboardByCourseAndPeriodAsync(int courseId, int periodId);
    Task<int> CountAssignmentCompletionsAsync(int assignmentId, int courseId, int periodId);
    Task<StudentCourseProfile> AddAsync(StudentCourseProfile entity);
    Task<StudentCourseProfile> UpdateAsync(StudentCourseProfile entity);
}