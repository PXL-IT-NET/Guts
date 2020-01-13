using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Guts.Common.Extensions;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ChapterAggregate;

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
                Code = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                CourseId = _random.NextPositive(),
                PeriodId = _random.NextPositive(),
                Assignments = new Collection<Assignment>()
            };
        }

        public ChapterBuilder WithId()
        {
            _chapter.Id = _random.NextPositive();
            return this;
        }

        public ChapterBuilder WithCourse()
        {
            _chapter.Course = new CourseBuilder().WithId().Build();
            _chapter.CourseId = _chapter.Course.Id;
            return this;
        }

        public ChapterBuilder WithCourse(string courseCode)
        {
            _chapter.Course = new CourseBuilder().WithId().WithCourseCode(courseCode).Build();
            _chapter.CourseId = _chapter.Course.Id;
            return this;
        }

        public ChapterBuilder WithoutCourseLoaded()
        {
            _chapter.Course = null;
            return this;
        }

        public ChapterBuilder WithPeriod(Period period)
        {
            _chapter.Period = period;
            _chapter.PeriodId = period.Id;
            return this;
        }

        public ChapterBuilder WithAssignments(int numberOfAssignments, int numberOfTestsPerAssignment)
        {
            _chapter.Assignments = new List<Assignment>();

            for (int i = 0; i < numberOfAssignments; i++)
            {
                var assignment = new AssignmentBuilder()
                    .WithId()
                    .WithRandomTests(numberOfTestsPerAssignment)
                    .Build();

                _chapter.Assignments.Add(assignment);
            }

            return this;
        }

        public ChapterBuilder WithCode(string code)
        {
            _chapter.Code = code;
            return this;
        }

        public Chapter Build()
        {
            return _chapter;
        }
    }
}