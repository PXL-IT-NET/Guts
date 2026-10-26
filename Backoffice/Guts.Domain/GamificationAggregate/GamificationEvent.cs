using System;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate;

public class GamificationEvent : Entity
{
    public int StudentCourseProfileId { get; set; }
    public StudentCourseProfile StudentCourseProfile { get; set; }

    public GamificationEventType EventType { get; set; }
    public int TargetId { get; set; }
    public int XpEarned { get; set; }
    public DateTime CreatedAt { get; set; }

    public GamificationEvent() { }

    public GamificationEvent(int studentCourseProfileId, GamificationEventType eventType, int targetId, int xpEarned, DateTime createdAt)
    {
        Contracts.Require(xpEarned >= 0, "XpEarned must be non-negative.");
        Contracts.Require(createdAt != default, "CreatedAt must be a valid date.");

        StudentCourseProfileId = studentCourseProfileId;
        EventType = eventType;
        TargetId = targetId;
        XpEarned = xpEarned;
        CreatedAt = createdAt;
    }
}