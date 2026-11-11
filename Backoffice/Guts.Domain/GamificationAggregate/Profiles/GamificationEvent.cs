using System;
using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate.Profiles;

public class GamificationEvent : Entity
{
    public int Id { get; internal set; }
    public int StudentCourseProfileId { get; internal set; }
    public StudentCourseProfile StudentCourseProfile { get; internal set; }

    public GamificationEventType EventType { get; internal set; }
    public int TargetId { get; internal set; }
    public int XpEarned { get; internal set; }
    public DateTime CreatedAt { get; internal set; }

    internal GamificationEvent() { }

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

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}