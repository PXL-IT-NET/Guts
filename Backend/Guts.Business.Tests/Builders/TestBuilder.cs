using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Business.Tests.Builders
{
    public class TestBuilder
    {
        private readonly Random _random;
        private readonly Test _test;

        public TestBuilder()
        {
            _random = new Random();
            _test = new Test
            {
                Id = 0,
                AssignmentId = _random.NextPositive(),
                Results = new List<TestResult>(),
                TestName = Guid.NewGuid().ToString()
            };
        }

        public TestBuilder WithId()
        {
            _test.Id = _random.NextPositive();
            return this;
        }

        public TestBuilder WithAssignmentId(int exerciseId)
        {
            _test.AssignmentId = exerciseId;
            return this;
        }

        public Test Build()
        {
            return _test;
        }
    }
}