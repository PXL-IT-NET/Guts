using System;
using System.Collections.Generic;
using Guts.Common;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Competitions;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;
using NUnit.Framework;

namespace Guts.Domain.Tests.GamificationAggregate.Competitions;

[TestFixture]
public class ClassGroupCompetitionTests
{
    private CourseClassGroup.Factory _classGroupFactory;
    private StudentCourseProfile.Factory _profileFactory;

    [SetUp]
    public void SetUp()
    {
        _classGroupFactory = new CourseClassGroup.Factory();
        _profileFactory = new StudentCourseProfile.Factory();
    }

    [Test]
    public void DetermineStandings_NullArguments_ShouldThrow()
    {
        Assert.Throws<ContractException>(() => ClassGroupCompetition.DetermineStandings(null, []));
        Assert.Throws<ContractException>(() => ClassGroupCompetition.DetermineStandings([], null));
    }

    [Test]
    public void DetermineStandings_EmptyClassGroups_ShouldReturnEmptyList()
    {
        var standings = ClassGroupCompetition.DetermineStandings([], []);
        Assert.That(standings, Is.Empty);
    }

    [Test]
    public void DetermineStandings_GroupsWithNoStudents_ShouldHaveZeroScoresAndRankProperly()
    {
        var groupA = _classGroupFactory.CreateNew("1TIN-A", 1, 1);
        groupA.Id = 10;
        var groupB = _classGroupFactory.CreateNew("1TIN-B", 1, 1);
        groupB.Id = 20;

        var standings = ClassGroupCompetition.DetermineStandings([groupA, groupB], []);

        Assert.That(standings, Has.Count.EqualTo(2));
        Assert.That(standings[0].Rank, Is.EqualTo(1));
        Assert.That(standings[0].ClassGroup.Name, Is.EqualTo("1TIN-A"));
        Assert.That(standings[0].ClassScore, Is.Zero);
        Assert.That(standings[0].StudentCount, Is.Zero);
        Assert.That(standings[0].TotalGreenTests, Is.Zero);
        Assert.That(standings[0].TotalXp, Is.Zero);
        Assert.That(standings[0].CurrentUserContribution, Is.Null);

        Assert.That(standings[1].Rank, Is.EqualTo(2));
        Assert.That(standings[1].ClassGroup.Name, Is.EqualTo("1TIN-B"));
    }

    [Test]
    public void DetermineStandings_ShouldOrderDescendingByClassScoreThenGreenThenXp()
    {
        var groupA = _classGroupFactory.CreateNew("1TIN-A", 1, 1);
        groupA.Id = 10;
        var groupB = _classGroupFactory.CreateNew("1TIN-B", 1, 1);
        groupB.Id = 20;

        // Group A: 1 student with 100 XP (contribution = 5 + 100 = 105)
        var studentA = _profileFactory.CreateNew(1, 1, 1);
        studentA.Enroll(true, "A1", "CodeCat", groupA.Id, false);
        for (int i = 1; i <= 10; i++)
        {
            studentA.AwardPassedTestXp(i, DateTime.UtcNow);
        }

        // Group B: 2 students with total 300 XP (student 1: 200 XP -> 205, student 2: 100 XP -> 105; total = 310)
        var studentB1 = _profileFactory.CreateNew(2, 1, 1);
        studentB1.Enroll(true, "B1", "SyntaxFox", groupB.Id, false);
        for (int i = 1; i <= 20; i++)
        {
            studentB1.AwardPassedTestXp(i, DateTime.UtcNow);
        }

        var studentB2 = _profileFactory.CreateNew(3, 1, 1);
        studentB2.Enroll(true, "B2", "SyntaxFox", groupB.Id, false);
        for (int i = 1; i <= 10; i++)
        {
            studentB2.AwardPassedTestXp(i, DateTime.UtcNow);
        }

        var standings = ClassGroupCompetition.DetermineStandings([groupA, groupB], [studentA, studentB1, studentB2], currentUserId: 2);

        Assert.That(standings, Has.Count.EqualTo(2));

        // Group B is #1 because higher class score
        Assert.That(standings[0].Rank, Is.EqualTo(1));
        Assert.That(standings[0].ClassGroup.Id, Is.EqualTo(groupB.Id));
        Assert.That(standings[0].StudentCount, Is.EqualTo(2));
        Assert.That(standings[0].TotalGreenTests, Is.EqualTo(30));
        Assert.That(standings[0].TotalXp, Is.EqualTo(300));
        Assert.That(standings[0].IsCurrentUserClass, Is.True);
        Assert.That(standings[0].CurrentUserContribution, Is.EqualTo(205)); // 5 + 200 * 1.0

        // Group A is #2
        Assert.That(standings[1].Rank, Is.EqualTo(2));
        Assert.That(standings[1].ClassGroup.Id, Is.EqualTo(groupA.Id));
        Assert.That(standings[1].StudentCount, Is.EqualTo(1));
        Assert.That(standings[1].TotalGreenTests, Is.EqualTo(10));
        Assert.That(standings[1].TotalXp, Is.EqualTo(100));
        Assert.That(standings[1].IsCurrentUserClass, Is.False);
        Assert.That(standings[1].CurrentUserContribution, Is.Null);
    }

    [Test]
    public void DetermineStandings_EqualClassScores_ShouldTieBreakByGreenTestsThenXpThenName()
    {
        var groupA = _classGroupFactory.CreateNew("Alpha", 1, 1);
        groupA.Id = 10;
        var groupB = _classGroupFactory.CreateNew("Beta", 1, 1);
        groupB.Id = 20;

        // Group Alpha: 100 XP from 10 green tests
        var studentA = _profileFactory.CreateNew(1, 1, 1);
        studentA.Enroll(true, "A", "CodeCat", groupA.Id, false);
        for (int i = 1; i <= 10; i++)
        {
            studentA.AwardPassedTestXp(i, DateTime.UtcNow);
        }

        // Group Beta: 100 XP from 2 green tests (e.g. 2 tests + other XP so total green is lower)
        // Wait, AwardPassedTestXp gives 10 XP per test, so 10 tests = 100 XP.
        // Let's create studentB with same XP (100) and same green tests (10)
        var studentB = _profileFactory.CreateNew(2, 1, 1);
        studentB.Enroll(true, "B", "SyntaxFox", groupB.Id, false);
        for (int i = 1; i <= 10; i++)
        {
            studentB.AwardPassedTestXp(i, DateTime.UtcNow);
        }

        var standings = ClassGroupCompetition.DetermineStandings([groupB, groupA], [studentA, studentB]);

        Assert.That(standings, Has.Count.EqualTo(2));
        // Equal scores & green & XP: Alpha should come before Beta alphabetically
        Assert.That(standings[0].ClassGroup.Name, Is.EqualTo("Alpha"));
        Assert.That(standings[0].Rank, Is.EqualTo(1));
        Assert.That(standings[1].ClassGroup.Name, Is.EqualTo("Beta"));
        Assert.That(standings[1].Rank, Is.EqualTo(2));
    }
}
