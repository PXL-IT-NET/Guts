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
                Id = RandomExtensions.NextPositive(_random),
                ChapterId = RandomExtensions.NextPositive(_random),
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

        //public ExerciseBuilder WithTestsMatching(IEnumerable<TestResultModel> testResultModels)
        //{
        //    foreach (var testResultModel in testResultModels)
        //    {
        //        var test = new Test
        //        {
        //            Id = RandomExtensions.NextPositive(_random),
        //            TestName = testResultModel.TestName
        //        };
        //        _exercise.Tests.Add(test);
        //    }
        //    return this;
        //}

        public Exercise Build()
        {
            return _exercise;
        }
    }
}