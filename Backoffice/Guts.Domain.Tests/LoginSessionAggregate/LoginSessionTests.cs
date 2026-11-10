using System;
using Guts.Common;
using Guts.Domain.LoginSessionAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.LoginSessionAggregate;

[TestFixture]
public class LoginSessionTests
{
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_NullOrWhitespaceIp_ThrowsContractException(string invalidIp)
    {
        Assert.Throws<ContractException>(() => LoginSession.Create(invalidIp));
    }

    [Test]
    public void Create_ValidIp_InitializesSessionWithGuidsAndActiveState()
    {
        var now = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var session = LoginSession.Create("192.168.1.1", now);

        Assert.That(session.IpAddress, Is.EqualTo("192.168.1.1"));
        Assert.That(session.CreateDateTime, Is.EqualTo(now));
        Assert.That(session.IsCancelled, Is.False);
        Assert.That(session.LoginToken, Is.Null);
        Assert.That(session.PublicIdentifier, Has.Length.EqualTo(36));
        Assert.That(session.SessionToken, Has.Length.EqualTo(36));
        Assert.That(session.PublicIdentifier, Is.Not.EqualTo(session.SessionToken));
    }

    [Test]
    public void Cancel_WhenActive_SetsIsCancelledToTrue()
    {
        var session = LoginSession.Create("127.0.0.1");

        session.Cancel();

        Assert.That(session.IsCancelled, Is.True);
    }

    [Test]
    public void Cancel_WhenAlreadyCancelled_ThrowsContractException()
    {
        var session = LoginSession.Create("127.0.0.1");
        session.Cancel();

        Assert.Throws<ContractException>(() => session.Cancel());
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void AttachLoginToken_InvalidToken_ThrowsContractException(string invalidToken)
    {
        var session = LoginSession.Create("127.0.0.1");

        Assert.Throws<ContractException>(() => session.AttachLoginToken(invalidToken));
    }

    [Test]
    public void AttachLoginToken_WhenCancelled_ThrowsContractException()
    {
        var session = LoginSession.Create("127.0.0.1");
        session.Cancel();

        Assert.Throws<ContractException>(() => session.AttachLoginToken("valid-token"));
    }

    [Test]
    public void AttachLoginToken_WhenExpired_ThrowsContractException()
    {
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var session = LoginSession.Create("127.0.0.1", createdAt);
        var twoHoursLater = createdAt.AddHours(2);

        Assert.Throws<ContractException>(() => session.AttachLoginToken("valid-token", twoHoursLater));
    }

    [Test]
    public void AttachLoginToken_WhenActiveAndNotExpired_SetsTrimmedToken()
    {
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var session = LoginSession.Create("127.0.0.1", createdAt);
        var tenMinutesLater = createdAt.AddMinutes(10);

        session.AttachLoginToken("  my-auth-token  ", tenMinutesLater);

        Assert.That(session.LoginToken, Is.EqualTo("my-auth-token"));
    }

    [Test]
    public void IsExpired_EvaluatesAgainstGivenTimeAndTimeout()
    {
        var createdAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var session = LoginSession.Create("127.0.0.1", createdAt);

        // Within 1 hour default window
        Assert.That(session.IsExpired(createdAt.AddMinutes(59)), Is.False);

        // Exactly at or after 1 hour default window
        Assert.That(session.IsExpired(createdAt.AddHours(1)), Is.True);
        Assert.That(session.IsExpired(createdAt.AddHours(2)), Is.True);

        // Custom timeout (30 minutes)
        Assert.That(session.IsExpired(createdAt.AddMinutes(29), TimeSpan.FromMinutes(30)), Is.False);
        Assert.That(session.IsExpired(createdAt.AddMinutes(31), TimeSpan.FromMinutes(30)), Is.True);
    }
}
