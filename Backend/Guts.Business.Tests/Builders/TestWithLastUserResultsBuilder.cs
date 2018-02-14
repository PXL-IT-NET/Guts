using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Data;
using Guts.Domain;

namespace Guts.Business.Tests.Builders
{
    public class TestWithLastUserResultsBuilder
    {
        private readonly Random _random;
        private readonly TestWithLastUserResults _testWithUserResults;

        public TestWithLastUserResultsBuilder()
        {
            _random = new Random();
            _testWithUserResults = new TestWithLastUserResults
            {
                Test = new TestBuilder().Build(),
                ResultsOfUsers = new List<TestResult>()
            };
        }

        public TestWithLastUserResultsBuilder WithExerciseId(int exerciseId)
        {
            _testWithUserResults.Test.ExerciseId = exerciseId;
            return this;
        }

        public TestWithLastUserResultsBuilder WithUserResults(int numberOfUsers)
        {
            var results = new List<TestResult>();
            for (int i = 0; i < numberOfUsers; i++)
            {
                results.Add(new TestResult
                {
                    Id = _random.NextPositive(),
                    Message = Guid.NewGuid().ToString(),
                    TestId = _testWithUserResults.Test.Id,
                    Passed = _random.NextBool(),
                    TestRunId = _random.NextPositive()
                });
            }
            _testWithUserResults.ResultsOfUsers = results;
            return this;
        }

        public TestWithLastUserResults Build()
        {
            return _testWithUserResults;
        }
    }
}