using System;
using Guts.Common.Extensions;
using Guts.Domain.CourseAggregate;

namespace Guts.Business.Tests.Builders
{
    public class CourseBuilder
    {
        private readonly Random _random;
        private readonly Course _course;

        public CourseBuilder()
        {
            _random = new Random();
            _course = new Course
            {
                Id = 0,
                Code = Guid.NewGuid().ToString(),
                Name = Guid.NewGuid().ToString()
            };
        }

        public CourseBuilder WithId()
        {
            _course.Id = _random.NextPositive();
            return this;
        }

        public CourseBuilder WithCourseCode(string courseCode)
        {
            _course.Code = courseCode;
            return this;
        }

        public Course Build()
        {
            return _course;
        }

        
    }
}