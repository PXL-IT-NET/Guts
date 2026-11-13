using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Repositories;
using Guts.Domain.PeriodAggregate;

namespace Guts.Application.Services.Period;

public class PeriodService(IPeriodFactory factory, IPeriodRepository repository) : IPeriodService
{
    public async Task<IPeriod> CreatePeriodAsync(string description, DateTime from, DateTime until)
    {
        IReadOnlyList<IPeriod> allPeriods = await repository.GetAllAsync();
        IPeriod period = factory.CreateNew(description, from, until, allPeriods);
        period = await repository.AddAsync(period);
        return period;
    }

    public async Task UpdatePeriodAsync(int periodId, string newDescription, DateTime newFrom, DateTime newUntil)
    {
        IReadOnlyList<IPeriod> allPeriods = await repository.GetAllAsync();
        IPeriod period = await repository.GetPeriodAsync(periodId);
        period.Update(newDescription, newFrom, newUntil, allPeriods);
        await repository.UpdateAsync(period);
    }

    public async Task DeletePeriodAsync(int periodId)
    {
        IPeriod period = await repository.GetPeriodAsync(periodId);
        period.EnsureCanBeDeleted();
        await repository.DeleteAsync(period);
    }
}