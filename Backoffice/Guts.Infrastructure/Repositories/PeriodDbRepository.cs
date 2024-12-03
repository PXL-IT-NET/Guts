using System;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.PeriodAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class PeriodDbRepository : BaseDbRepository<IPeriod, Period>, IPeriodRepository
    {

        public PeriodDbRepository(GutsContext context) : base(context)
        {
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
    }
}