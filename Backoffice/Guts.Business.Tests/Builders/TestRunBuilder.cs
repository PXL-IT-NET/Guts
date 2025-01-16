using System;
using Guts.Common.Extensions;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.Tests.Builders;

namespace Guts.Business.Tests.Builders
{
    public class TestRunBuilder
    {
        private readonly TestRun _testRun;

        public TestRunBuilder(Random random)
        {

            _testRun = new TestRun
            {
                Id = 0,
                UserId = Random.Shared.NextPositive(),
                AssignmentId = Random.Shared.NextPositive(),
                CreateDateTime = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 100))
            };
        }

        public TestRunBuilder WithCreationDate(DateTime date)
        {
            _testRun.CreateDateTime = date;
            return this;
        }

        public TestRunBuilder WithUser()
        {
            var user = new UserBuilder().WithId().Build();
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