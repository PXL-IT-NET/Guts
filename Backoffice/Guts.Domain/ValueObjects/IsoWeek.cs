using System;
using System.Collections.Generic;
using System.Globalization;
using Guts.Common;

namespace Guts.Domain.ValueObjects;

public sealed class IsoWeek : ValueObject<IsoWeek>, IComparable<IsoWeek>, IEquatable<IsoWeek>
{
    public int Year { get; }
    public int WeekNumber { get; }

    public DateTime StartOfWeek { get; }
    public DateTime FridayCutoff { get; }
    public DateTime EndOfWeek { get; }

    private IsoWeek(int year, int weekNumber)
    {
        Contracts.Require(year > 0, "Year must be positive.");
        int weeksInYear = ISOWeek.GetWeeksInYear(year);
        Contracts.Require(weekNumber >= 1 && weekNumber <= weeksInYear,
            $"Week number must be between 1 and {weeksInYear} for year {year}.");

        Year = year;
        WeekNumber = weekNumber;

        DateTime monday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday);
        StartOfWeek = DateTime.SpecifyKind(monday, DateTimeKind.Utc);

        DateTime friday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Friday);
        FridayCutoff = new DateTime(friday.Year, friday.Month, friday.Day, 23, 59, 59, DateTimeKind.Utc);

        DateTime sunday = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Sunday);
        EndOfWeek = new DateTime(sunday.Year, sunday.Month, sunday.Day, 23, 59, 59, DateTimeKind.Utc);
    }

    public static IsoWeek From(int year, int weekNumber) => new(year, weekNumber);

    public static IsoWeek From(DateTime dateTime)
    {
        int year = ISOWeek.GetYear(dateTime);
        int week = ISOWeek.GetWeekOfYear(dateTime);
        return new IsoWeek(year, week);
    }

    public static IsoWeek Current() => From(DateTime.UtcNow);

    public IsoWeek Previous() => From(StartOfWeek.AddDays(-7));

    public IsoWeek Next() => From(StartOfWeek.AddDays(7));

    public int WeeksBetween(IsoWeek other)
    {
        Contracts.Require(other != null, "Other IsoWeek cannot be null.");
        TimeSpan diff = other.StartOfWeek - StartOfWeek;
        return (int)Math.Round(diff.TotalDays / 7.0);
    }

    public bool Contains(DateTime timestamp)
    {
        DateTime utc = timestamp.Kind == DateTimeKind.Utc ? timestamp : timestamp.ToUniversalTime();
        return utc >= StartOfWeek && utc <= EndOfWeek;
    }

    public int CompareTo(IsoWeek other)
    {
        if (other is null) return 1;
        int yearCompare = Year.CompareTo(other.Year);
        return yearCompare != 0 ? yearCompare : WeekNumber.CompareTo(other.WeekNumber);
    }

    public bool Equals(IsoWeek other) => base.Equals(other);

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(IsoWeek left, IsoWeek right) => Equals(left, right);

    public static bool operator !=(IsoWeek left, IsoWeek right) => !Equals(left, right);

    public static bool operator <(IsoWeek left, IsoWeek right) => left is not null && left.CompareTo(right) < 0;
    public static bool operator >(IsoWeek left, IsoWeek right) => left is not null && left.CompareTo(right) > 0;
    public static bool operator <=(IsoWeek left, IsoWeek right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >=(IsoWeek left, IsoWeek right) => left is null ? right is null : left.CompareTo(right) >= 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Year;
        yield return WeekNumber;
    }

    public override string ToString() => $"W{WeekNumber:D2}-{Year}";
}
