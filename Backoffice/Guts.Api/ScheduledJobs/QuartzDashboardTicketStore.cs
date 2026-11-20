using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;

namespace Guts.Api.ScheduledJobs;

public interface IQuartzDashboardTicketStore
{
    string CreateTicket(ClaimsPrincipal principal);
    ClaimsPrincipal ConsumeTicket(string ticket);
}

public class MemoryQuartzDashboardTicketStore(IMemoryCache memoryCache) : IQuartzDashboardTicketStore
{
    private const string CacheKeyPrefix = "QuartzDashboardTicket:";
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromSeconds(60);
    private readonly Lock _lock = new();

    public string CreateTicket(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
        string ticket = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var claims = principal.Claims
            .Where(c => c.Type is ClaimTypes.NameIdentifier or ClaimTypes.Name or ClaimTypes.Email or ClaimTypes.Role)
            .Select(c => new Claim(c.Type, c.Value))
            .ToList();

        memoryCache.Set(CacheKeyPrefix + ticket, claims, TicketLifetime);
        return ticket;
    }

    public ClaimsPrincipal ConsumeTicket(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return null;
        }

        string cacheKey = CacheKeyPrefix + ticket;
        List<Claim> claims;

        lock (_lock)
        {
            if (!memoryCache.TryGetValue(cacheKey, out claims))
            {
                return null;
            }

            memoryCache.Remove(cacheKey);
        }

        var identity = new ClaimsIdentity(claims, ApiConstants.QuartzDashboardCookieScheme);
        return new ClaimsPrincipal(identity);
    }
}
