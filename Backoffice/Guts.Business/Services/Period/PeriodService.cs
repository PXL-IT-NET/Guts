using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.PeriodAggregate;

namespace Guts.Business.Services.Period
{
    public class PeriodService : IPeriodService
    {
        private readonly IPeriodFactory _factory;
        private readonly IPeriodRepository _repository;

        public PeriodService(IPeriodFactory factory, IPeriodRepository repository)
        {
            _factory = factory;
            _repository = repository;
        }
        public async Task<IPeriod> CreatePeriodAsync(string description, DateTime from, DateTime until)
        {
            IList<IPeriod> allPeriods = await _repository.GetAllAsync();
            IPeriod period = _factory.CreateNew(description, from, until, allPeriods);
            period = await _repository.AddAsync(period);
            return period;
        }

        public async Task UpdatePeriodAsync(int periodId, string newDescription, DateTime newFrom, DateTime newUntil)
        {
            IList<IPeriod> allPeriods = await _repository.GetAllAsync();
            IPeriod period = await _repository.GetPeriodAsync(periodId);
            period.Update(newDescription, newFrom, newUntil, allPeriods);
            await _repository.UpdateAsync(period);
        }
    }
}