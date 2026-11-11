using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using NUnit.Framework;

namespace Guts.Domain.Tests;

[TestFixture]
public class EntityTests
{
    private class DummySingleIdEntity : Entity
    {
        public int Id { get; set; }

        public DummySingleIdEntity(int id)
        {
            Id = id;
        }

        protected override IEnumerable<object> GetIdComponents()
        {
            yield return Id;
        }
    }

    private class OtherDummyEntity : Entity
    {
        public int Id { get; set; }

        public OtherDummyEntity(int id)
        {
            Id = id;
        }

        protected override IEnumerable<object> GetIdComponents()
        {
            yield return Id;
        }
    }

    private class DummyCompositeEntity : Entity
    {
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string Key { get; set; }

        public DummyCompositeEntity(string entityType, int entityId, string key)
        {
            EntityType = entityType;
            EntityId = entityId;
            Key = key;
        }

        protected override IEnumerable<object> GetIdComponents()
        {
            yield return EntityType;
            yield return EntityId;
            yield return Key;
        }
    }

    [Test]
    public void Equals_NullObject_ShouldReturnFalse()
    {
        var entity = new DummySingleIdEntity(Random.Shared.NextPositive());

        Assert.That(entity.Equals(null), Is.False);
        Assert.That(entity == null, Is.False);
        Assert.That(null == entity, Is.False);
        Assert.That(entity != null, Is.True);
        Assert.That(null != entity, Is.True);
    }

    [Test]
    public void OperatorEquals_BothNull_ShouldReturnTrue()
    {
        DummySingleIdEntity entityA = null;
        DummySingleIdEntity entityB = null;

        Assert.That(entityA == entityB, Is.True);
        Assert.That(entityA != entityB, Is.False);
    }

    [Test]
    public void Equals_SameReference_ShouldReturnTrue()
    {
        var entity = new DummySingleIdEntity(1);
        var sameReference = entity;

        Assert.That(entity.Equals(sameReference), Is.True);
        Assert.That(entity == sameReference, Is.True);
        Assert.That(entity != sameReference, Is.False);
    }

    [Test]
    public void Equals_DifferentEntityTypesWithSameIdComponents_ShouldReturnFalse()
    {
        int id = Random.Shared.NextPositive();
        Entity entityA = new DummySingleIdEntity(id);
        Entity entityB = new OtherDummyEntity(id);

        Assert.That(entityA.Equals(entityB), Is.False);
        Assert.That(entityA == entityB, Is.False);
        Assert.That(entityA != entityB, Is.True);
    }

    [Test]
    public void Equals_NonEntityObject_ShouldReturnFalse()
    {
        var entity = new DummySingleIdEntity(Random.Shared.NextPositive());

        Assert.That(entity.Equals(new object()), Is.False);
    }

    [Test]
    public void Equals_EitherOrBothTransient_ShouldReturnFalse()
    {
        int positiveId = Random.Shared.NextPositive();
        var unpersisted1 = new DummySingleIdEntity(0);
        var unpersisted2 = new DummySingleIdEntity(0);
        var persisted = new DummySingleIdEntity(positiveId);

        Assert.That(unpersisted1.Equals(unpersisted2), Is.False);
        Assert.That(unpersisted1 == unpersisted2, Is.False);
        Assert.That(unpersisted1 != unpersisted2, Is.True);

        Assert.That(unpersisted1.Equals(persisted), Is.False);
        Assert.That(persisted.Equals(unpersisted1), Is.False);
    }

    [Test]
    public void Equals_SameTypeAndMatchingSingleId_ShouldReturnTrue()
    {
        int id = Random.Shared.NextPositive();
        var entityA = new DummySingleIdEntity(id);
        var entityB = new DummySingleIdEntity(id);

        Assert.That(entityA.Equals(entityB), Is.True);
        Assert.That(entityA == entityB, Is.True);
        Assert.That(entityA != entityB, Is.False);
        Assert.That(entityA.GetHashCode(), Is.EqualTo(entityB.GetHashCode()));
    }

    [Test]
    public void Equals_SameTypeAndDifferentSingleId_ShouldReturnFalse()
    {
        int idA = Random.Shared.NextPositive();
        int idB = idA + 1;
        var entityA = new DummySingleIdEntity(idA);
        var entityB = new DummySingleIdEntity(idB);

        Assert.That(entityA.Equals(entityB), Is.False);
        Assert.That(entityA == entityB, Is.False);
        Assert.That(entityA != entityB, Is.True);
        Assert.That(entityA.GetHashCode(), Is.Not.EqualTo(entityB.GetHashCode()));
    }

    [Test]
    public void Equals_CompositeIdentifier_MatchingComponents_ShouldReturnTrue()
    {
        var entityA = new DummyCompositeEntity("TypeA", 42, "Key1");
        var entityB = new DummyCompositeEntity("TypeA", 42, "Key1");

        Assert.That(entityA.Equals(entityB), Is.True);
        Assert.That(entityA == entityB, Is.True);
        Assert.That(entityA != entityB, Is.False);
        Assert.That(entityA.GetHashCode(), Is.EqualTo(entityB.GetHashCode()));
    }

    [Test]
    public void Equals_CompositeIdentifier_DifferingComponents_ShouldReturnFalse()
    {
        var entityA = new DummyCompositeEntity("TypeA", 42, "Key1");
        var entityB = new DummyCompositeEntity("TypeA", 42, "Key2");
        var entityC = new DummyCompositeEntity("TypeA", 43, "Key1");
        var entityD = new DummyCompositeEntity("TypeB", 42, "Key1");

        Assert.That(entityA.Equals(entityB), Is.False);
        Assert.That(entityA.Equals(entityC), Is.False);
        Assert.That(entityA.Equals(entityD), Is.False);
        Assert.That(entityA.GetHashCode(), Is.Not.EqualTo(entityB.GetHashCode()));
    }
}
