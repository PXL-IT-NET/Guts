using System;
using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.GamificationAggregate.Profiles;

public class WeeklyStreak : ValueObject<WeeklyStreak>
{
    public int CurrentStreak { get; private set; }
    public int BestStreak { get; private set; }
    public int LastActiveYear { get; private set; }
    public int LastActiveWeek { get; private set; }

    public WeeklyStreak()
    {
        CurrentStreak = 0;
        BestStreak = 0;
        LastActiveYear = 0;
        LastActiveWeek = 0;
    }

    public WeeklyStreak(int currentStreak, int bestStreak, int lastActiveYear, int lastActiveWeek)
    {
        Contracts.Require(currentStreak >= 0, "CurrentStreak must be non-negative.");
        Contracts.Require(bestStreak >= currentStreak, "BestStreak cannot be less than CurrentStreak.");
        Contracts.Require(lastActiveYear >= 0, "LastActiveYear must be non-negative.");
        Contracts.Require(lastActiveWeek >= 0, "LastActiveWeek must be non-negative.");

        CurrentStreak = currentStreak;
        BestStreak = bestStreak;
        LastActiveYear = lastActiveYear;
        LastActiveWeek = lastActiveWeek;
    }

    public WeeklyStreak RecordActivity(DateTime timestamp)
    {
        var week = IsoWeek.From(timestamp);

        if (LastActiveYear == week.Year && LastActiveWeek == week.WeekNumber)
        {
            // Already active this week
            return this;
        }

        int weeksDiff = CalculateWeeksDifference(LastActiveYear, LastActiveWeek, week.Year, week.WeekNumber);

        int newCurrentStreak;
        if (LastActiveYear == 0 && LastActiveWeek == 0)
        {
            newCurrentStreak = 1;
        }
        else if (weeksDiff == 1)
        {
            newCurrentStreak = CurrentStreak + 1;
        }
        else if (weeksDiff <= 0)
        {
            return this;
        }
        else
        {
            newCurrentStreak = 1;
        }

        int newBestStreak = Math.Max(BestStreak, newCurrentStreak);
        return new WeeklyStreak(newCurrentStreak, newBestStreak, week.Year, week.WeekNumber);
    }

    public bool IsActiveInWeek(DateTime timestamp)
    {
        var week = IsoWeek.From(timestamp);
        return LastActiveYear == week.Year && LastActiveWeek == week.WeekNumber;
    }

    public bool NeedsEvaluation(DateTime timestamp)
    {
        if (CurrentStreak == 0 || LastActiveYear == 0)
        {
            return false;
        }

        var week = IsoWeek.From(timestamp);
        return CalculateWeeksDifference(LastActiveYear, LastActiveWeek, week.Year, week.WeekNumber) > 1;
    }

    public WeeklyStreak EvaluateStreak(DateTime timestamp, bool hasPassedAllAvailableTests)
    {
        if (!NeedsEvaluation(timestamp))
        {
            return this;
        }

        if (hasPassedAllAvailableTests)
        {
            var prevWeek = IsoWeek.From(timestamp).Previous();
            return new WeeklyStreak(CurrentStreak, BestStreak, prevWeek.Year, prevWeek.WeekNumber);
        }

        return new WeeklyStreak(0, BestStreak, LastActiveYear, LastActiveWeek);
    }

    public static int CalculateWeeksDifference(int fromYear, int fromWeek, int toYear, int toWeek)
    {
        if (fromYear == 0) return 1;
        return IsoWeek.From(fromYear, fromWeek).WeeksBetween(IsoWeek.From(toYear, toWeek));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CurrentStreak;
        yield return BestStreak;
        yield return LastActiveYear;
        yield return LastActiveWeek;
    }
}