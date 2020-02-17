using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.LoginSessionAggregate;

namespace Guts.Business.Services
{
    internal class LoginSessionService : ILoginSessionService
    {
        private readonly ILoginSesssionRepository _loginSesssionRepository;

        public LoginSessionService(ILoginSesssionRepository loginSesssionRepository)
        {
            _loginSesssionRepository = loginSesssionRepository;
        }

        public async Task<LoginSession> GetSessionAsync(string publicIdentifier, string clientIpAddress, string sessionToken = null)
        {
            return await _loginSesssionRepository.GetSingleAsync(publicIdentifier, clientIpAddress, sessionToken);
        }

        public async Task<LoginSession> CreateSessionAsync(string clientIpAddress)
        {
            var newSession = new LoginSession
            {
                PublicIdentifier = Guid.NewGuid().ToString(),
                IpAddress = clientIpAddress,
                IsCancelled = false,
                SessionToken = Guid.NewGuid().ToString(),
                LoginToken = null,
                CreateDateTime = DateTime.UtcNow
            };

            var addedSession = await _loginSesssionRepository.AddAsync(newSession);
            return addedSession;
        }

        public async Task CleanUpOldSessionsAsync()
        {
            var allSessions = await _loginSesssionRepository.GetAllAsync();
            var sessionsToDelete = allSessions.Where(s => s.CreateDateTime < DateTime.UtcNow.AddHours(-1)).ToList();
            await _loginSesssionRepository.DeleteBulkAsync(sessionsToDelete);
        }

        public async Task CancelSessionAsync(string publicIdentifier, string clientIpAddress)
        {
            var session = await _loginSesssionRepository.GetSingleAsync(publicIdentifier, clientIpAddress);
            session.IsCancelled = true;
            await _loginSesssionRepository.UpdateAsync(session);
        }

        public async Task SetLoginTokenForSessionAsync(string publicIdentifier, string loginToken)
        {
            var session = await _loginSesssionRepository.GetSingleAsync(publicIdentifier);
            session.LoginToken = loginToken;
            await _loginSesssionRepository.UpdateAsync(session);
        }
    }
}