using System;
using Guts.Common.Extensions;
using Guts.Domain.Tests.Builders;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.UserAggregate
{
    [TestFixture]
    public class UserTests
    {
        private class DerivedUser : User
        {
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
    }
}
