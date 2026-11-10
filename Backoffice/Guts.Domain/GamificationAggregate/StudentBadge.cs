using System;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate;

public class StudentBadge : Entity
{
    public int StudentCourseProfileId { get; set; }
    public StudentCourseProfile StudentCourseProfile { get; set; }

    public Badge Badge { get; set; }
    public DateTime EarnedAt { get; set; }

    public StudentBadge() { }

    public StudentBadge(int studentCourseProfileId, Badge badge, DateTime earnedAt)
    {
        Contracts.Require(badge != null, "Badge is required.");
        Contracts.Require(earnedAt != default, "EarnedAt must be a valid date.");
        StudentCourseProfileId = studentCourseProfileId;
        Badge = badge;
        EarnedAt = earnedAt;
    }
}
