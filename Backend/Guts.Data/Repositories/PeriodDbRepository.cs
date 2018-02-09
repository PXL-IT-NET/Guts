using System;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class PeriodDbRepository : IPeriodRepository
    {
        private readonly GutsContext _context;

        public PeriodDbRepository(GutsContext context)
        {
            _context = context;
        }

        public async Task<Period> GetCurrentPeriodAsync()
        {
            var today = DateTime.Today;
            var period = await _context.Periods.FirstOrDefaultAsync(p => p.From <= today && p.Until >= today);
            if (period == null)
            {
                throw new DataNotFoundException();
            }
            return period;
        }
    }
}