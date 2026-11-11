using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        Contracts.Require(!string.IsNullOrWhiteSpace(code), "Course code cannot be empty.");
        Contracts.Require(!string.IsNullOrWhiteSpace(name), "Course name cannot be empty.");
        Code = code.Trim();
        Name = name.Trim();
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}