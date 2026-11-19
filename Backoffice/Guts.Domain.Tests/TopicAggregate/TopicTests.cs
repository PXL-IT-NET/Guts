using System;
using Guts.Common.Extensions;
using Guts.Domain.TopicAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.TopicAggregate;

[TestFixture]
public class TopicTests
{
    private class DummyTopic : Topic
    {
        public DummyTopic() : base()
        {
        }

        public DummyTopic(int id) : base(id)
        {
        }
    }

    [Test]
    public void CodeSetter_WhenDescriptionIsNull_ShouldAutomaticallySetDescriptionToCode()
    {
        var topic = new DummyTopic();
        var code = new Code("Chapter_01");

        topic.Code = code;

        Assert.That(topic.Code, Is.EqualTo(code));
        Assert.That(topic.Description, Is.EqualTo(code.Value));
    }

    [Test]
    public void CodeSetter_WhenDescriptionIsEmpty_ShouldAutomaticallySetDescriptionToCode()
    {
        var topic = new DummyTopic
        {
            Description = string.Empty
        };
        var code = new Code("Chapter_02");

        topic.Code = code;

        Assert.That(topic.Code, Is.EqualTo(code));
        Assert.That(topic.Description, Is.EqualTo(code.Value));
    }

    [Test]
    public void CodeSetter_WhenDescriptionIsAlreadySet_ShouldPreserveExistingDescription()
    {
        string existingDescription = Random.Shared.NextString();
        var topic = new DummyTopic
        {
            Description = existingDescription
        };
        var code = new Code("Chapter_03");

        topic.Code = code;

        Assert.That(topic.Code, Is.EqualTo(code));
        Assert.That(topic.Description, Is.EqualTo(existingDescription));
    }

    [Test]
    public void ConstructorWithId_ValidId_ShouldSetId()
    {
        int id = Random.Shared.NextPositive();

        var topic = new DummyTopic(id);

        Assert.That(topic.Id, Is.EqualTo(id));
    }

    [Test]
    public void GetTestIds_ShouldAggregateTestIdsAcrossAllAssignments()
    {
        var topic = new DummyTopic(1);
        var a1 = new Guts.Domain.AssignmentAggregate.Assignment { Id = 10 };
        a1.AddTest("T1", id: 101);
        a1.AddTest("T2", id: 102);

        var a2 = new Guts.Domain.AssignmentAggregate.Assignment { Id = 20 };
        a2.AddTest("T3", id: 201);

        topic.AddAssignment(a1);
        topic.AddAssignment(a2);

        Assert.That(topic.GetTestIds(), Is.EquivalentTo(new[] { 101, 102, 201 }));

        topic.Assignments = null;
        Assert.That(topic.GetTestIds(), Is.Empty);
    }
}