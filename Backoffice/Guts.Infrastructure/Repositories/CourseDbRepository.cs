using Guts.Application;
using Guts.Application.Repositories;
using Guts.Domain.CourseAggregate;
using Microsoft.EntityFrameworkCore;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guts.Infrastructure.Repositories;

internal class CourseDbRepository : BaseDbRepository<Course, Course>, ICourseRepository
{
    public CourseDbRepository(GutsContext context) : base(context)
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "EF Core SQL translation does not support StringComparison overloads.")]
    public async Task<Course> GetSingleAsync(string courseCode)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code.ToLower() == courseCode.ToLower());
        if (course == null)
        {
            throw new DataNotFoundException();
        }
        return course;
    }

    public override async Task<IReadOnlyList<Course>> GetAllAsync()
    {
        return await _context.Set<Course>().OrderBy(c => c.Name).ToListAsync();
    }

    public override Task DeleteAsync(Course entityToDelete)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            int courseId = entityToDelete.Id;

            _context.StudentCourseProfiles.RemoveRange(_context.StudentCourseProfiles.Where(p => p.CourseId == courseId));

            var classGroups = await _context.CourseClassGroups
                .Where(cg => cg.CourseId == courseId)
                .ToListAsync();
            _context.CourseClassGroups.RemoveRange(classGroups);

            var gamificationConfigs = await _context.CourseGamificationConfigs
                .Where(gc => gc.CourseId == courseId)
                .ToListAsync();
            _context.CourseGamificationConfigs.RemoveRange(gamificationConfigs);

            _context.Topics.RemoveRange(_context.Topics.Where(t => t.CourseId == courseId));

            _context.Courses.Remove(entityToDelete);
            await _context.SaveChangesAsync();
        });
    }
}