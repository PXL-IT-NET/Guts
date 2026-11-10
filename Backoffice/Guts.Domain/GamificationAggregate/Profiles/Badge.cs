using System.Collections.Generic;
using System.Linq;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate.Profiles;

public class Badge : ValueObject<Badge>
{
    public int Value { get; }
    public string Name { get; }
    public string Description { get; }
    public string Icon { get; }

    public static readonly Badge FirstSpark = new(1, "First Spark", "Passed your first test in the course", "🌱");
    public static readonly Badge SpeedDemon = new(2, "Speed Demon", "Completed all tests of an assignment within 24 hours", "⚡");
    public static readonly Badge CleanSweep = new(4, "Clean Sweep", "Solved 100% of all exercises in a chapter", "🧹");
    public static readonly Badge NightOwl = new(5, "Night Owl", "Submitted a passing test between 00:00 and 05:00", "🦉");
    public static readonly Badge EarlyBird = new(6, "Early Bird", "Submitted a passing test between 05:00 and 08:00", "🌅");
    public static readonly Badge WeekendWarrior = new(7, "Weekend Warrior", "Passed a test during the weekend", "⚔️");
    public static readonly Badge CenturyClub = new(8, "Century Club", "Achieved 100 unique passing tests in the course", "💯");
    public static readonly Badge OnFire = new(9, "On Fire", "Maintained a 4-week coding streak", "🔥");
    public static readonly Badge IronWill = new(10, "Iron Will", "Maintained an 8-week coding streak", "🛡️");
    public static readonly Badge SprintGold = new(11, "Sprint Gold", "Finished #1 on the Weekly Sprint Leaderboard", "🥇");
    public static readonly Badge TeamPowerhouse = new(12, "Team Powerhouse", "Project team where all members have green tests", "🤝");
    public static readonly Badge SprintSilver = new(13, "Sprint Silver", "Finished in the top 2 of the Weekly Sprint Leaderboard", "🥈");
    public static readonly Badge SprintBronze = new(14, "Sprint Bronze", "Finished in the top 3 of the Weekly Sprint Leaderboard", "🥉");
    public static readonly Badge SprintTop4 = new(15, "Sprint Top 4", "Finished in the top 4 of the Weekly Sprint Leaderboard", "🎖️");
    public static readonly Badge SprintTop5 = new(16, "Sprint Top 5", "Finished in the top 5 of the Weekly Sprint Leaderboard", "⭐");
    public static readonly Badge WarmingUp = new(17, "Warming Up", "Maintained a 2-week coding streak", "🕯️");
    public static readonly Badge Unstoppable = new(18, "Unstoppable", "Maintained a 6-week coding streak", "☄️");
    public static readonly Badge QuarterCentury = new(19, "Quarter Century", "Achieved 25 unique passing tests in the course", "🎯");
    public static readonly Badge HalfCentury = new(20, "Half Century", "Achieved 50 unique passing tests in the course", "🏅");
    public static readonly Badge FirstVictory = new(21, "First Victory", "Completed all tests of your first assignment", "🏁");
    public static readonly Badge HighFive = new(22, "High Five", "Completed all tests of 5 assignments in the course", "🖐️");
    public static readonly Badge RankDefender = new(23, "Rank Defender", "Defended your Hall of Fame rank without being overtaken for 3 weeks", "🏰");

    public static readonly IReadOnlyList<Badge> All = new[]
    {
        FirstSpark, SpeedDemon, CleanSweep, NightOwl, EarlyBird, WeekendWarrior,
        CenturyClub, OnFire, IronWill, SprintGold, TeamPowerhouse, SprintSilver,
        SprintBronze, SprintTop4, SprintTop5, WarmingUp, Unstoppable, QuarterCentury,
        HalfCentury, FirstVictory, HighFive, RankDefender
    };

    private static readonly Dictionary<int, Badge> AllByValue = All.ToDictionary(b => b.Value);

    public Badge(int value, string name, string description, string icon)
    {
        Contracts.Require(value > 0, "Badge Value must be positive.");
        Contracts.Require(!string.IsNullOrWhiteSpace(name), "Badge Name cannot be empty.");
        Contracts.Require(!string.IsNullOrWhiteSpace(icon), "Badge Icon cannot be empty.");

        Value = value;
        Name = name;
        Description = description ?? string.Empty;
        Icon = icon;
    }

    public static Badge FromValue(int value)
    {
        if (AllByValue.TryGetValue(value, out var badge))
        {
            return badge;
        }

        return new Badge(value, value.ToString(), string.Empty, "🏅");
    }

    public static implicit operator int(Badge badge) => badge?.Value ?? 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Name;
}
