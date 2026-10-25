using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.GamificationAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class CourseClassGroupDbRepository : BaseDbRepository<CourseClassGroup, CourseClassGroup>, ICourseClassGroupRepository
    {
        public CourseClassGroupDbRepository(GutsContext context) : base(context) { }

        public async Task<IReadOnlyList<CourseClassGroup>> GetByCourseAndPeriodAsync(int courseId, int periodId)
        {
            return await _context.CourseClassGroups
                .Where(ccg => ccg.CourseId == courseId && ccg.PeriodId == periodId)
                .OrderBy(ccg => ccg.Name)
                .ToListAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "EF Core SQL translation does not support StringComparison overloads.")]
        public async Task<CourseClassGroup> GetByNameAsync(int courseId, int periodId, string name)
        {
            var trimmedLower = name.Trim().ToLower();
            return await _context.CourseClassGroups
                .FirstOrDefaultAsync(ccg => ccg.CourseId == courseId && ccg.PeriodId == periodId && ccg.Name.ToLower() == trimmedLower);
        }
    }
}

