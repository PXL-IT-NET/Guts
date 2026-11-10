using System;
using Guts.Common.Extensions;
using Guts.Domain.CourseAggregate;

namespace Guts.Domain.Tests.Builders;

public class CourseBuilder : BaseBuilder<Course>
{
    public CourseBuilder()
    {
        Item = new Course
        {
            Id = 0,
            Code = Guid.NewGuid().ToString()[..10],
            Name = Guid.NewGuid().ToString()
        };
    }

    public CourseBuilder WithId()
    {
        Item.Id = Random.Shared.NextPositive();
        return this;
    }

    public CourseBuilder WithId(int id)
    {
        Item.Id = id;
        return this;
    }

    public CourseBuilder WithCode(string code)
    {
        Item.Code = code;
        return this;
    }

    public CourseBuilder WithCourseCode(string courseCode)
    {
        return WithCode(courseCode);
    }

    public CourseBuilder WithName(string name)
    {
        Item.Name = name;
        return this;
    }
}
