using System;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Domain.PeriodAggregate;

namespace Guts.Application.Services.Period;

public interface IPeriodService
{
    Task<IPeriod> CreatePeriodAsync(string description, DateTime from, DateTime until);
    Task UpdatePeriodAsync(int periodId, string newDescription, DateTime newFrom, DateTime newUntil);
    Task<DeletionJobDto> DeletePeriodAsync(int periodId);
}