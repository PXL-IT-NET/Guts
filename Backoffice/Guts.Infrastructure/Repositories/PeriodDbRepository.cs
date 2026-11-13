using Guts.Application;
using Guts.Application.Repositories;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Guts.Infrastructure.Repositories;

internal class PeriodDbRepository : BaseDbRepository<IPeriod, Period>, IPeriodRepository
{

    public PeriodDbRepository(GutsContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<IPeriod>> GetAllAsync()
    {
        return await _context.Periods.OrderBy(p => p.From).ToListAsync();
    }

    public async Task<IPeriod> GetPeriodAsync(int? periodId = null)
    {
        Period period = null;
        if (periodId.HasValue)
        {
            period = await _context.Periods.FirstOrDefaultAsync(p => p.Id == periodId);
        }
        else
        {
            DateTime today = DateTime.Today;
            period = await _context.Periods.FirstOrDefaultAsync(p => p.From <= today && p.Until >= today);
        }

        if (period == null)
        {
            throw new DataNotFoundException();
        }
        return period;
    }

    public override Task DeleteAsync(IPeriod entityToDelete)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            int periodId = entityToDelete.Id;

            _context.StudentCourseProfiles.RemoveRange(_context.StudentCourseProfiles.Where(p => p.PeriodId == periodId));

            var classGroups = await _context.CourseClassGroups
                .Where(cg => cg.PeriodId == periodId)
                .ToListAsync();
            _context.CourseClassGroups.RemoveRange(classGroups);

            _context.Topics.RemoveRange(_context.Topics.Where(t => t.PeriodId == periodId));

            _context.Periods.Remove((Period)entityToDelete);
            await _context.SaveChangesAsync();
        });
    }
}