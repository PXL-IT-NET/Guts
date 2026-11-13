using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Guts.Common;

namespace Guts.Domain.CourseAggregate;

public class Course : AggregateRoot
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; }

    [Required]
    public string Name { get; set; }

    public Course()
    {
    }

    public Course(string code, string name)
    {
        SetCodeAndName(code, name);
    }

    public void Update(string code, string name)
    {
        SetCodeAndName(code, name);
    }

    public void Update(string code, string name, IReadOnlyCollection<Course> allCourses)
    {
        SetCodeAndName(code, name);
        if (allCourses != null)
        {
            bool duplicateExists = allCourses.Any(c =>
                c.Id != Id && string.Equals(c.Code, Code, StringComparison.OrdinalIgnoreCase));
            Contracts.Require(!duplicateExists, $"A course with code '{Code}' already exists.");
        }
    }

    private void SetCodeAndName(string code, string name)
    {
        Contracts.Require(!string.IsNullOrWhiteSpace(code), "Course code cannot be empty.");
        Contracts.Require(!string.IsNullOrWhiteSpace(name), "Course name cannot be empty.");
        string trimmedCode = code.Trim();
        Contracts.Require(trimmedCode.Length <= 20, "Course code cannot exceed 20 characters.");
        Code = trimmedCode;
        Name = name.Trim();
    }

    public class Factory : ICourseFactory
    {
        public Course CreateNew(string code, string name, IReadOnlyCollection<Course> existingCourses)
        {
            var course = new Course(code, name);
            bool duplicateExists = existingCourses.Any(c =>
                string.Equals(c.Code, course.Code, StringComparison.OrdinalIgnoreCase));
            Contracts.Require(!duplicateExists, $"A course with code '{course.Code}' already exists.");
            return course;
        }
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}