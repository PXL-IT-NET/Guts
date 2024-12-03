using System.Threading.Tasks;
using Guts.Domain.PeriodAggregate;

namespace Guts.Business.Repositories
{
    public interface IPeriodRepository
    {
        Task<Period> GetPeriodAsync(int? periodId = null);
    }
}