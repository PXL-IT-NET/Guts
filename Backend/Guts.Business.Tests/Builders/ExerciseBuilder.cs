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
                Id = 0,
                ChapterId = _random.NextPositive(),
                Code = Guid.NewGuid().ToString(),
                Tests = new List<Test>()
            };
        }

        public ExerciseBuilder WithId()
        {
            _exercise.Id = _random.NextPositive();
            return this;
        }

        public ExerciseBuilder WithRandomTests(int numberOfTests)
        {
            var tests = new List<Test>();
            for (int i = 0; i < numberOfTests; i++)
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

        public ExerciseBuilder WithChapter(Chapter chapter)
        {
            _exercise.Chapter = chapter;
            _exercise.ChapterId = chapter.Id;
            return this;
        }


        public ExerciseBuilder WithoutChapterLoaded()
        {
            _exercise.Chapter = null;
            return this;
        }

        public ExerciseBuilder WithoutTestsLoaded()
        {
            _exercise.Tests = null;
            return this;
        }

        public ExerciseBuilder WithTestCodeHash(string testCodeHash)
        {
            _exercise.TestCodeHashes.Add(new TestCodeHash
            {
                Id = _random.NextPositive(),
                AssignmentId = _exercise.Id,
                Hash = testCodeHash
            });
            return this;
        }

        public Exercise Build()
        {
            return _exercise;
        }

    }
}