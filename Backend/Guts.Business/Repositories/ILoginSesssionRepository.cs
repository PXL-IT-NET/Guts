using System.Threading.Tasks;
using Guts.Domain.LoginSessionAggregate;

namespace Guts.Business.Repositories
{
    public interface ILoginSesssionRepository : IBasicRepository<LoginSession>
    {
        Task<LoginSession> GetSingleAsync(string publicIdentifier, string ipAddress = null, string sessionToken = null);
    }
}