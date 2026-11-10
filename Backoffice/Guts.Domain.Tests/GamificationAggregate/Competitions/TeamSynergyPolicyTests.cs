using System;
using System.Collections.Generic;
using Guts.Domain.GamificationAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate;

[TestFixture]
public class TeamSynergyPolicyTests
{
    private DateTime _testTime;

    [SetUp]
    public void SetUp()
    {
        _testTime = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    }

    [Test]
    public void IsSynergyAchieved_WhenAssignmentTestIdsNullOrEmpty_ShouldReturnFalse()
    {
        var profile = CreateProfileWithPassedTests(1, 101);

        Assert.That(TeamSynergyPolicy.IsSynergyAchieved(null, 1, new[] { profile }), Is.False);
        Assert.That(TeamSynergyPolicy.IsSynergyAchieved(new HashSet<int>(), 1, new[] { profile }), Is.False);
    }

    [Test]
    public void IsSynergyAchieved_WhenProfilesNullOrEmpty_ShouldReturnFalse()
    {
        var testIds = new HashSet<int> { 101 };

        Assert.That(TeamSynergyPolicy.IsSynergyAchieved(testIds, 1, null), Is.False);
        Assert.That(TeamSynergyPolicy.IsSynergyAchieved(testIds, 1, Array.Empty<StudentCourseProfile>()), Is.False);
        Assert.That(TeamSynergyPolicy.IsSynergyAchieved(testIds, 0, Array.Empty<StudentCourseProfile>()), Is.False);
    }

    [Test]
    public void IsSynergyAchieved_WhenProfileCountDoesNotMatchRequiredCount_ShouldReturnFalse()
    {
        var testIds = new HashSet<int> { 101, 102 };
        var p1 = CreateProfileWithPassedTests(1, 101, 102);

        // Required count is 2, but only 1 profile provided (e.g. member has no profile in DB yet)
        bool result = TeamSynergyPolicy.IsSynergyAchieved(testIds, 2, new[] { p1 });

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSynergyAchieved_WhenOneMemberHasNoPassingTestsForAssignment_ShouldReturnFalse()
    {
        var testIds = new HashSet<int> { 101, 102 };
        var p1 = CreateProfileWithPassedTests(1, 101, 102);
        // p2 passed a test, but from a different assignment (999)
        var p2 = CreateProfileWithPassedTests(2, 999);

        bool result = TeamSynergyPolicy.IsSynergyAchieved(testIds, 2, new[] { p1, p2 });

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSynergyAchieved_WhenAllMembersContributedButSubsetOfAssignmentTestsNotPassed_ShouldReturnFalse()
    {
        // Assignment has 3 tests: 101, 102, 103
        var testIds = new HashSet<int> { 101, 102, 103 };
        var p1 = CreateProfileWithPassedTests(1, 101);
        var p2 = CreateProfileWithPassedTests(2, 102);
        // Neither p1 nor p2 passed test 103

        bool result = TeamSynergyPolicy.IsSynergyAchieved(testIds, 2, new[] { p1, p2 });

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsSynergyAchieved_WhenAllMembersContributedAndAllAssignmentTestsCovered_ShouldReturnTrue()
    {
        var testIds = new HashSet<int> { 101, 102, 103 };
        var p1 = CreateProfileWithPassedTests(1, 101, 102);
        var p2 = CreateProfileWithPassedTests(2, 102, 103);

        bool result = TeamSynergyPolicy.IsSynergyAchieved(testIds, 2, new[] { p1, p2 });

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsSynergyAchieved_WhenMultipleTeamMembersOverlapAndHaveUnrelatedEvents_ShouldReturnTrue()
    {
        var testIds = new HashSet<int> { 101, 102, 103, 104 };
        var p1 = CreateProfileWithPassedTests(1, 101, 500); // 500 is other exercise
        var p2 = CreateProfileWithPassedTests(2, 102, 103);
        var p3 = CreateProfileWithPassedTests(3, 104);

        bool result = TeamSynergyPolicy.IsSynergyAchieved(testIds, 3, new[] { p1, p2, p3 });

        Assert.That(result, Is.True);
    }

    private StudentCourseProfile CreateProfileWithPassedTests(int userId, params int[] testIds)
    {
        var profile = new StudentCourseProfile(userId, 10, 1);
        foreach (int testId in testIds)
        {
            profile.AwardPassedTestXp(testId, _testTime);
        }
        return profile;
    }
}

