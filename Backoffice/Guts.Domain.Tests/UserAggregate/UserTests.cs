using System;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.UserAggregate;

[TestFixture]
public class UserTests
{
    private sealed class DerivedUser : User
    {
        public override string ToString() => nameof(DerivedUser);
    }

    [Test]
    public void Equals_NullObject_ShouldReturnFalse()
    {
        User user = new UserBuilder().WithId().Build();

        Assert.That(user.Equals(null), Is.False);
        Assert.That(user == null, Is.False);
        Assert.That(null == user, Is.False);
        Assert.That(user != null, Is.True);
        Assert.That(null != user, Is.True);
    }

    [Test]
    public void OperatorEquals_BothNull_ShouldReturnTrue()
    {
        User userA = null;
        User userB = null;

        Assert.That(userA == userB, Is.True);
        Assert.That(userA != userB, Is.False);
    }

    [Test]
    public void Equals_SameReference_ShouldReturnTrue()
    {
        User user = new UserBuilder().WithId(0).Build();
        User sameReference = user;

        Assert.That(user.Equals(sameReference), Is.True);
        Assert.That(user == sameReference, Is.True);
        Assert.That(user != sameReference, Is.False);
    }

    [Test]
    public void Equals_DifferentType_ShouldReturnFalse()
    {
        int id = Random.Shared.NextPositive();
        User user = new UserBuilder().WithId(id).Build();
        User derivedUser = new DerivedUser { Id = id };

        Assert.That(user.Equals(derivedUser), Is.False);
        Assert.That(user == derivedUser, Is.False);
        Assert.That(user != derivedUser, Is.True);
        Assert.That(user.Equals(new object()), Is.False);
    }

    [Test]
    public void Equals_ZeroId_ShouldReturnFalse()
    {
        User unpersistedA = new UserBuilder().WithId(0).Build();
        User unpersistedB = new UserBuilder().WithId(0).Build();
        User persisted = new UserBuilder().WithId().Build();

        Assert.That(unpersistedA.Equals(unpersistedB), Is.False);
        Assert.That(unpersistedA == unpersistedB, Is.False);
        Assert.That(unpersistedA != unpersistedB, Is.True);
        Assert.That(unpersistedA.Equals(persisted), Is.False);
        Assert.That(persisted.Equals(unpersistedA), Is.False);
    }

    [Test]
    public void Equals_SamePositiveId_ShouldReturnTrue()
    {
        int id = Random.Shared.NextPositive();
        User userA = new UserBuilder().WithId(id).Build();
        User userB = new UserBuilder().WithId(id).Build();

        Assert.That(userA.Equals(userB), Is.True);
        Assert.That(userA == userB, Is.True);
        Assert.That(userA != userB, Is.False);
        Assert.That(userA.GetHashCode(), Is.EqualTo(userB.GetHashCode()));
    }

    [Test]
    public void Equals_DifferentPositiveId_ShouldReturnFalse()
    {
        int idA = Random.Shared.NextPositive();
        int idB = idA + 1;
        User userA = new UserBuilder().WithId(idA).Build();
        User userB = new UserBuilder().WithId(idB).Build();

        Assert.That(userA.Equals(userB), Is.False);
        Assert.That(userA == userB, Is.False);
        Assert.That(userA != userB, Is.True);
        Assert.That(userA.GetHashCode(), Is.Not.EqualTo(userB.GetHashCode()));
    }

    [TestCase("John", "Doe", "John Doe")]
    [TestCase("  John  ", "  Doe  ", "John     Doe")]
    [TestCase("John", "", "John")]
    [TestCase("", "Doe", "Doe")]
    public void FullName_ShouldCombineFirstAndLastName(string first, string last, string expected)
    {
        var user = new User { FirstName = first, LastName = last };
        Assert.That(user.FullName, Is.EqualTo(expected));
    }

    [Test]
    public void MarkEmailVerified_DefaultTimestamp_ThrowsContractException()
    {
        var user = new UserBuilder().WithId().Build();

        Assert.Throws<ContractException>(() => user.MarkEmailVerified(default));
    }

    [Test]
    public void MarkEmailVerified_ValidTimestamp_SetsUtcTimestamp()
    {
        var user = new UserBuilder().WithId().Build();
        var timestamp = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Unspecified);

        user.MarkEmailVerified(timestamp);

        Assert.That(user.LastVerifiedOnUtc, Is.EqualTo(new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc)));
        Assert.That(user.LastVerifiedOnUtc!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void RequiresAnnualVerification_NeverVerified_ReturnsTrue()
    {
        var user = new UserBuilder().WithId().Build();

        Assert.That(user.RequiresAnnualVerification(new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc)), Is.True);
    }

    [Test]
    public void RequiresAnnualVerification_VerifiedLessThanOneYearAgo_ReturnsFalse()
    {
        var utcNow = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
        var user = new UserBuilder()
            .WithId()
            .WithLastVerifiedOnUtc(utcNow.AddYears(-1).AddSeconds(1))
            .Build();

        Assert.That(user.RequiresAnnualVerification(utcNow), Is.False);
    }

    [Test]
    public void RequiresAnnualVerification_VerifiedOneYearOrLongerAgo_ReturnsTrue()
    {
        var utcNow = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
        var exactBoundaryUser = new UserBuilder()
            .WithId()
            .WithLastVerifiedOnUtc(utcNow.AddYears(-1))
            .Build();
        var olderUser = new UserBuilder()
            .WithId()
            .WithLastVerifiedOnUtc(utcNow.AddYears(-2))
            .Build();

        Assert.That(exactBoundaryUser.RequiresAnnualVerification(utcNow), Is.True);
        Assert.That(olderUser.RequiresAnnualVerification(utcNow), Is.True);
    }
}