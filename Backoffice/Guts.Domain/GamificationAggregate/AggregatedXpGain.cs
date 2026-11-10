using System;

namespace Guts.Domain.GamificationAggregate;

public class AggregatedXpGain
{
    public string Description { get; }
    public int XpEarned { get; }
    public DateTime CreatedAt { get; }

    public AggregatedXpGain(string description, int xpEarned, DateTime createdAt)
    {
        Description = description ?? string.Empty;
        XpEarned = xpEarned;
        CreatedAt = createdAt;
    }
}
