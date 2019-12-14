using System;
using Guts.Common.Extensions;
using Guts.Data;
using Guts.Domain.TestRunAggregate;

namespace Guts.Business.Tests.Builders
{
    public class TestWithLastResultOfUserBuilder
    {
        private readonly Random _random;
        private readonly TestWithLastResultOfUser _testWithLastResultOfUser;

        public TestWithLastResultOfUserBuilder()
        {
            _random = new Random();
            _testWithLastResultOfUser = new TestWithLastResultOfUser
            {
                Test = new TestBuilder().Build()
            };

            _testWithLastResultOfUser.TestResult = new TestResult
            {
                Id = _random.NextPositive(),
                Message = Guid.NewGuid().ToString(),
                TestId = _testWithLastResultOfUser.Test.Id,
                Passed = _random.NextBool(),
                TestRunId = _random.NextPositive()
            };
        }

        public TestWithLastResultOfUserBuilder WithAssignmentId(int assignmentId)
        {
            _testWithLastResultOfUser.Test.AssignmentId = assignmentId;
            return this;
        }

        public TestWithLastResultOfUser Build()
        {
            return _testWithLastResultOfUser;
        }
    }
}