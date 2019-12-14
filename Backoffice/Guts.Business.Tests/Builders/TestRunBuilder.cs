using System;
using Guts.Common.Extensions;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Tests.Builders
{
    public class TestRunBuilder
    {
        private readonly Random _random;
        private readonly TestRun _testRun;

        public TestRunBuilder(Random random)
        {
            _random = random;

            _testRun = new TestRun
            {
                Id = 0,
                UserId = _random.NextPositive(),
                AssignmentId = _random.NextPositive(),
                SourceCode = Guid.NewGuid().ToString(),
                CreateDateTime = DateTime.UtcNow.AddDays(-_random.Next(1, 100))
            };
        }

        public TestRunBuilder WithId()
        {
            _testRun.Id = _random.NextPositive();
            return this;
        }

        public TestRunBuilder WithUserId(int userId)
        {
            _testRun.UserId = userId;
            return this;
        }

        public TestRunBuilder WithAssignmentId(int assignmentId)
        {
            _testRun.AssignmentId = assignmentId;
            return this;
        }

        public TestRunBuilder WithCreationDate(DateTime date)
        {
            _testRun.CreateDateTime = date;
            return this;
        }

        public TestRun Build()
        {
            return _testRun;
        }
    }
}