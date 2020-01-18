using System;
using Guts.Common.Extensions;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Domain.Tests.Builders
{
    public class TestResultBuilder
    {
        private readonly Random _random;
        private readonly TestResult _testResult;

        public TestResultBuilder()
        {
            _random = new Random();

            _testResult = new TestResult
            {
                Id = 0,
                Passed = _random.NextBool(),
                UserId = _random.NextPositive(),
                TestRunId = _random.NextPositive(),
                Message = Guid.NewGuid().ToString(),
                TestId = _random.NextPositive()
            };
        }

        public TestResultBuilder WithId()
        {
            _testResult.Id = _random.NextPositive();
            return this;
        }

        public TestResultBuilder WithPassed(bool passed)
        {
            _testResult.Passed = passed;
            return this;
        }

        public TestResultBuilder WithUser(int userId)
        {
            _testResult.UserId = userId;
            return this;
        }

        public TestResultBuilder WithTest(int testId)
        {
            _testResult.TestId = testId;
            return this;
        }

        public TestResultBuilder WithTest(Test test)
        {
            _testResult.TestId = test.Id;
            _testResult.Test = test;
            return this;
        }

        public TestResult Build()
        {
            return _testResult;
        }
    }
}