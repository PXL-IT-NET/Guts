using Guts.Common.Extensions;
using Guts.Data;
using Guts.Domain;
using System;
using System.Collections.Generic;

namespace Guts.Business.Tests.Builders
{
    public class TestWithLastResultOfMultipleUsersBuilder
    {
        private readonly Random _random;
        private readonly TestWithLastResultOfMultipleUsers _testWithLastResultOfMultipleUsers;
        private readonly List<TestResult> _testResults;

        public TestWithLastResultOfMultipleUsersBuilder()
        {
            _random = new Random();
            _testResults = new List<TestResult>();

            _testWithLastResultOfMultipleUsers = new TestWithLastResultOfMultipleUsers
            {
                Test = new TestBuilder().Build(),
                TestResults = _testResults
            };
        }

        public TestWithLastResultOfMultipleUsersBuilder WithAssignmentId(int assignmentId)
        {
            _testWithLastResultOfMultipleUsers.Test.AssignmentId = assignmentId;
            return this;
        }

        public TestWithLastResultOfMultipleUsersBuilder WithUserResult(int userId, bool passed)
        {
            var result = new TestResult
            {
                Id = _random.NextPositive(),
                Passed = passed,
                UserId = userId,
                TestRunId = _random.NextPositive(),
                Message = Guid.NewGuid().ToString(),
                TestId = _testWithLastResultOfMultipleUsers.Test.Id
            };
            _testResults.Add(result);

            return this;
        }

        public TestWithLastResultOfMultipleUsers Build()
        {
            return _testWithLastResultOfMultipleUsers;
        }
    }
}