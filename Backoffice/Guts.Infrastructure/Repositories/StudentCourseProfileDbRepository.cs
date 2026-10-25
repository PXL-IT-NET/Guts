using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.GamificationAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class StudentCourseProfileDbRepository : BaseDbRepository<StudentCourseProfile, StudentCourseProfile>, IStudentCourseProfileRepository
    {
        public StudentCourseProfileDbRepository(GutsContext context) : base(context) { }

        public async Task<StudentCourseProfile> GetByUserCourseAndPeriodAsync(int userId, int courseId, int periodId)
        {
            return await _context.StudentCourseProfiles
                .Include(sp => sp.Badges)
                .Include(sp => sp.Events)
                .Include(sp => sp.ClassGroup)
                .Include(sp => sp.User)
                .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.CourseId == courseId && sp.PeriodId == periodId);
        }

        public async Task<IReadOnlyList<StudentCourseProfile>> GetForLeaderboardByCourseAndPeriodAsync(int courseId, int periodId)
        {
            return await _context.StudentCourseProfiles
                .Include(sp => sp.Badges)
                .Include(sp => sp.Events)
                .Include(sp => sp.ClassGroup)
                .Include(sp => sp.User)
                .Where(sp => sp.CourseId == courseId && sp.PeriodId == periodId && sp.ShowInLeaderBoard)
                .ToListAsync();
        }

        public async Task<int> CountAssignmentCompletionsAsync(int assignmentId, int courseId, int periodId)
        {
            return await _context.GamificationEvents
                .CountAsync(ge => ge.EventType == GamificationEventType.AssignmentCompleted &&
                                  ge.TargetId == assignmentId &&
                                  ge.StudentCourseProfile.CourseId == courseId &&
                                  ge.StudentCourseProfile.PeriodId == periodId);
        }
    }
}
