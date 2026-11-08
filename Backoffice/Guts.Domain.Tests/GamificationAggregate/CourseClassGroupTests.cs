using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate;

[TestFixture]
public class CourseClassGroupTests
{
    [Test]
    public void Constructor_ValidArguments_ShouldSetProperties()
    {
        var group = new CourseClassGroup("1TIN-A", 10, 20);
        Assert.That(group.Name, Is.EqualTo("1TIN-A"));
        Assert.That(group.CourseId, Is.EqualTo(10));
        Assert.That(group.PeriodId, Is.EqualTo(20));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Constructor_InvalidName_ShouldThrowContractException(string invalidName)
    {
        Assert.Throws<ContractException>(() => new CourseClassGroup(invalidName, 10, 20));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_InvalidCourseId_ShouldThrowContractException(int invalidCourseId)
    {
        Assert.Throws<ContractException>(() => new CourseClassGroup("1TIN-A", invalidCourseId, 20));
    }

    [Test]
    public void Name_Setter_Valid_ShouldTrimAndSet()
    {
        var group = new CourseClassGroup("1TIN-A", 10, 20);
        group.Name = "  2TIN-B  ";
        Assert.That(group.Name, Is.EqualTo("2TIN-B"));
    }

    [Test]
    public void DefaultConstructor_And_NavigationProperties_ShouldWork()
    {
        var group = new CourseClassGroup
        {
            Name = "1TIN-C",
            CourseId = 5,
            PeriodId = 6,
            Course = new Guts.Domain.CourseAggregate.Course { Id = 5 },
            Period = new Guts.Domain.Tests.Builders.PeriodBuilder().WithId().Build()
        };

        Assert.That(group.Name, Is.EqualTo("1TIN-C"));
        Assert.That(group.Course.Id, Is.EqualTo(5));
        Assert.That(group.Period, Is.Not.Null);
    }

    [Test]
    public void Factory_CreateNew_ShouldCreateCourseClassGroup()
    {
        var factory = new CourseClassGroup.Factory();
        var group = factory.CreateNew("1TIN-D", 10, 20);

        Assert.That(group, Is.Not.Null);
        Assert.That(group.Name, Is.EqualTo("1TIN-D"));
        Assert.That(group.CourseId, Is.EqualTo(10));
        Assert.That(group.PeriodId, Is.EqualTo(20));
    }

    [TestCase(1, 1.00)]
    [TestCase(5, 1.00)]
    [TestCase(6, 0.60)]
    [TestCase(10, 0.60)]
    [TestCase(11, 0.30)]
    [TestCase(15, 0.30)]
    [TestCase(16, 0.15)]
    [TestCase(25, 0.15)]
    public void GetContributionWeightForRank_ValidRank_ShouldReturnSteppedBracketWeight(int rank, double expectedWeight)
    {
        double weight = CourseClassGroup.GetContributionWeightForRank(rank);
        Assert.That(weight, Is.EqualTo(expectedWeight));
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void GetContributionWeightForRank_InvalidRank_ShouldThrowContractException(int invalidRank)
    {
        Assert.Throws<ContractException>(() => CourseClassGroup.GetContributionWeightForRank(invalidRank));
    }

    [TestCase(0, 1, 5)]
    [TestCase(100, 1, 105)]
    [TestCase(100, 6, 65)]
    [TestCase(100, 11, 35)]
    [TestCase(100, 16, 20)]
    [TestCase(10, 16, 7)] // 5 + Round(10 * 0.15 = 1.5 -> 2) = 7
    public void CalculateStudentContribution_ValidInputs_ShouldReturnJoinBonusPlusRoundedWeightedXp(
        int totalXp,
        int withinClassRank,
        int expectedContribution)
    {
        int contribution = CourseClassGroup.CalculateStudentContribution(totalXp, withinClassRank);
        Assert.That(contribution, Is.EqualTo(expectedContribution));
    }

    [Test]
    public void CalculateStudentContribution_InvalidInputs_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => CourseClassGroup.CalculateStudentContribution(-1, 1));
        Assert.Throws<ContractException>(() => CourseClassGroup.CalculateStudentContribution(10, 0));
    }

    [Test]
    public void CalculateClassScore_EmptyProfiles_ShouldReturnZeroAndNullUserContribution()
    {
        var result = CourseClassGroup.CalculateClassScore(Array.Empty<StudentCourseProfile>(), currentUserId: 10);

        Assert.That(result.ClassScore, Is.EqualTo(0));
        Assert.That(result.CurrentUserContribution, Is.Null);
    }

    [Test]
    public void CalculateClassScore_NullProfiles_ShouldThrowContractException()
    {
        Assert.Throws<ContractException>(() => CourseClassGroup.CalculateClassScore(null));
    }

    [Test]
    public void CalculateClassScore_ShouldApplySteppedBracketsAndNeverDecreaseWhenLowOrZeroXpStudentJoins()
    {
        var now = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

        var profiles = new List<StudentCourseProfile>();
        // Create 5 top students with 100 XP each -> each contributes 5 + 100 = 105 (total 525)
        for (int i = 1; i <= 5; i++)
        {
            var p = new StudentCourseProfile(i, 1, 1) { Id = i };
            p.Enroll(true, $"User{i}", "CodeCat", 1, false);
            for (int t = 1; t <= 10; t++)
            {
                p.AwardPassedTestXp(t, now.AddMinutes(i), false);
            }
            profiles.Add(p);
        }

        var initialScore = CourseClassGroup.CalculateClassScore(profiles);
        Assert.That(initialScore.ClassScore, Is.EqualTo(525));

        // Student #6 joins with 0 XP -> score must increase by +5 join bonus to 530
        var zeroXpStudent = new StudentCourseProfile(6, 1, 1) { Id = 6 };
        zeroXpStudent.Enroll(false, "ShyStudent", "CodeCat", 1, false); // Even with ShowInLeaderBoard = false
        profiles.Add(zeroXpStudent);

        var afterZeroXpJoin = CourseClassGroup.CalculateClassScore(profiles, currentUserId: 6);
        Assert.That(afterZeroXpJoin.ClassScore, Is.EqualTo(530));
        Assert.That(afterZeroXpJoin.CurrentUserContribution, Is.EqualTo(5));

        // Student #6 passes 2 tests (20 XP) at rank 6 (60% bracket) -> contributes 5 + 12 = 17, total 542
        zeroXpStudent.AwardPassedTestXp(1, now.AddHours(1), false);
        zeroXpStudent.AwardPassedTestXp(2, now.AddHours(1), false);

        var afterTests = CourseClassGroup.CalculateClassScore(profiles, currentUserId: 6);
        Assert.That(afterTests.ClassScore, Is.EqualTo(542));
        Assert.That(afterTests.CurrentUserContribution, Is.EqualTo(17));
    }

    [Test]
    public void CalculateClassScore_AcrossAllFourBrackets_ShouldWeightBySortedXpRank()
    {
        var now = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
        var profiles = new List<StudentCourseProfile>();

        // 16 students:
        // Ranks 1-5: 100 XP (5 * (5 + 100) = 525)
        // Ranks 6-10: 50 XP (5 * (5 + 30) = 175)
        // Ranks 11-15: 20 XP (5 * (5 + 6) = 55)
        // Rank 16: 10 XP (1 * (5 + 2) = 7)
        // Total = 525 + 175 + 55 + 7 = 762
        int[] testCounts = [10, 10, 10, 10, 10, 5, 5, 5, 5, 5, 2, 2, 2, 2, 2, 1];
        for (int i = 0; i < testCounts.Length; i++)
        {
            int userId = i + 1;
            var p = new StudentCourseProfile(userId, 1, 1) { Id = userId };
            p.Enroll(true, $"User{userId}", "CodeCat", 1, false);
            for (int t = 1; t <= testCounts[i]; t++)
            {
                p.AwardPassedTestXp(t, now.AddMinutes(userId), false);
            }
            profiles.Add(p);
        }

        // Pass in reverse order to verify CalculateClassScore sorts by TotalXp descending
        var result = CourseClassGroup.CalculateClassScore(profiles.AsEnumerable().Reverse(), currentUserId: 16);
        Assert.That(result.ClassScore, Is.EqualTo(762));
        Assert.That(result.CurrentUserContribution, Is.EqualTo(7));
    }
}