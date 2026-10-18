using System;
using Guts.Common.Extensions;
using Guts.Domain.TopicAggregate;
using Guts.Domain.ValueObjects;
using NUnit.Framework;

namespace Guts.Domain.Tests.TopicAggregate
{
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
    }
}
