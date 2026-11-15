using Guts.Application;
using Guts.Application.Repositories;
using Guts.Domain.CourseAggregate;
using Guts.Domain.DeletionJobAggregate;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guts.Infrastructure.Repositories;

internal class CourseDbRepository : BaseDbRepository<Course, Course>, ICourseRepository
{
    public CourseDbRepository(GutsContext context) : base(context)
    {
    }

    public override async Task<Course> GetByIdAsync(params object[] idValues)
    {
        Course course = await base.GetByIdAsync(idValues);
        if (course.IsDeleting)
        {
            throw new DataNotFoundException();
        }
        return course;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "EF Core SQL translation does not support StringComparison overloads.")]
    public async Task<Course> GetSingleAsync(string courseCode)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => !c.IsDeleting && c.Code.ToLower() == courseCode.ToLower());
        if (course == null)
        {
            throw new DataNotFoundException();
        }
        return course;
    }

    public override async Task<IReadOnlyList<Course>> GetAllAsync()
    {
        return await _context.Set<Course>()
            .Where(c => !c.IsDeleting)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public Task QueueDeletionAsync(Course course, DeletionJob job)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            _context.Courses.Update(course);
            await _context.DeletionJobs.AddAsync(job);
            await _context.SaveChangesAsync();
        });
    }

    public override Task DeleteAsync(Course entityToDelete)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            int courseId = entityToDelete.Id;

            await _context.CourseClassGroups
                .Where(cg => cg.CourseId == courseId)
                .ExecuteDeleteAsync();

            await _context.Courses
                .Where(c => c.Id == courseId)
                .ExecuteDeleteAsync();

            _context.Entry(entityToDelete).State = EntityState.Detached;
        });
    }
}