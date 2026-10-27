using System;
using System.Collections.Generic;
using System.Globalization;
using Guts.Common;

namespace Guts.Domain.GamificationAggregate;

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
        int year = ISOWeek.GetYear(timestamp);
        int week = ISOWeek.GetWeekOfYear(timestamp);

        if (LastActiveYear == year && LastActiveWeek == week)
        {
            // Already active this week
            return this;
        }

        int weeksDiff = CalculateWeeksDifference(LastActiveYear, LastActiveWeek, year, week);

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
        return new WeeklyStreak(newCurrentStreak, newBestStreak, year, week);
    }

    public bool IsActiveInWeek(DateTime timestamp)
    {
        int year = ISOWeek.GetYear(timestamp);
        int week = ISOWeek.GetWeekOfYear(timestamp);
        return LastActiveYear == year && LastActiveWeek == week;
    }

    public bool NeedsEvaluation(DateTime timestamp)
    {
        if (CurrentStreak == 0 || LastActiveYear == 0)
        {
            return false;
        }

        int year = ISOWeek.GetYear(timestamp);
        int week = ISOWeek.GetWeekOfYear(timestamp);
        return CalculateWeeksDifference(LastActiveYear, LastActiveWeek, year, week) > 1;
    }

    public WeeklyStreak EvaluateStreak(DateTime timestamp, bool hasPassedAllAvailableTests)
    {
        if (!NeedsEvaluation(timestamp))
        {
            return this;
        }

        if (hasPassedAllAvailableTests)
        {
            int currentYear = ISOWeek.GetYear(timestamp);
            int currentWeek = ISOWeek.GetWeekOfYear(timestamp);
            DateTime previousWeekMonday = ISOWeek.ToDateTime(currentYear, currentWeek, DayOfWeek.Monday).AddDays(-7);
            int prevYear = ISOWeek.GetYear(previousWeekMonday);
            int prevWeek = ISOWeek.GetWeekOfYear(previousWeekMonday);

            return new WeeklyStreak(CurrentStreak, BestStreak, prevYear, prevWeek);
        }

        return new WeeklyStreak(0, BestStreak, LastActiveYear, LastActiveWeek);
    }

    public static int CalculateWeeksDifference(int fromYear, int fromWeek, int toYear, int toWeek)
    {
        if (fromYear == 0) return 1;
        DateTime fromDate = ISOWeek.ToDateTime(fromYear, fromWeek, DayOfWeek.Monday);
        DateTime toDate = ISOWeek.ToDateTime(toYear, toWeek, DayOfWeek.Monday);
        TimeSpan diff = toDate - fromDate;
        return (int)Math.Round(diff.TotalDays / 7.0);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CurrentStreak;
        yield return BestStreak;
        yield return LastActiveYear;
        yield return LastActiveWeek;
    }
}