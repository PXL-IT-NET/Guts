using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Business.Tests.Builders
{
    public class ChapterBuilder
    {
        private readonly Random _random;
        private readonly Chapter _chapter;

        public ChapterBuilder()
        {
            _random = new Random();
            _chapter = new Chapter
            {
                Id = 0,
                Number = _random.NextPositive(),
                CourseId = _random.NextPositive(),
                PeriodId = _random.NextPositive(),
                Exercises = new Collection<Exercise>()
            };
        }

        public ChapterBuilder WithId()
        {
            _chapter.Id = _random.NextPositive();
            return this;
        }

        public ChapterBuilder WithCourse(string courseCode)
        {
            _chapter.Course = new Course
            {
                Id = _random.NextPositive(),
                Code = courseCode,
                Name = Guid.NewGuid().ToString()
            };
            _chapter.CourseId = _chapter.Course.Id;
            return this;
        }

        public ChapterBuilder WithPeriod(Period period)
        {
            _chapter.Period = period;
            _chapter.PeriodId = period.Id;
            return this;
        }

        public ChapterBuilder WithExercises(int maxNumberOfExercises, int maxNumberOfTestsPerExercise)
        {
            _chapter.Exercises = new List<Exercise>();
            var numberOfExercises = _random.Next(1, maxNumberOfExercises + 1);

            for (int i = 0; i < numberOfExercises; i++)
            {
                var numberOfTests = _random.Next(0, maxNumberOfTestsPerExercise + 1);
                var exercise = new ExerciseBuilder().Build();

                _chapter.Exercises.Add(exercise);
            }

            return this;
        }

        public Chapter Build()
        {
            return _chapter;
        }

        
    }
}