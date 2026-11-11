using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Common;

namespace Guts.Domain.LoginSessionAggregate;

public class LoginSession : AggregateRoot
{
    public int Id { get; set; }

    public static readonly TimeSpan DefaultExpirationTimeout = TimeSpan.FromHours(1);

    [Required]
    public string PublicIdentifier { get; internal set; }

    [Required]
    public string SessionToken { get; internal set; }

    [Required]
    public string IpAddress { get; internal set; }

    public string LoginToken { get; internal set; }

    public bool IsCancelled { get; internal set; }

    public DateTime CreateDateTime { get; internal set; }

    public LoginSession()
    {
    }

    public static LoginSession Create(string ipAddress, DateTime? createdAt = null)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(ipAddress), "IP address cannot be empty.");

        return new LoginSession
        {
            PublicIdentifier = Guid.NewGuid().ToString(),
            SessionToken = Guid.NewGuid().ToString(),
            IpAddress = ipAddress.Trim(),
            IsCancelled = false,
            LoginToken = null,
            CreateDateTime = createdAt ?? DateTime.UtcNow
        };
    }

    public void Cancel()
    {
        Contracts.Require(!IsCancelled, "Session is already cancelled.");
        IsCancelled = true;
    }

    public void AttachLoginToken(string loginToken, DateTime? now = null)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(loginToken), "Login token cannot be empty.");
        Contracts.Require(!IsCancelled, "Cannot attach a login token to a cancelled session.");
        Contracts.Require(!IsExpired(now ?? DateTime.UtcNow), "Cannot attach a login token to an expired session.");

        LoginToken = loginToken.Trim();
    }

    public bool IsExpired(DateTime now, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? DefaultExpirationTimeout;
        return now >= CreateDateTime.Add(effectiveTimeout);
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}