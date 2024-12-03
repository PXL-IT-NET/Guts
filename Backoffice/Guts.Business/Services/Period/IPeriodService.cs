using System;
using System.Threading.Tasks;
using Guts.Domain.PeriodAggregate;

namespace Guts.Business.Services.Period
{
    public interface IPeriodService
    {
        Task<IPeriod> CreatePeriodAsync(string description, DateTime from, DateTime until);
        Task UpdatePeriodAsync(int periodId, string newDescription, DateTime newFrom, DateTime newUntil);
    }
}