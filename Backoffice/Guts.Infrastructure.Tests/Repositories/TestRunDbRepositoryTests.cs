using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class TestRunDbRepositoryTests : RepositoryTestsBase
{
    private int _courseId;
    private int _periodId;
    private int _projectId;
    private int _assignmentId;
    private int _teamId;
    private int _user1Id;
    private int _user2Id;
    private DateTime _t1;
    private DateTime _t2;

    [SetUp]
    public async Task SetUpSeedData()
    {
        _t1 = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        _t2 = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        _courseId = course.Id;
        _periodId = period.Id;
        var user1 = await Seeder.SeedUserAsync("Alice");
        var user2 = await Seeder.SeedUserAsync("Bob");

        _user1Id = user1.Id;
        _user2Id = user2.Id;

        var project = await Seeder.SeedProjectAsync(
            course.Id,
            period.Id,
            testNames: ["Test1"],
            teams: ("Team 01", [_user1Id, _user2Id]));

        _projectId = project.Id;
        var assignment = project.Assignments.First();
        _assignmentId = assignment.Id;
        var testId = assignment.Tests.First().Id;
        _teamId = project.Teams.First().Id;

        await Seeder.SeedTestRunAsync(_assignmentId, _user1Id, _t1, (testId, false));
        await Seeder.SeedTestRunAsync(_assignmentId, _user1Id, _t2, (testId, true));
        await Seeder.SeedTestRunAsync(_assignmentId, _user2Id, _t2, (testId, true));

        await using var context = CreateDbContext();
        context.SolutionFiles.Add(TestDataSeeder.CreateSolutionFile(_assignmentId, _user1Id, "A.cs", _t1, "code"));
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task GetUserTestRuns_GetLastTestRunOfAllUsers_And_GetTeamTestRuns_ShouldReturnMatchingRuns()
    {
        await using var context = CreateDbContext();
        var repo = new TestRunDbRepository(context);

        var userRuns = await repo.GetUserTestRunsForAssignmentAsync(_assignmentId, _user1Id, null);
        Assert.That(userRuns, Has.Count.EqualTo(2));

        var lastRuns = await repo.GetLastTestRunForAssignmentOfAllUsersAsync(_assignmentId);
        Assert.That(lastRuns, Has.Count.EqualTo(2));

        var teamRuns = await repo.GetTeamTestRunsForAssignmentAsync(_assignmentId, _teamId, null);
        Assert.That(teamRuns, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task RemoveAllTopicTestRunsOfUserAsync_ShouldRemoveTestRunsResultsAndSolutionFilesForUser()
    {
        await using (var context = CreateDbContext())
        {
            var repo = new TestRunDbRepository(context);
            await repo.RemoveAllTopicTestRunsOfUserAsync(_projectId, _user1Id);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TestRunDbRepository(context);
            var user1Runs = await repo.GetUserTestRunsForAssignmentAsync(_assignmentId, _user1Id, null);
            Assert.That(user1Runs, Is.Empty);
            Assert.That(context.SolutionFiles.Where(sf => sf.UserId == _user1Id).ToList(), Is.Empty);
        }
    }

    [Test]
    public async Task GetPruningAssignmentTargetsForPeriodAsync_ShouldFilterByPeriodDeletingFlagAndRunCount()
    {
        // Active period chapter with 2 runs -> INCLUDED (IsProject = false)
        var activeChapter = await Seeder.SeedChapterAsync(_courseId, _periodId, "Ch01", "Ch 1", "Ex01", "T1");
        int activeChapterAssignmentId = activeChapter.Assignments.First().Id;
        int activeChapterTestId = activeChapter.Assignments.First().Tests.First().Id;
        await Seeder.SeedTestRunAsync(activeChapterAssignmentId, _user1Id, _t1, (activeChapterTestId, false));
        await Seeder.SeedTestRunAsync(activeChapterAssignmentId, _user1Id, _t2, (activeChapterTestId, false));

        // Active period chapter with only 1 run -> EXCLUDED by SQL pre-filter
        var singleRunChapter = await Seeder.SeedChapterAsync(_courseId, _periodId, "Ch02", "Ch 2", "Ex02", "T2");
        int singleRunAssignmentId = singleRunChapter.Assignments.First().Id;
        int singleRunTestId = singleRunChapter.Assignments.First().Tests.First().Id;
        await Seeder.SeedTestRunAsync(singleRunAssignmentId, _user1Id, _t1, (singleRunTestId, true));

        // Past period chapter with 2 runs -> EXCLUDED (different period)
        var pastPeriod = await Seeder.SeedPeriodAsync(DateTime.Today.AddYears(-2), DateTime.Today.AddYears(-1), "Past");
        var pastChapter = await Seeder.SeedChapterAsync(_courseId, pastPeriod.Id, "ChOld", "Old", "ExOld", "TOld");
        int pastAssignmentId = pastChapter.Assignments.First().Id;
        int pastTestId = pastChapter.Assignments.First().Tests.First().Id;
        await Seeder.SeedTestRunAsync(pastAssignmentId, _user1Id, _t1, (pastTestId, false));
        await Seeder.SeedTestRunAsync(pastAssignmentId, _user1Id, _t2, (pastTestId, false));

        // Active period chapter in deleting course -> EXCLUDED
        var deletingCourse = await Seeder.SeedCourseAsync("DEL1", "Deleting Course");
        var deletingChapter = await Seeder.SeedChapterAsync(deletingCourse.Id, _periodId, "ChDel", "Del", "ExDel", "TDel");
        int deletingAssignmentId = deletingChapter.Assignments.First().Id;
        int deletingTestId = deletingChapter.Assignments.First().Tests.First().Id;
        await Seeder.SeedTestRunAsync(deletingAssignmentId, _user1Id, _t1, (deletingTestId, false));
        await Seeder.SeedTestRunAsync(deletingAssignmentId, _user1Id, _t2, (deletingTestId, false));
        await using (var ctx = CreateDbContext())
        {
            var courseEntity = await ctx.Courses.FindAsync(deletingCourse.Id);
            courseEntity.MarkAsDeleting();
            await ctx.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new TestRunDbRepository(context);

        var targets = await repo.GetPruningAssignmentTargetsForPeriodAsync(_periodId);

        Assert.That(targets.Select(t => (t.AssignmentId, t.IsProject)), Is.EquivalentTo(new[]
        {
            (_assignmentId, true),
            (activeChapterAssignmentId, false)
        }));
    }

    [Test]
    public async Task PruningRepositoryMethods_ShouldReadSnapshotsTeamMapAndDeleteRedundantRunsPreservingHashesAndSolutionFiles()
    {
        // Register a teacher TestCodeHash on the seeded assignment to prove it is never touched
        await using (var seedCtx = CreateDbContext())
        {
            seedCtx.Set<TestCodeHash>().Add(new TestCodeHash
            {
                AssignmentId = _assignmentId,
                Hash = "VALID-TEACHER-HASH-123"
            });
            await seedCtx.SaveChangesAsync();
        }

        // Seed a single-run chapter assignment to verify single-run snapshot early-return
        var singleChapter = await Seeder.SeedChapterAsync(_courseId, _periodId, "ChSingle", "Single", "ExS", "TS");
        int singleAssignmentId = singleChapter.Assignments.First().Id;
        int singleTestId = singleChapter.Assignments.First().Tests.First().Id;
        await Seeder.SeedTestRunAsync(singleAssignmentId, _user1Id, _t1, (singleTestId, true));

        await using (var context = CreateDbContext())
        {
            var repo = new TestRunDbRepository(context);

            var singleSnapshots = await repo.GetTestRunSnapshotsForAssignmentAsync(singleAssignmentId);
            Assert.That(singleSnapshots, Has.Count.EqualTo(1));

            var snapshots = await repo.GetTestRunSnapshotsForAssignmentAsync(_assignmentId);
            Assert.That(snapshots, Has.Count.EqualTo(3));

            var teamMap = await repo.GetProjectTeamMembershipMapAsync(_projectId);
            Assert.That(teamMap[_user1Id], Is.EqualTo(_teamId));
            Assert.That(teamMap[_user2Id], Is.EqualTo(_teamId));

            // Empty batch deletion returns (0, 0)
            var emptyDelete = await repo.DeleteTestRunsBatchAsync([]);
            Assert.That(emptyDelete.TestRunsDeleted, Is.EqualTo(0));
            Assert.That(emptyDelete.TestResultsDeleted, Is.EqualTo(0));

            // Seeded project team runs: user1 @ t1 (false), user1 @ t2 (true), user2 @ t2 (true) -> user2 @ t2 is redundant!
            var redundantIds = TestRunPruningPolicy.FindRedundantTestRunIds(snapshots, teamMap);
            Assert.That(redundantIds, Has.Count.EqualTo(1));

            var deleteResult = await repo.DeleteTestRunsBatchAsync(redundantIds);
            Assert.That(deleteResult.TestRunsDeleted, Is.EqualTo(1));
            Assert.That(deleteResult.TestResultsDeleted, Is.EqualTo(1));
        }

        await using (var verifyCtx = CreateDbContext())
        {
            Assert.That(await verifyCtx.TestRuns.CountAsync(r => r.AssignmentId == _assignmentId), Is.EqualTo(2));
            Assert.That(await verifyCtx.TestResults.CountAsync(tr => tr.TestRun.AssignmentId == _assignmentId), Is.EqualTo(2));
            Assert.That(await verifyCtx.Set<TestCodeHash>().CountAsync(h => h.AssignmentId == _assignmentId && h.Hash == "VALID-TEACHER-HASH-123"), Is.EqualTo(1));
            Assert.That(await verifyCtx.SolutionFiles.CountAsync(sf => sf.AssignmentId == _assignmentId), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetUserAndTeamTestRunSnapshotsForAssignmentAsync_ShouldProjectLightweightSnapshotsAndRespectDateCutoff()
    {
        await using var context = CreateDbContext();
        var repo = new TestRunDbRepository(context);

        var userAtT1 = await repo.GetUserTestRunSnapshotsForAssignmentAsync(_assignmentId, _user1Id, _t1);
        Assert.That(userAtT1, Has.Count.EqualTo(1));
        Assert.That(userAtT1[0].Results, Has.Count.EqualTo(1));
        Assert.That(userAtT1[0].Results[0].Passed, Is.False);

        var userAll = await repo.GetUserTestRunSnapshotsForAssignmentAsync(_assignmentId, _user1Id, null);
        Assert.That(userAll, Has.Count.EqualTo(2));

        var userEmpty = await repo.GetUserTestRunSnapshotsForAssignmentAsync(_assignmentId, _user1Id, _t1.AddMinutes(-1));
        Assert.That(userEmpty, Is.Empty);

        var teamAtT1 = await repo.GetTeamTestRunSnapshotsForAssignmentAsync(_assignmentId, _teamId, _t1);
        Assert.That(teamAtT1, Has.Count.EqualTo(1));

        var teamAll = await repo.GetTeamTestRunSnapshotsForAssignmentAsync(_assignmentId, _teamId, null);
        Assert.That(teamAll, Has.Count.EqualTo(3));

        var teamEmpty = await repo.GetTeamTestRunSnapshotsForAssignmentAsync(_assignmentId, _teamId, _t1.AddMinutes(-1));
        Assert.That(teamEmpty, Is.Empty);
    }

    [Test]
    public async Task TeamQueries_WhenAssignmentBelongsToIndividualChapter_ShouldReturnEmptyAndNotLeakTeammateTestRuns()
    {
        var chapter = await Seeder.SeedChapterAsync(_courseId, _periodId, "ChSec", "Security Chapter", "ExSec", "TSec");
        int chapterAssignmentId = chapter.Assignments.First().Id;
        int chapterTestId = chapter.Assignments.First().Tests.First().Id;
        await Seeder.SeedTestRunAsync(chapterAssignmentId, _user2Id, _t1, (chapterTestId, true));

        await using var context = CreateDbContext();
        var repo = new TestRunDbRepository(context);

        var leakedRuns = await repo.GetTeamTestRunsForAssignmentAsync(chapterAssignmentId, _teamId, null);
        Assert.That(leakedRuns, Is.Empty);

        var leakedSnapshots = await repo.GetTeamTestRunSnapshotsForAssignmentAsync(chapterAssignmentId, _teamId, null);
        Assert.That(leakedSnapshots, Is.Empty);
    }
}

