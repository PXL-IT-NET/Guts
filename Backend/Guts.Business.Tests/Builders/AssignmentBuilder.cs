using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Business.Tests.Builders
{
    public class AssignmentBuilder
    {
        private readonly Assignment _assignment;
        private readonly Random _random;

        public AssignmentBuilder()
        {
            _random = new Random();
            _assignment = new Assignment
            {
                Id = 0,
                TopicId = _random.NextPositive(),
                Code = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                Tests = new List<Test>()
            };
        }

        public AssignmentBuilder WithId()
        {
            _assignment.Id = _random.NextPositive();
            return this;
        }

        public AssignmentBuilder WithRandomTests(int numberOfTests)
        {
            var tests = new List<Test>();
            for (int i = 0; i < numberOfTests; i++)
            {
                var test = new Test
                {
                    Id = _random.NextPositive(),
                    TestName = Guid.NewGuid().ToString()
                };
                tests.Add(test);
            }
            _assignment.Tests = tests;
            return this;
        }

        public AssignmentBuilder WithTopic(Topic topic)
        {
            _assignment.Topic = topic;
            _assignment.TopicId = topic.Id;
            return this;
        }


        public AssignmentBuilder WithoutTopicLoaded()
        {
            _assignment.Topic = null;
            return this;
        }

        public AssignmentBuilder WithoutTestsLoaded()
        {
            _assignment.Tests = null;
            return this;
        }

        public AssignmentBuilder WithTestCodeHash(string testCodeHash)
        {
            _assignment.TestCodeHashes.Add(new TestCodeHash
            {
                Id = _random.NextPositive(),
                AssignmentId = _assignment.Id,
                Hash = testCodeHash
            });
            return this;
        }

        public Assignment Build()
        {
            return _assignment;
        }

    }
}