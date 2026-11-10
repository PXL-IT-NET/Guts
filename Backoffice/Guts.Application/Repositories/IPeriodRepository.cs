using System.Threading.Tasks;
using Guts.Domain.PeriodAggregate;

namespace Guts.Application.Repositories;

public interface IPeriodRepository : IBasicRepository<IPeriod>
{
    Task<IPeriod> GetPeriodAsync(int? periodId = null);
}