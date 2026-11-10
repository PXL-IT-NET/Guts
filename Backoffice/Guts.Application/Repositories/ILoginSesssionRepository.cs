using System.Threading.Tasks;
using Guts.Domain.LoginSessionAggregate;

namespace Guts.Application.Repositories;

public interface ILoginSesssionRepository : IBasicRepository<LoginSession>
{
    Task<LoginSession> GetSingleAsync(string publicIdentifier, string ipAddress = null, string sessionToken = null);
}