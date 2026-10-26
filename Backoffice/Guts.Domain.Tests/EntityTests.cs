using System;
using Guts.Common;
using Guts.Common.Extensions;
using NUnit.Framework;

namespace Guts.Domain.Tests;

[TestFixture]
public class EntityTests
{
    private class DummyEntity : Entity
    {
        public DummyEntity() : base()
        {
        }

        public DummyEntity(int id) : base(id)
        {
        }
    }

    private class OtherDummyEntity : Entity
    {
        public OtherDummyEntity() : base()
        {
        }

        public OtherDummyEntity(int id) : base(id)
        {
        }
    }

    [Test]
    public void DefaultConstructor_ShouldInitializeIdToZero()
    {
        var entity = new DummyEntity();

        Assert.That(entity.Id, Is.Zero);
    }

    [Test]
    public void ConstructorWithId_ValidPositiveId_ShouldSetId()
    {
        int id = Random.Shared.NextPositive();

        var entity = new DummyEntity(id);

        Assert.That(entity.Id, Is.EqualTo(id));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-100)]
    public void ConstructorWithId_ZeroOrNegativeId_ShouldThrowContractException(int invalidId)
    {
        Assert.That(() => new DummyEntity(invalidId), Throws.InstanceOf<ContractException>());
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(42)]
    public void IdSetter_ZeroOrPositiveValue_ShouldSetId(int validId)
    {
        var entity = new DummyEntity();

        entity.Id = validId;

        Assert.That(entity.Id, Is.EqualTo(validId));
    }

    [TestCase(-1)]
    [TestCase(-50)]
    public void IdSetter_NegativeValue_ShouldThrowContractException(int negativeId)
    {
        var entity = new DummyEntity();

        Assert.That(() => entity.Id = negativeId, Throws.InstanceOf<ContractException>());
    }

    [Test]
    public void Equals_NullObject_ShouldReturnFalse()
    {
        var entity = new DummyEntity(Random.Shared.NextPositive());

        Assert.That(entity.Equals(null), Is.False);
        Assert.That(entity == null, Is.False);
        Assert.That(null == entity, Is.False);
        Assert.That(entity != null, Is.True);
        Assert.That(null != entity, Is.True);
    }

    [Test]
    public void OperatorEquals_BothNull_ShouldReturnTrue()
    {
        DummyEntity entityA = null;
        DummyEntity entityB = null;

        Assert.That(entityA == entityB, Is.True);
        Assert.That(entityA != entityB, Is.False);
    }

    [Test]
    public void Equals_SameReference_ShouldReturnTrue()
    {
        var entity = new DummyEntity();
        var sameReference = entity;

        Assert.That(entity.Equals(sameReference), Is.True);
        Assert.That(entity == sameReference, Is.True);
        Assert.That(entity != sameReference, Is.False);
    }

    [Test]
    public void Equals_DifferentEntityTypesWithSameId_ShouldReturnFalse()
    {
        int id = Random.Shared.NextPositive();
        Entity entityA = new DummyEntity(id);
        Entity entityB = new OtherDummyEntity(id);

        Assert.That(entityA.Equals(entityB), Is.False);
        Assert.That(entityA == entityB, Is.False);
        Assert.That(entityA != entityB, Is.True);
    }

    [Test]
    public void Equals_NonEntityObject_ShouldReturnFalse()
    {
        var entity = new DummyEntity(Random.Shared.NextPositive());

        Assert.That(entity.Equals(new object()), Is.False);
    }

    [Test]
    public void Equals_EitherOrBothHaveZeroId_ShouldReturnFalse()
    {
        int positiveId = Random.Shared.NextPositive();
        var unpersisted1 = new DummyEntity();
        var unpersisted2 = new DummyEntity();
        var persisted = new DummyEntity(positiveId);

        Assert.That(unpersisted1.Equals(unpersisted2), Is.False);
        Assert.That(unpersisted1 == unpersisted2, Is.False);
        Assert.That(unpersisted1 != unpersisted2, Is.True);

        Assert.That(unpersisted1.Equals(persisted), Is.False);
        Assert.That(persisted.Equals(unpersisted1), Is.False);
    }

    [Test]
    public void Equals_SameTypeAndMatchingPositiveId_ShouldReturnTrue()
    {
        int id = Random.Shared.NextPositive();
        var entityA = new DummyEntity(id);
        var entityB = new DummyEntity(id);

        Assert.That(entityA.Equals(entityB), Is.True);
        Assert.That(entityA == entityB, Is.True);
        Assert.That(entityA != entityB, Is.False);
        Assert.That(entityA.GetHashCode(), Is.EqualTo(entityB.GetHashCode()));
    }

    [Test]
    public void Equals_SameTypeAndDifferentPositiveId_ShouldReturnFalse()
    {
        int idA = Random.Shared.NextPositive();
        int idB = idA + 1;
        var entityA = new DummyEntity(idA);
        var entityB = new DummyEntity(idB);

        Assert.That(entityA.Equals(entityB), Is.False);
        Assert.That(entityA == entityB, Is.False);
        Assert.That(entityA != entityB, Is.True);
        Assert.That(entityA.GetHashCode(), Is.Not.EqualTo(entityB.GetHashCode()));
    }
}