using System;
using System.Globalization;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate
{
    [TestFixture]
    public class WeeklyStreakTests
    {
        [Test]
        public void DefaultConstructor_ShouldInitializeWithZeroStreak()
        {
            var streak = new WeeklyStreak();
            Assert.That(streak.CurrentStreak, Is.EqualTo(0));
            Assert.That(streak.BestStreak, Is.EqualTo(0));
            Assert.That(streak.LastActiveYear, Is.EqualTo(0));
            Assert.That(streak.LastActiveWeek, Is.EqualTo(0));
        }

        [Test]
        public void ParameterizedConstructor_ValidValues_ShouldSetProperties()
        {
            var streak = new WeeklyStreak(3, 5, 2026, 42);
            Assert.That(streak.CurrentStreak, Is.EqualTo(3));
            Assert.That(streak.BestStreak, Is.EqualTo(5));
            Assert.That(streak.LastActiveYear, Is.EqualTo(2026));
            Assert.That(streak.LastActiveWeek, Is.EqualTo(42));
        }

        [Test]
        public void ParameterizedConstructor_BestStreakLessThanCurrent_ShouldThrowContractException()
        {
            Assert.Throws<ContractException>(() => new WeeklyStreak(5, 3, 2026, 42));
        }

        [Test]
        public void RecordActivity_FirstTime_ShouldStartStreakAtOne()
        {
            var streak = new WeeklyStreak();
            var now = new DateTime(2026, 10, 21); // Wednesday
            var updated = streak.RecordActivity(now);

            Assert.That(updated.CurrentStreak, Is.EqualTo(1));
            Assert.That(updated.BestStreak, Is.EqualTo(1));
            Assert.That(updated.LastActiveYear, Is.EqualTo(ISOWeek.GetYear(now)));
            Assert.That(updated.LastActiveWeek, Is.EqualTo(ISOWeek.GetWeekOfYear(now)));
        }

        [Test]
        public void RecordActivity_SameWeek_ShouldNotIncrementStreak()
        {
            var now = new DateTime(2026, 10, 21); // Wednesday
            var streak = new WeeklyStreak().RecordActivity(now);
            var nextDay = now.AddDays(1); // Thursday of same week
            var updated = streak.RecordActivity(nextDay);

            Assert.That(updated.CurrentStreak, Is.EqualTo(1));
            Assert.That(updated.BestStreak, Is.EqualTo(1));
        }

        [Test]
        public void RecordActivity_ConsecutiveWeek_ShouldIncrementStreak()
        {
            var week1 = new DateTime(2026, 10, 19); // Monday Week 43
            var week2 = week1.AddDays(7); // Monday Week 44

            var streak = new WeeklyStreak().RecordActivity(week1);
            var updated = streak.RecordActivity(week2);

            Assert.That(updated.CurrentStreak, Is.EqualTo(2));
            Assert.That(updated.BestStreak, Is.EqualTo(2));
            Assert.That(updated.LastActiveWeek, Is.EqualTo(ISOWeek.GetWeekOfYear(week2)));
        }

        [Test]
        public void RecordActivity_SkippedWeek_ShouldResetStreakToOne()
        {
            var week1 = new DateTime(2026, 10, 19); // Monday Week 43
            var week3 = week1.AddDays(14); // Monday Week 45 (skipped Week 44)

            var streak = new WeeklyStreak().RecordActivity(week1);
            var updated = streak.RecordActivity(week3);

            Assert.That(updated.CurrentStreak, Is.EqualTo(1));
            Assert.That(updated.BestStreak, Is.EqualTo(1));
        }

        [Test]
        public void IsActiveInWeek_MatchingWeek_ShouldReturnTrue()
        {
            var now = new DateTime(2026, 10, 21);
            var streak = new WeeklyStreak().RecordActivity(now);

            Assert.That(streak.IsActiveInWeek(now), Is.True);
            Assert.That(streak.IsActiveInWeek(now.AddDays(10)), Is.False);
        }
    }
}

