using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain.GamificationAggregate.Competitions;
using Guts.Domain.GamificationAggregate.Profiles;

namespace Guts.Domain.GamificationAggregate;

public class RecentGainsSummary
{
    public int TotalXpGained { get; }
    public DateTime? PreviousVisitDate { get; }
    public IReadOnlyList<AggregatedXpGain> Gains { get; }

    public RecentGainsSummary(DateTime? previousVisitDate, IReadOnlyList<GamificationEvent> recentEvents)
    {
        PreviousVisitDate = previousVisitDate;
        recentEvents ??= [];
        TotalXpGained = recentEvents.Sum(e => e.XpEarned);
        Gains = AggregateEvents(recentEvents);
    }

    private static List<AggregatedXpGain> AggregateEvents(IReadOnlyList<GamificationEvent> events)
    {
        var aggregated = new List<AggregatedXpGain>();

        var passedTestEvents = events
            .Where(e => e.EventType == GamificationEventType.PassedTest)
            .ToList();

        if (passedTestEvents.Count > 0)
        {
            int count = passedTestEvents.Count;
            string testWord = count == 1 ? "test" : "tests";
            aggregated.Add(new AggregatedXpGain(
                $"Made {count} {testWord} green",
                passedTestEvents.Sum(e => e.XpEarned),
                passedTestEvents.Max(e => e.CreatedAt)));
        }

        var overtakeEvents = events
            .Where(e => e.EventType == GamificationEventType.HallOfFameOvertake)
            .ToList();

        int overtakeXp = overtakeEvents.Sum(e => e.XpEarned);
        if (overtakeEvents.Count > 0 && overtakeXp > 0)
        {
            int count = overtakeEvents.Count;
            string studentWord = count == 1 ? "student" : "students";
            aggregated.Add(new AggregatedXpGain(
                $"Overtake bonus (surpassed {count} {studentWord} in Hall of Fame)",
                overtakeXp,
                overtakeEvents.Max(e => e.CreatedAt)));
        }

        foreach (var evt in events.Where(e =>
                     e.EventType != GamificationEventType.PassedTest &&
                     e.EventType != GamificationEventType.HallOfFameOvertake))
        {
            string desc = evt.EventType switch
            {
                GamificationEventType.AssignmentCompleted => "Completed assignment bonus",
                GamificationEventType.AssignmentCompletedQuickly => "Quick assignment completion bonus (Top 10)",
                GamificationEventType.ChapterCompleted => "Completed chapter bonus",
                GamificationEventType.TeamSynergy => "Team synergy bonus",
                GamificationEventType.HallOfFameTierUp =>
                    $"Hall of Fame level up bonus ({HallOfFameLeague.GetTierMetadata((HallOfFameTier)evt.TargetId).Name})",
                GamificationEventType.HallOfFameRankDefended => "Hall of Fame rank defense bonus",
                _ => "Gamification bonus"
            };

            aggregated.Add(new AggregatedXpGain(
                desc,
                evt.XpEarned,
                evt.CreatedAt));
        }

        return aggregated.OrderByDescending(g => g.CreatedAt).ToList();
    }
}
