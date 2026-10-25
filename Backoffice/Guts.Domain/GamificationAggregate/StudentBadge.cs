using System;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate
{
    public class StudentBadge : Entity
    {
        public int StudentCourseProfileId { get; set; }
        public StudentCourseProfile StudentCourseProfile { get; set; }

        public BadgeType BadgeType { get; set; }
        public DateTime EarnedAt { get; set; }

        public StudentBadge() { }

        public StudentBadge(int studentCourseProfileId, BadgeType badgeType, DateTime earnedAt)
        {
            Contracts.Require(earnedAt != default, "EarnedAt must be a valid date.");
            StudentCourseProfileId = studentCourseProfileId;
            BadgeType = badgeType;
            EarnedAt = earnedAt;
        }
    }
}
