using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.TestAggregate;
using Guts.Infrastructure.Migrations;
using Guts.Infrastructure.Tests.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Migrations;

[TestFixture]
public class AddGamificationMigrationTests
{
    private class TestAddGamification : AddGamification
    {
        public void ApplyUp(MigrationBuilder builder) => Up(builder);
        public void ApplyDown(MigrationBuilder builder) => Down(builder);
    }

    private SqliteConnection _connection;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [TearDown]
    public void TearDown()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    private GutsContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GutsContext>()
            .UseSqlite(_connection)
            .Options;

        return new GutsContext(options);
    }

    [Test]
    public async Task Up_ShouldInsertStudentProfilesAndGamificationEventsCorrectly()
    {
        // 1. Setup base database schema
        using (var initialContext = CreateDbContext())
        {
            initialContext.Database.EnsureCreated();
        }

        var seeder = new TestDataSeeder(CreateDbContext);

        // 2. Seed test data before migration
        var (course, period) = await seeder.SeedCourseAndActivePeriodAsync("NET1");
        var emptyCourse = await seeder.SeedCourseAsync("EMPTY", "Empty Course");

        // Regular chapter with 2 tests
        var chapter = await seeder.SeedChapterAsync(
            course.Id,
            period.Id,
            code: "Ch01",
            description: "Chapter 1",
            assignmentCode: "Ex01",
            testNames: new[] { "Test_A", "Test_B" });

        var assignment = chapter.Assignments.First();
        var testA = assignment.Tests.First(t => t.TestName == "Test_A");
        var testB = assignment.Tests.First(t => t.TestName == "Test_B");

        // Century chapter with 100 tests
        var centuryTestNames = Enumerable.Range(1, 100).Select(i => $"CenturyTest_{i}").ToArray();
        var centuryChapter = await seeder.SeedChapterAsync(
            course.Id,
            period.Id,
            code: "Ch02",
            description: "Chapter 2",
            assignmentCode: "Ex02",
            testNames: centuryTestNames);
        var centuryAssignment = centuryChapter.Assignments.First();

        // Exam chapter with 1 test
        var examChapter = await seeder.SeedChapterAsync(
            course.Id,
            period.Id,
            code: "ExamCh",
            description: "Exam Chapter",
            assignmentCode: "ExamEx",
            testNames: new[] { "ExamTest_1" });
        var examAssignment = examChapter.Assignments.First();
        await seeder.SeedExamWithPartAndEvaluationAsync(course.Id, period.Id, examAssignment);

        // Users
        var studentNormal = await seeder.SeedUserAsync("Alice", "Normal", "alice@student.pxl.be");
        var studentCentury = await seeder.SeedUserAsync("Bob", "Century", "bob@student.pxl.be");
        var studentFailing = await seeder.SeedUserAsync("Charlie", "Failing", "charlie@student.pxl.be");
        var studentExam = await seeder.SeedUserAsync("Dave", "Exam", "dave@student.pxl.be");

        var t1 = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc);
        var t3 = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var t4 = new DateTime(2026, 1, 16, 11, 0, 0, DateTimeKind.Utc);

        // StudentNormal:
        // Run 1: Test A PASSED, Test B FAILED
        await seeder.SeedTestRunAsync(assignment.Id, studentNormal.Id, t1, (testA.Id, true), (testB.Id, false));
        // Run 2: Test A PASSED again (duplicate pass at later date), Test B PASSED (first pass)
        await seeder.SeedTestRunAsync(assignment.Id, studentNormal.Id, t2, (testA.Id, true), (testB.Id, true));

        // StudentCentury: 100 tests passed
        var centuryResults = centuryAssignment.Tests.Select(t => (t.Id, true)).ToArray();
        await seeder.SeedTestRunAsync(centuryAssignment.Id, studentCentury.Id, t3, centuryResults);

        // StudentFailing: only failed tests
        await seeder.SeedTestRunAsync(assignment.Id, studentFailing.Id, t4, (testA.Id, false), (testB.Id, false));

        // StudentExam: passed test on an exam assignment
        var examTest = examAssignment.Tests.First();
        await seeder.SeedTestRunAsync(examAssignment.Id, studentExam.Id, t4, (examTest.Id, true));

        // 3. Drop gamification tables to simulate pre-migration state
        using (var dropContext = CreateDbContext())
        {
            dropContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS StudentBadges;");
            dropContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS GamificationEvents;");
            dropContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS StudentCourseProfiles;");
            dropContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS CourseClassGroups;");
            dropContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS CourseGamificationConfigs;");
        }

        // 4. Run the AddGamification migration
        using (var migrateContext = CreateDbContext())
        {
            var builder = new MigrationBuilder(migrateContext.Database.ProviderName);
            var migration = new TestAddGamification();
            migration.ApplyUp(builder);

            var sqlGenerator = migrateContext.GetService<IMigrationsSqlGenerator>();
            var commands = sqlGenerator.Generate(builder.Operations);

            foreach (var command in commands)
            {
                migrateContext.Database.ExecuteSqlRaw(command.CommandText);
            }
        }

        // 5. Verify results after migration
        await using var verifyContext = CreateDbContext();

        // 5a. Verify StudentProfiles
        var profiles = await verifyContext.StudentCourseProfiles.ToListAsync();
        Assert.That(profiles.Count, Is.EqualTo(2), "Only students with non-exam passing tests should have a profile.");

        var aliceProfile = profiles.FirstOrDefault(p => p.UserId == studentNormal.Id);
        Assert.That(aliceProfile, Is.Not.Null);
        Assert.That(aliceProfile.CourseId, Is.EqualTo(course.Id));
        Assert.That(aliceProfile.PeriodId, Is.EqualTo(period.Id));
        Assert.That(aliceProfile.TotalXp, Is.EqualTo(20), "2 unique passing tests * 10 XP = 20 XP");
        Assert.That(aliceProfile.ShowInLeaderBoard, Is.True);
        Assert.That(aliceProfile.UsePseudonym, Is.True);
        Assert.That(aliceProfile.Pseudonym, Is.EqualTo($"Student_{studentNormal.Id}"));
        Assert.That(aliceProfile.Avatar, Is.EqualTo("CodeCat"));

        var bobProfile = profiles.FirstOrDefault(p => p.UserId == studentCentury.Id);
        Assert.That(bobProfile, Is.Not.Null);
        Assert.That(bobProfile.TotalXp, Is.EqualTo(1000), "100 unique passing tests * 10 XP = 1000 XP");

        // Assert no profiles for failing-only or exam-only students
        Assert.That(profiles.Any(p => p.UserId == studentFailing.Id), Is.False, "Failing-only student should not have a profile.");
        Assert.That(profiles.Any(p => p.UserId == studentExam.Id), Is.False, "Exam-only student should not have a profile.");

        // 5b. Verify GamificationEvents
        var aliceEvents = await verifyContext.GamificationEvents
            .Where(e => e.StudentCourseProfileId == aliceProfile.Id)
            .OrderBy(e => e.TargetId)
            .ToListAsync();

        Assert.That(aliceEvents.Count, Is.EqualTo(2), "Alice should have exactly 2 events for 2 unique passing tests.");
        Assert.That(aliceEvents.All(e => e.EventType == GamificationEventType.PassedTest), Is.True);
        Assert.That(aliceEvents.All(e => e.XpEarned == 10), Is.True);

        var eventForTestA = aliceEvents.FirstOrDefault(e => e.TargetId == testA.Id);
        Assert.That(eventForTestA, Is.Not.Null);
        Assert.That(eventForTestA.CreatedAt, Is.EqualTo(t1), "Event timestamp should be the earliest pass timestamp.");

        var eventForTestB = aliceEvents.FirstOrDefault(e => e.TargetId == testB.Id);
        Assert.That(eventForTestB, Is.Not.Null);
        Assert.That(eventForTestB.CreatedAt, Is.EqualTo(t2));

        var bobEventsCount = await verifyContext.GamificationEvents
            .CountAsync(e => e.StudentCourseProfileId == bobProfile.Id);
        Assert.That(bobEventsCount, Is.EqualTo(100), "Bob should have 100 gamification events.");

        // 5c. Verify StudentBadges
        var aliceBadges = await verifyContext.StudentBadges
            .Where(b => b.StudentCourseProfileId == aliceProfile.Id)
            .ToListAsync();
        Assert.That(aliceBadges.Count, Is.EqualTo(1));
        Assert.That(aliceBadges[0].BadgeType, Is.EqualTo(BadgeType.FirstSpark));
        Assert.That(aliceBadges[0].EarnedAt, Is.EqualTo(t1));

        var bobBadges = await verifyContext.StudentBadges
            .Where(b => b.StudentCourseProfileId == bobProfile.Id)
            .ToListAsync();
        Assert.That(bobBadges.Count, Is.EqualTo(2), "Bob should earn both FirstSpark and CenturyClub.");
        Assert.That(bobBadges.Select(b => b.BadgeType), Is.EquivalentTo(new[] { BadgeType.FirstSpark, BadgeType.CenturyClub }));

        // 5d. Verify CourseGamificationConfigs
        var configs = await verifyContext.CourseGamificationConfigs.ToListAsync();
        Assert.That(configs.Count, Is.EqualTo(2));
        Assert.That(configs.All(c => !c.IsGamificationEnabled), Is.True);
        Assert.That(configs.All(c => c.IsWeeklyStreakEnabled), Is.True);
        Assert.That(configs.All(c => c.IsLeaderboardEnabled), Is.True);
        Assert.That(configs.All(c => c.IsClassCompetitionEnabled), Is.True);
        Assert.That(configs.All(c => c.IsTeamSynergyEnabled), Is.True);
        Assert.That(configs.All(c => c.IsBadgesEnabled), Is.True);
    }

    [Test]
    public void Down_ShouldDropAllGamificationTablesSuccessfully()
    {
        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        using var testContext = CreateDbContext();
        var builder = new MigrationBuilder(testContext.Database.ProviderName);
        var migration = new TestAddGamification();
        migration.ApplyDown(builder);

        var sqlGenerator = testContext.GetService<IMigrationsSqlGenerator>();
        var commands = sqlGenerator.Generate(builder.Operations);

        foreach (var command in commands)
        {
            testContext.Database.ExecuteSqlRaw(command.CommandText);
        }

        // Verify the tables no longer exist in sqlite_master
        var tableNames = new[]
        {
            "CourseGamificationConfigs",
            "GamificationEvents",
            "StudentBadges",
            "StudentCourseProfiles",
            "CourseClassGroups"
        };

        foreach (var tableName in tableNames)
        {
            using var command = testContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}';";
            var result = Convert.ToInt32(command.ExecuteScalar());
            Assert.That(result, Is.EqualTo(0), $"Table {tableName} should have been dropped.");
        }
    }
    [Test]
    public void Context_ShouldNotHavePendingModelChanges()
    {
        var options = new DbContextOptionsBuilder<GutsContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=dummy;Trusted_Connection=True;")
            .Options;
        using var context = new GutsContext(options);
                Assert.That(context.Database.HasPendingModelChanges(), Is.False);
    }
}

