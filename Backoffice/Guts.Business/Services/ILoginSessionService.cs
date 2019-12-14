using System.Threading.Tasks;
using Guts.Domain.LoginSessionAggregate;

namespace Guts.Business.Services
{
    public interface ILoginSessionService
    {
        Task<LoginSession> GetSessionAsync(string publicIdentifier, string clientIpAddress, string sessionToken = null);
        Task<LoginSession> CreateSessionAsync(string clientIpAddress);
        Task CleanUpOldSessionsAsync();
        Task CancelSessionAsync(string publicIdentifier, string clientIpAddress);
        Task SetLoginTokenForSessionAsync(string publicIdentifier, string loginToken);
    }
}