using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Domain.Tests.Builders
{
    internal class AssignmentBuilder : BaseBuilder<Assignment>
    {
        public AssignmentBuilder()
        {
            Item = new Assignment
            {
                TopicId = Random.NextPositive(),
                Code = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                Tests = new List<Test>()
            };
        }

        public AssignmentBuilder WithId()
        {
            Item.Id = Random.NextPositive();
            return this;
        }

        public AssignmentBuilder WithId(int id)
        {
            Item.Id = id;
            return this;
        }

        public AssignmentBuilder WithCode(string code)
        {
            Item.Code = code;
            return this;
        }

        public AssignmentBuilder WithRandomTests(int numberOfTests)
        {
            var tests = new List<Test>();
            for (int i = 0; i < numberOfTests; i++)
            {
                var test = new Test
                {
                    Id = Random.NextPositive(),
                    TestName = Guid.NewGuid().ToString(),
                    AssignmentId = Item.Id
                };
                tests.Add(test);
            }
            Item.Tests = tests;
            return this;
        }

        public AssignmentBuilder WithTopic(Topic topic)
        {
            Item.Topic = topic;
            Item.TopicId = topic.Id;
            return this;
        }

        public AssignmentBuilder WithoutTopicLoaded()
        {
            Item.Topic = null;
            return this;
        }

        public AssignmentBuilder WithoutTestsLoaded()
        {
            Item.Tests = null;
            return this;
        }

        public AssignmentBuilder WithTestCodeHash(string testCodeHash)
        {
            Item.TestCodeHashes.Add(new TestCodeHash
            {
                Id = Random.NextPositive(),
                AssignmentId = Item.Id,
                Hash = testCodeHash
            });
            return this;
        }
    }
}