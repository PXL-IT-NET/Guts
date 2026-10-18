using System;
using Guts.Common.Extensions;
using Guts.Domain.RoleAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.RoleAggregate
{
    [TestFixture]
    public class RoleTests
    {
        private class DerivedRole : Role
        {
        }

        [Test]
        public void Equals_NullObject_ShouldReturnFalse()
        {
            var role = new Role { Id = Random.Shared.NextPositive() };

            Assert.That(role.Equals(null), Is.False);
            Assert.That(role == null, Is.False);
            Assert.That(null == role, Is.False);
            Assert.That(role != null, Is.True);
            Assert.That(null != role, Is.True);
        }

        [Test]
        public void OperatorEquals_BothNull_ShouldReturnTrue()
        {
            Role roleA = null;
            Role roleB = null;

            Assert.That(roleA == roleB, Is.True);
            Assert.That(roleA != roleB, Is.False);
        }

        [Test]
        public void Equals_SameReference_ShouldReturnTrue()
        {
            var role = new Role { Id = 0 };
            Role sameReference = role;

            Assert.That(role.Equals(sameReference), Is.True);
            Assert.That(role == sameReference, Is.True);
            Assert.That(role != sameReference, Is.False);
        }

        [Test]
        public void Equals_DifferentType_ShouldReturnFalse()
        {
            int id = Random.Shared.NextPositive();
            var role = new Role { Id = id };
            Role derivedRole = new DerivedRole { Id = id };

            Assert.That(role.Equals(derivedRole), Is.False);
            Assert.That(role == derivedRole, Is.False);
            Assert.That(role != derivedRole, Is.True);
            Assert.That(role.Equals(new object()), Is.False);
        }

        [Test]
        public void Equals_ZeroId_ShouldReturnFalse()
        {
            var unpersistedA = new Role { Id = 0 };
            var unpersistedB = new Role { Id = 0 };
            var persisted = new Role { Id = Random.Shared.NextPositive() };

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
            var roleA = new Role { Id = id };
            var roleB = new Role { Id = id };

            Assert.That(roleA.Equals(roleB), Is.True);
            Assert.That(roleA == roleB, Is.True);
            Assert.That(roleA != roleB, Is.False);
            Assert.That(roleA.GetHashCode(), Is.EqualTo(roleB.GetHashCode()));
        }

        [Test]
        public void Equals_DifferentPositiveId_ShouldReturnFalse()
        {
            int idA = Random.Shared.NextPositive();
            int idB = idA + 1;
            var roleA = new Role { Id = idA };
            var roleB = new Role { Id = idB };

            Assert.That(roleA.Equals(roleB), Is.False);
            Assert.That(roleA == roleB, Is.False);
            Assert.That(roleA != roleB, Is.True);
            Assert.That(roleA.GetHashCode(), Is.Not.EqualTo(roleB.GetHashCode()));
        }
    }
}
