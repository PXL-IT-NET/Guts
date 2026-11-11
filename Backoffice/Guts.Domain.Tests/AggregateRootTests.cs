using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using NUnit.Framework;

namespace Guts.Domain.Tests;

[TestFixture]
public class AggregateRootTests
{
    private class DummyAggregateRoot : AggregateRoot
    {
        public int Id { get; set; }

        public DummyAggregateRoot()
        {
        }

        public DummyAggregateRoot(int id)
        {
            Id = id;
        }

        protected override IEnumerable<object> GetIdComponents()
        {
            yield return Id;
        }
    }

    [Test]
    public void ShouldInheritFromEntity()
    {
        var aggregate = new DummyAggregateRoot();
        Assert.That(aggregate, Is.InstanceOf<Entity>());
        Assert.That(aggregate, Is.InstanceOf<IEntity>());
    }

    [Test]
    public void IdentityValues_ShouldReflectIdComponents()
    {
        int id = Random.Shared.NextPositive();
        var aggregate = new DummyAggregateRoot(id);

        Assert.That(aggregate.IdentityValues, Is.EquivalentTo(new object[] { id }));
    }

    [Test]
    public void Equals_MatchingIds_ShouldBeEqual()
    {
        int id = Random.Shared.NextPositive();
        var aggregateA = new DummyAggregateRoot(id);
        var aggregateB = new DummyAggregateRoot(id);

        Assert.That(aggregateA.Equals(aggregateB), Is.True);
        Assert.That(aggregateA == aggregateB, Is.True);
        Assert.That(aggregateA.GetHashCode(), Is.EqualTo(aggregateB.GetHashCode()));
    }

    [Test]
    public void Equals_DifferentIds_ShouldNotBeEqual()
    {
        int idA = Random.Shared.NextPositive();
        int idB = idA + 1;
        var aggregateA = new DummyAggregateRoot(idA);
        var aggregateB = new DummyAggregateRoot(idB);

        Assert.That(aggregateA.Equals(aggregateB), Is.False);
        Assert.That(aggregateA == aggregateB, Is.False);
    }
}

