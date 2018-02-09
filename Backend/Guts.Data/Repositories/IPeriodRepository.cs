using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IPeriodRepository
    {
        Task<Period> GetCurrentPeriodAsync();
    }
}