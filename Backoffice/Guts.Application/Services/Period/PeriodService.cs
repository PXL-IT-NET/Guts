using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Services.Deletion;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.PeriodAggregate;

namespace Guts.Application.Services.Period;

public class PeriodService(
    IPeriodFactory factory,
    IPeriodRepository repository,
    IDeletionJobService deletionJobService) : IPeriodService
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

    public async Task<DeletionJobDto> DeletePeriodAsync(int periodId)
    {
        IPeriod period = await repository.GetPeriodAsync(periodId);
        period.MarkAsDeleting();
        return await deletionJobService.QueueDeletionJobAsync(
            DeletionTargetType.Period,
            period.Id,
            period.Description,
            job => repository.QueueDeletionAsync(period, job));
    }
}