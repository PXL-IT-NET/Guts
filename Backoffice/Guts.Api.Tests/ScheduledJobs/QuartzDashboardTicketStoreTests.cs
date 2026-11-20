using System;
using System.Security.Claims;
using Guts.Api.ScheduledJobs;
using Guts.Domain.RoleAggregate;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;

namespace Guts.Api.Tests.ScheduledJobs;

[TestFixture]
public class QuartzDashboardTicketStoreTests
{
    private MemoryCache _memoryCache;
    private MemoryQuartzDashboardTicketStore _store;

    [SetUp]
    public void SetUp()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _store = new MemoryQuartzDashboardTicketStore(_memoryCache);
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache.Dispose();
    }

    [Test]
    public void CreateAndConsumeTicket_ShouldPreserveAdminClaimsAndAllowSingleUseOnly()
    {
        var adminPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Name, "admin@pxl.be"),
            new Claim(ClaimTypes.Email, "admin@pxl.be"),
            new Claim(ClaimTypes.Role, Role.Constants.Admin),
            new Claim("custom_ignored", "value")
        ], "Bearer"));

        string ticket = _store.CreateTicket(adminPrincipal);

        Assert.That(ticket, Is.Not.Null.And.Not.Empty);

        ClaimsPrincipal consumed = _store.ConsumeTicket(ticket);
        Assert.That(consumed, Is.Not.Null);
        Assert.That(consumed.Identity?.AuthenticationType, Is.EqualTo(ApiConstants.QuartzDashboardCookieScheme));
        Assert.That(consumed.IsInRole(Role.Constants.Admin), Is.True);
        Assert.That(consumed.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo("7"));
        Assert.That(consumed.HasClaim(c => c.Type == "custom_ignored"), Is.False);

        // Second consume must fail (single-use)
        Assert.That(_store.ConsumeTicket(ticket), Is.Null);
    }

    [Test]
    public void ConsumeTicket_WhenMissingOrUnknown_ShouldReturnNull()
    {
        Assert.That(_store.ConsumeTicket(null), Is.Null);
        Assert.That(_store.ConsumeTicket("   "), Is.Null);
        Assert.That(_store.ConsumeTicket("non-existent-ticket"), Is.Null);
    }

    [Test]
    public void CreateTicket_WhenPrincipalNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _store.CreateTicket(null));
    }
}
