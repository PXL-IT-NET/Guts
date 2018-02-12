using System;
using System.Collections.Generic;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Business.Tests.Builders
{
    public class ExerciseBuilder
    {
        private readonly Exercise _exercise;
        private readonly Random _random;

        public ExerciseBuilder()
        {
            _random = new Random();
            _exercise = new Exercise
            {
                Id = _random.NextPositive(),
                ChapterId = _random.NextPositive(),
                Tests = new List<Test>()
            };
        }

        public ExerciseBuilder WithRandomTests(int numberOfTestResults)
        {
            var tests = new List<Test>();
            for (int i = 0; i < numberOfTestResults; i++)
            {
                var test = new Test
                {
                    Id = _random.NextPositive(),
                    TestName = Guid.NewGuid().ToString()
                };
                tests.Add(test);
            }
            _exercise.Tests = tests;
            return this;
        }

        public Exercise Build()
        {
            return _exercise;
        }
    }
}