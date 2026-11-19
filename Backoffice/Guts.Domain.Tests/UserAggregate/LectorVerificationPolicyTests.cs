using System;
using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.UserAggregate;

[TestFixture]
public class LectorVerificationPolicyTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void IsLocked_NullUserOrNullRoles_ReturnsFalse()
    {
        var user = new UserBuilder().WithId().Build();

        Assert.That(LectorVerificationPolicy.IsLocked(null, [Role.Constants.Lector], UtcNow), Is.False);
        Assert.That(LectorVerificationPolicy.IsLocked(user, null, UtcNow), Is.False);
    }

    [Test]
    public void IsLocked_StudentWithoutLectorRole_NeverLocked()
    {
        var student = new UserBuilder().WithId().Build();

        Assert.That(LectorVerificationPolicy.IsLocked(student, [Role.Constants.Student], UtcNow), Is.False);
    }

    [Test]
    public void IsLocked_AdminOnlyWithoutLectorRole_NeverLocked()
    {
        var admin = new UserBuilder().WithId().Build();

        Assert.That(LectorVerificationPolicy.IsLocked(admin, [Role.Constants.Admin], UtcNow), Is.False);
    }

    [Test]
    public void IsLocked_LectorNeverVerified_ReturnsTrue()
    {
        var lector = new UserBuilder().WithId().Build();

        Assert.That(LectorVerificationPolicy.IsLocked(lector, [Role.Constants.Lector], UtcNow), Is.True);
    }

    [Test]
    public void IsLocked_DualLectorAdminNeverVerified_ReturnsTrue()
    {
        var dualUser = new UserBuilder().WithId().Build();

        Assert.That(
            LectorVerificationPolicy.IsLocked(dualUser, [Role.Constants.Lector, Role.Constants.Admin], UtcNow),
            Is.True);
    }

    [Test]
    public void IsLocked_LectorVerifiedWithinLastYear_ReturnsFalse()
    {
        var lector = new UserBuilder()
            .WithId()
            .WithLastVerifiedOnUtc(UtcNow.AddMonths(-6))
            .Build();

        Assert.That(LectorVerificationPolicy.IsLocked(lector, [Role.Constants.Lector], UtcNow), Is.False);
    }

    [Test]
    public void IsLocked_LectorVerifiedOneYearAgo_ReturnsTrue()
    {
        var lector = new UserBuilder()
            .WithId()
            .WithLastVerifiedOnUtc(UtcNow.AddYears(-1))
            .Build();

        Assert.That(LectorVerificationPolicy.IsLocked(lector, [Role.Constants.Lector], UtcNow), Is.True);
    }

    [Test]
    public void EnsureCanUnlock_NullUser_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() =>
            LectorVerificationPolicy.EnsureCanUnlock(null, [Role.Constants.Lector], UtcNow));
    }

    [Test]
    public void EnsureCanUnlock_NonLector_ThrowsContractException()
    {
        var student = new UserBuilder().WithId().Build();

        Assert.Throws<ContractException>(() =>
            LectorVerificationPolicy.EnsureCanUnlock(student, [Role.Constants.Student], UtcNow));
    }

    [Test]
    public void EnsureCanUnlock_AlreadyVerifiedLector_ThrowsContractException()
    {
        var verifiedLector = new UserBuilder()
            .WithId()
            .WithLastVerifiedOnUtc(UtcNow.AddDays(-10))
            .Build();

        Assert.Throws<ContractException>(() =>
            LectorVerificationPolicy.EnsureCanUnlock(verifiedLector, [Role.Constants.Lector], UtcNow));
    }

    [Test]
    public void EnsureCanUnlock_LockedLector_DoesNotThrow()
    {
        var lockedLector = new UserBuilder().WithId().Build();

        Assert.DoesNotThrow(() =>
            LectorVerificationPolicy.EnsureCanUnlock(lockedLector, [Role.Constants.Lector], UtcNow));
    }

    [Test]
    public void CanUnlock_ShouldMatchIsLockedState()
    {
        var lockedLector = new UserBuilder().WithId().Build();
        var verifiedLector = new UserBuilder().WithId().WithLastVerifiedOnUtc(UtcNow.AddDays(-5)).Build();

        Assert.That(LectorVerificationPolicy.CanUnlock(lockedLector, [Role.Constants.Lector], UtcNow), Is.True);
        Assert.That(LectorVerificationPolicy.CanUnlock(verifiedLector, [Role.Constants.Lector], UtcNow), Is.False);
    }
}
