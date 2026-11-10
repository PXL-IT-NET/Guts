using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Repositories;
using Guts.Domain.LoginSessionAggregate;

namespace Guts.Application.Services;

internal class LoginSessionService(ILoginSesssionRepository loginSesssionRepository) : ILoginSessionService
{
    public async Task<LoginSession> GetSessionAsync(string publicIdentifier, string clientIpAddress, string sessionToken = null)
    {
        return await loginSesssionRepository.GetSingleAsync(publicIdentifier, clientIpAddress, sessionToken);
    }

    public async Task<LoginSession> CreateSessionAsync(string clientIpAddress)
    {
        var newSession = LoginSession.Create(clientIpAddress);
        return await loginSesssionRepository.AddAsync(newSession);
    }

    public async Task CleanUpOldSessionsAsync()
    {
        var allSessions = await loginSesssionRepository.GetAllAsync();
        DateTime now = DateTime.UtcNow;
        var sessionsToDelete = allSessions.Where(s => s.IsExpired(now)).ToList();
        await loginSesssionRepository.DeleteBulkAsync(sessionsToDelete);
    }

    public async Task CancelSessionAsync(string publicIdentifier, string clientIpAddress)
    {
        var session = await loginSesssionRepository.GetSingleAsync(publicIdentifier, clientIpAddress);
        session.Cancel();
        await loginSesssionRepository.UpdateAsync(session);
    }

    public async Task SetLoginTokenForSessionAsync(string publicIdentifier, string loginToken)
    {
        var session = await loginSesssionRepository.GetSingleAsync(publicIdentifier);
        session.AttachLoginToken(loginToken);
        await loginSesssionRepository.UpdateAsync(session);
    }
}