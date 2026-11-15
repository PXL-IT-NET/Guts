using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.ValueObjects;
using Guts.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ChunkedDeletionDbRepositoryTests : RepositoryTestsBase
{
    private sealed record SeededGraph(
        int TargetCourseId,
        int KeepCourseId,
        int TargetPeriodId,
        int KeepPeriodId);

    private async Task<SeededGraph> SeedRichCourseAndPeriodAsync()
    {
        var (targetCourse, keepPeriod) = await Seeder.SeedCourseAndActivePeriodAsync("DEL1");
        var keepCourse = await Seeder.SeedCourseAsync("KEEP1", "Keep Course");
        var pastPeriod = await Seeder.SeedPeriodAsync(
            DateTime.Today.AddYears(-2),
            DateTime.Today.AddYears(-1),
            "Past 2020");

        var user1 = await Seeder.SeedUserAsync("Alice", "One", "alice@pxl.be");
        var user2 = await Seeder.SeedUserAsync("Bob", "Two", "bob@pxl.be");

        // Target data: (targetCourse, pastPeriod) + a kept slice (keepCourse, keepPeriod)
        var targetChapter = await Seeder.SeedChapterAsync(targetCourse.Id, pastPeriod.Id, "Ch1", "Target Ch", "Ex1", "T1", "T2", "T3");
        var keepChapter = await Seeder.SeedChapterAsync(keepCourse.Id, keepPeriod.Id, "ChK", "Keep Ch", "ExK", "TK1");

        var targetProject = await Seeder.SeedProjectAsync(
            targetCourse.Id, pastPeriod.Id, "Pr1", "Target Proj", "Cmp1", ["PT1", "PT2"], ("Team A", [user1.Id, user2.Id]));
        var keepProject = await Seeder.SeedProjectAsync(
            keepCourse.Id, keepPeriod.Id, "PrK", "Keep Proj", "CmpK", ["PK1"], ("Team K", [user1.Id]));

        var targetPa = await Seeder.SeedProjectAssessmentAsync(targetProject.Id, "Sprint 1");
        var keepPa = await Seeder.SeedProjectAssessmentAsync(keepProject.Id, "Keep Sprint");
        await Seeder.SeedProjectTeamAssessmentAsync(targetPa.Id, targetProject.Teams.First().Id, (user1.Id, user2.Id));
        await Seeder.SeedProjectTeamAssessmentAsync(keepPa.Id, keepProject.Teams.First().Id, (user1.Id, user1.Id));

        await using (var context = CreateDbContext())
        {
            // Seed multiple TestRuns, TestResults, and SolutionFiles to force multi-chunk deletion with batchSize=2
            var targetAssignment = await context.Assignments
                .Include(a => a.Tests)
                .FirstAsync(a => a.TopicId == targetChapter.Id);
            var keepAssignment = await context.Assignments
                .Include(a => a.Tests)
                .FirstAsync(a => a.TopicId == keepChapter.Id);

            for (int i = 0; i < 3; i++)
            {
                var run = new TestRun
                {
                    AssignmentId = targetAssignment.Id,
                    UserId = user1.Id,
                    CreateDateTime = DateTime.UtcNow.AddMinutes(-i),
                    TestResults = targetAssignment.Tests.Select(t => new TestResult
                    {
                        TestId = t.Id,
                        UserId = user1.Id,
                        Passed = true,
                        CreateDateTime = DateTime.UtcNow.AddMinutes(-i)
                    }).ToList()
                };
                context.TestRuns.Add(run);

                context.SolutionFiles.Add(SolutionFile.CreateNew(
                    targetAssignment.Id,
                    user1.Id,
                    new FilePath($"Program{i}.cs"),
                    "class Program {}"));
            }

            context.TestRuns.Add(new TestRun
            {
                AssignmentId = keepAssignment.Id,
                UserId = user1.Id,
                CreateDateTime = DateTime.UtcNow,
                TestResults = keepAssignment.Tests.Select(t => new TestResult
                {
                    TestId = t.Id,
                    UserId = user1.Id,
                    Passed = true,
                    CreateDateTime = DateTime.UtcNow
                }).ToList()
            });
            context.SolutionFiles.Add(SolutionFile.CreateNew(
                keepAssignment.Id,
                user1.Id,
                new FilePath("Keep.cs"),
                "class Keep {}"));

            var targetGroup = new CourseClassGroup("1TIN-A", targetCourse.Id, pastPeriod.Id);
            var keepGroup = new CourseClassGroup("1TIN-K", keepCourse.Id, keepPeriod.Id);
            context.CourseClassGroups.AddRange(targetGroup, keepGroup);
            context.CourseGamificationConfigs.AddRange(
                new CourseGamificationConfig(targetCourse.Id),
                new CourseGamificationConfig(keepCourse.Id));
            await context.SaveChangesAsync();

            var targetProfile = new StudentCourseProfile(user1.Id, targetCourse.Id, pastPeriod.Id);
            targetProfile.Enroll(true, "Hero", "Mage", targetGroup.Id, false);
            targetProfile.AwardPassedTestXp(1, DateTime.UtcNow);

            var keepProfile = new StudentCourseProfile(user1.Id, keepCourse.Id, keepPeriod.Id);
            keepProfile.Enroll(true, "KeepHero", "Knight", keepGroup.Id, false);
            keepProfile.AwardPassedTestXp(2, DateTime.UtcNow);

            context.StudentCourseProfiles.AddRange(targetProfile, keepProfile);
            await context.SaveChangesAsync();
        }

        return new SeededGraph(targetCourse.Id, keepCourse.Id, pastPeriod.Id, keepPeriod.Id);
    }

    [Test]
    public async Task ExecuteChunkedDeletionAsync_Course_DeletesAllLeafToRootInBatchesWithoutTouchingOtherCourses()
    {
        var graph = await SeedRichCourseAndPeriodAsync();
        var batchEvents = new List<(int Count, string Phase)>();

        await using (var context = CreateDbContext())
        {
            var repo = new ChunkedDeletionDbRepository(context);
            long total = await repo.CountDependentAndRootRowsAsync(DeletionTargetType.Course, graph.TargetCourseId);
            Assert.That(total, Is.GreaterThan(15));

            await repo.ExecuteChunkedDeletionAsync(
                DeletionTargetType.Course,
                graph.TargetCourseId,
                batchSize: 2,
                (count, phase) =>
                {
                    batchEvents.Add((count, phase));
                    return Task.CompletedTask;
                });

            Assert.That(batchEvents.Sum(e => e.Count), Is.EqualTo(total));
            Assert.That(batchEvents.Select(e => e.Phase).Distinct(), Does.Contain("Deleting test results"));
            Assert.That(batchEvents.Select(e => e.Phase).Distinct(), Does.Contain("Deleting solution files"));
            Assert.That(batchEvents[^1].Phase, Is.EqualTo("Removing course"));
        }

        await using (var verify = CreateDbContext())
        {
            Assert.That(await verify.Courses.AnyAsync(c => c.Id == graph.TargetCourseId), Is.False);
            Assert.That(await verify.Topics.AnyAsync(t => t.CourseId == graph.TargetCourseId), Is.False);
            Assert.That(await verify.StudentCourseProfiles.AnyAsync(sp => sp.CourseId == graph.TargetCourseId), Is.False);
            Assert.That(await verify.CourseClassGroups.AnyAsync(cg => cg.CourseId == graph.TargetCourseId), Is.False);
            Assert.That(await verify.CourseGamificationConfigs.AnyAsync(gc => gc.CourseId == graph.TargetCourseId), Is.False);

            // Untouched course remains completely intact
            Assert.That(await verify.Courses.AnyAsync(c => c.Id == graph.KeepCourseId), Is.True);
            Assert.That(await verify.Topics.AnyAsync(t => t.CourseId == graph.KeepCourseId), Is.True);
            Assert.That(await verify.TestResults.AnyAsync(), Is.True);
            Assert.That(await verify.SolutionFiles.AnyAsync(), Is.True);
            Assert.That(await verify.PeerAssessments.AnyAsync(), Is.True);
            Assert.That(await verify.StudentCourseProfiles.AnyAsync(sp => sp.CourseId == graph.KeepCourseId), Is.True);
            Assert.That(await verify.CourseGamificationConfigs.AnyAsync(gc => gc.CourseId == graph.KeepCourseId), Is.True);
        }
    }

    [Test]
    public async Task ExecuteChunkedDeletionAsync_Period_DeletesAllLeafToRootInBatchesWithoutTouchingOtherPeriods()
    {
        var graph = await SeedRichCourseAndPeriodAsync();
        var batchEvents = new List<(int Count, string Phase)>();

        await using (var context = CreateDbContext())
        {
            var repo = new ChunkedDeletionDbRepository(context);
            long total = await repo.CountDependentAndRootRowsAsync(DeletionTargetType.Period, graph.TargetPeriodId);
            Assert.That(total, Is.GreaterThan(15));

            await repo.ExecuteChunkedDeletionAsync(
                DeletionTargetType.Period,
                graph.TargetPeriodId,
                batchSize: 2,
                (count, phase) =>
                {
                    batchEvents.Add((count, phase));
                    return Task.CompletedTask;
                });

            Assert.That(batchEvents.Sum(e => e.Count), Is.EqualTo(total));
            Assert.That(batchEvents[^1].Phase, Is.EqualTo("Removing period"));
        }

        await using (var verify = CreateDbContext())
        {
            Assert.That(await verify.Periods.AnyAsync(p => p.Id == graph.TargetPeriodId), Is.False);
            Assert.That(await verify.Topics.AnyAsync(t => t.PeriodId == graph.TargetPeriodId), Is.False);
            Assert.That(await verify.StudentCourseProfiles.AnyAsync(sp => sp.PeriodId == graph.TargetPeriodId), Is.False);
            Assert.That(await verify.CourseClassGroups.AnyAsync(cg => cg.PeriodId == graph.TargetPeriodId), Is.False);

            // Both courses & active period remain intact
            Assert.That(await verify.Periods.AnyAsync(p => p.Id == graph.KeepPeriodId), Is.True);
            Assert.That(await verify.Courses.AnyAsync(c => c.Id == graph.TargetCourseId), Is.True);
            Assert.That(await verify.Topics.AnyAsync(t => t.PeriodId == graph.KeepPeriodId), Is.True);
        }
    }

    [Test]
    public async Task ExecuteChunkedDeletionAsync_EmptyCourseAndPeriod_CountsAndDeletesRootRow()
    {
        var emptyCourse = await Seeder.SeedCourseAsync("EMP1", "Empty Course");
        var emptyPeriod = await Seeder.SeedPeriodAsync(
            DateTime.Today.AddYears(-3),
            DateTime.Today.AddYears(-2),
            "Empty Past Period");

        await using (var context = CreateDbContext())
        {
            var repo = new ChunkedDeletionDbRepository(context);
            Assert.That(await repo.CountDependentAndRootRowsAsync(DeletionTargetType.Course, emptyCourse.Id), Is.EqualTo(1));
            Assert.That(await repo.CountDependentAndRootRowsAsync(DeletionTargetType.Period, emptyPeriod.Id), Is.EqualTo(1));

            await repo.ExecuteChunkedDeletionAsync(DeletionTargetType.Course, emptyCourse.Id, 100, null);
            await repo.ExecuteChunkedDeletionAsync(DeletionTargetType.Period, emptyPeriod.Id, 100, null);
        }

        await using (var verify = CreateDbContext())
        {
            Assert.That(await verify.Courses.AnyAsync(c => c.Id == emptyCourse.Id), Is.False);
            Assert.That(await verify.Periods.AnyAsync(p => p.Id == emptyPeriod.Id), Is.False);
        }
    }
}
