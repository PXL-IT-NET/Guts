using System;
using Guts.Common.Extensions;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.ValueObjects;

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
                CreateDateTime = DateTime.UtcNow.AddDays(-_random.Next(1, 100))
            };
        }

        public TestRunBuilder WithCreationDate(DateTime date)
        {
            _testRun.CreateDateTime = date;
            return this;
        }

        public TestRunBuilder WithUser()
        {
            var user = new UserBuilder(_random).WithId().Build();
            _testRun.User = user;
            _testRun.UserId = user.Id;
            return this;
        }

        public TestRun Build()
        {
            return _testRun;
        }

        
    }
}