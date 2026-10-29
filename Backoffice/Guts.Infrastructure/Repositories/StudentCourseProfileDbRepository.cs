using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.RoleAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class StudentCourseProfileDbRepository : BaseDbRepository<StudentCourseProfile, StudentCourseProfile>, IStudentCourseProfileRepository
{
    public StudentCourseProfileDbRepository(GutsContext context) : base(context) { }

    private IQueryable<int> LectorUserIdsQuery =>
        from ur in _context.UserRoles
        join r in _context.Roles on ur.RoleId equals r.Id
        where r.Name == Role.Constants.Lector
        select ur.UserId;

    public async Task<bool> IsUserLectorAsync(int userId)
    {
        return await LectorUserIdsQuery.AnyAsync(id => id == userId);
    }

    public async Task<StudentCourseProfile> GetByUserCourseAndPeriodAsync(int userId, int courseId, int periodId)
    {
        return await _context.StudentCourseProfiles
            .AsSplitQuery()
            .Include(sp => sp.Badges)
            .Include(sp => sp.Events)
            .Include(sp => sp.ClassGroup)
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.CourseId == courseId && sp.PeriodId == periodId);
    }

    public async Task<IReadOnlyList<StudentCourseProfile>> GetByCourseAndPeriodAsync(int courseId, int periodId)
    {
        var lectorUserIds = LectorUserIdsQuery;

        return await _context.StudentCourseProfiles
            .AsSplitQuery()
            .Include(sp => sp.Badges)
            .Include(sp => sp.Events)
            .Include(sp => sp.ClassGroup)
            .Include(sp => sp.User)
            .Where(sp => sp.CourseId == courseId &&
                         sp.PeriodId == periodId &&
                         !lectorUserIds.Contains(sp.UserId))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StudentCourseProfile>> GetForLeaderboardByCourseAndPeriodAsync(int courseId, int periodId)
    {
        var lectorUserIds = LectorUserIdsQuery;

        return await _context.StudentCourseProfiles
            .AsSplitQuery()
            .Include(sp => sp.ClassGroup)
            .Include(sp => sp.User)
            .Where(sp => sp.CourseId == courseId &&
                         sp.PeriodId == periodId &&
                         sp.ShowInLeaderBoard &&
                         !lectorUserIds.Contains(sp.UserId))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StudentCourseProfile>> GetForWeeklySprintLeaderboardAsync(int courseId, int periodId, System.DateTime startOfWeek)
    {
        var lectorUserIds = LectorUserIdsQuery;

        return await _context.StudentCourseProfiles
            .AsSplitQuery()
            .Include(sp => sp.ClassGroup)
            .Include(sp => sp.User)
            .Include(sp => sp.Events.Where(e => e.CreatedAt >= startOfWeek))
            .Where(sp => sp.CourseId == courseId &&
                         sp.PeriodId == periodId &&
                         sp.ShowInLeaderBoard &&
                         !lectorUserIds.Contains(sp.UserId))
            .ToListAsync();
    }

    public async Task<int> CountAssignmentCompletionsAsync(int assignmentId, int courseId, int periodId)
    {
        var lectorUserIds = LectorUserIdsQuery;

        return await _context.GamificationEvents
            .CountAsync(ge => ge.EventType == GamificationEventType.AssignmentCompleted &&
                              ge.TargetId == assignmentId &&
                              ge.StudentCourseProfile.CourseId == courseId &&
                              ge.StudentCourseProfile.PeriodId == periodId &&
                              !lectorUserIds.Contains(ge.StudentCourseProfile.UserId));
    }

    public async Task UpdateRangeAsync(IEnumerable<StudentCourseProfile> entities)
    {
        _context.StudentCourseProfiles.UpdateRange(entities);
        await _context.SaveChangesAsync();
    }
}
