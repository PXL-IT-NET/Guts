using System;
using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate.Profiles;

public class StudentBadge : Entity
{
    public int Id { get; internal set; }
    public int StudentCourseProfileId { get; internal set; }
    public StudentCourseProfile StudentCourseProfile { get; internal set; }

    public Badge Badge { get; internal set; }
    public DateTime EarnedAt { get; internal set; }

    protected StudentBadge() { }

    public StudentBadge(int studentCourseProfileId, Badge badge, DateTime earnedAt)
    {
        Contracts.Require(badge != null, "Badge is required.");
        Contracts.Require(earnedAt != default, "EarnedAt must be a valid date.");
        StudentCourseProfileId = studentCourseProfileId;
        Badge = badge;
        EarnedAt = earnedAt;
    }

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}
