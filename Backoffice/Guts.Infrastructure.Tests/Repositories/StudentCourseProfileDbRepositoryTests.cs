using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class StudentCourseProfileDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetByUserCourseAndPeriodAsync_ShouldIncludeUserAndClassGroup()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
        var user = await Seeder.SeedUserAsync("John", "Doe");

        int profileId;
        await using (var seedContext = CreateDbContext())
        {
            var classGroup = new CourseClassGroup("1TIN-A", course.Id, period.Id);
            seedContext.CourseClassGroups.Add(classGroup);
            await seedContext.SaveChangesAsync();

            var profile = new StudentCourseProfile(user.Id, course.Id, period.Id);
            profile.Enroll(true, "HeroCode", "Wizard", classGroup.Id, false);
            seedContext.StudentCourseProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
            profileId = profile.Id;
        }

        await using var context = CreateDbContext();
        var repo = new StudentCourseProfileDbRepository(context);

        var found = await repo.GetByUserCourseAndPeriodAsync(user.Id, course.Id, period.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found.Id, Is.EqualTo(profileId));
        Assert.That(found.User, Is.Not.Null);
        Assert.That(found.User.FirstName, Is.EqualTo("John"));
        Assert.That(found.User.LastName, Is.EqualTo("Doe"));
        Assert.That(found.ClassGroup, Is.Not.Null);
        Assert.That(found.ClassGroup.Name, Is.EqualTo("1TIN-A"));
    }

    [Test]
    public async Task AddAsync_Then_GetByUserCourseAndPeriodAsync_OnSameContext_ShouldIncludeUser()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
        var user = await Seeder.SeedUserAsync("Jane", "Smith");

        await using var context = CreateDbContext();
        var repo = new StudentCourseProfileDbRepository(context);

        var profile = new StudentCourseProfile(user.Id, course.Id, period.Id);
        profile = await repo.AddAsync(profile);

        Assert.That(profile.User, Is.Null, "Initially profile.User is null because it was added in memory");

        var retrieved = await repo.GetByUserCourseAndPeriodAsync(user.Id, course.Id, period.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.User, Is.Not.Null, "After GetByUserCourseAndPeriodAsync on the SAME DbContext, retrieved.User should be loaded");
        Assert.That(retrieved.User.FirstName, Is.EqualTo("Jane"));

        retrieved.Enroll(true, "ChangedName", "Cat", null, false);
        var updated = await repo.UpdateAsync(retrieved);
        Assert.That(updated.User, Is.Not.Null);
        Assert.That(updated.User.FirstName, Is.EqualTo("Jane"));
    }

    [Test]
    public async Task GetForLeaderboardByCourseAndPeriodAsync_ShouldReturnOnlyProfilesWithShowInLeaderboard()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
        var user1 = await Seeder.SeedUserAsync("Alice", "One");
        var user2 = await Seeder.SeedUserAsync("Bob", "Two");

        await using (var seedContext = CreateDbContext())
        {
            var profile1 = new StudentCourseProfile(user1.Id, course.Id, period.Id);
            profile1.Enroll(true, "AliceAlias", "Wizard", null, false);

            var profile2 = new StudentCourseProfile(user2.Id, course.Id, period.Id);
            profile2.HideFromLeaderboard();

            seedContext.StudentCourseProfiles.AddRange(profile1, profile2);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new StudentCourseProfileDbRepository(context);

        var leaderboardProfiles = await repo.GetForLeaderboardByCourseAndPeriodAsync(course.Id, period.Id);
        Assert.That(leaderboardProfiles.Count, Is.EqualTo(1));
        Assert.That(leaderboardProfiles[0].UserId, Is.EqualTo(user1.Id));
    }

    [Test]
    public async Task CountAssignmentCompletionsAsync_ShouldCountOnlyCompletedEventsForAssignment()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
        var user = await Seeder.SeedUserAsync("Charlie", "Three");
        const int assignmentId = 42;

        await using (var seedContext = CreateDbContext())
        {
            var profile = new StudentCourseProfile(user.Id, course.Id, period.Id);
            profile.AwardAssignmentBonus(assignmentId, System.DateTime.UtcNow, 0);
            seedContext.StudentCourseProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new StudentCourseProfileDbRepository(context);

        var count = await repo.CountAssignmentCompletionsAsync(assignmentId, course.Id, period.Id);
        Assert.That(count, Is.EqualTo(1));

        var nonExistentCount = await repo.CountAssignmentCompletionsAsync(999, course.Id, period.Id);
        Assert.That(nonExistentCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetByCourseAndPeriodAsync_And_UpdateRangeAsync_ShouldLoadAllProfilesAndPersistBatchUpdates()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
        var user1 = await Seeder.SeedUserAsync("Alice", "One");
        var user2 = await Seeder.SeedUserAsync("Bob", "Two");

        await using (var seedContext = CreateDbContext())
        {
            var profile1 = new StudentCourseProfile(user1.Id, course.Id, period.Id);
            profile1.AwardPassedTestXp(101, new System.DateTime(2026, 10, 5, 10, 0, 0, System.DateTimeKind.Utc));

            var profile2 = new StudentCourseProfile(user2.Id, course.Id, period.Id);
            profile2.HideFromLeaderboard();
            profile2.AwardPassedTestXp(101, new System.DateTime(2026, 10, 5, 10, 0, 0, System.DateTimeKind.Utc));

            seedContext.StudentCourseProfiles.AddRange(profile1, profile2);
            await seedContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateDbContext())
        {
            var repo = new StudentCourseProfileDbRepository(updateContext);
            var allProfiles = await repo.GetByCourseAndPeriodAsync(course.Id, period.Id);

            Assert.That(allProfiles, Has.Count.EqualTo(2));

            var evalTime = new System.DateTime(2026, 10, 19, 10, 0, 0, System.DateTimeKind.Utc);
            foreach (var p in allProfiles)
            {
                p.EvaluateWeeklyStreak(evalTime, new[] { 101, 102 });
            }

            await repo.UpdateRangeAsync(allProfiles);
        }

        await using (var verifyContext = CreateDbContext())
        {
            var repo = new StudentCourseProfileDbRepository(verifyContext);
            var verifiedProfiles = await repo.GetByCourseAndPeriodAsync(course.Id, period.Id);

            Assert.That(verifiedProfiles, Has.Count.EqualTo(2));
            Assert.That(verifiedProfiles[0].WeeklyStreak.CurrentStreak, Is.Zero);
            Assert.That(verifiedProfiles[1].WeeklyStreak.CurrentStreak, Is.Zero);
        }
    }

    [Test]
    public async Task IsUserLectorAsync_And_LeaderboardQueries_ShouldIdentifyAndExcludeLectors()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
        var student = await Seeder.SeedUserAsync("Student", "One");
        var lector = await Seeder.SeedUserAsync("Teacher", "Lector");

        await using (var seedContext = CreateDbContext())
        {
            var lectorRole = new Role { Name = Role.Constants.Lector, NormalizedName = Role.Constants.Lector.ToUpperInvariant() };
            seedContext.Roles.Add(lectorRole);
            await seedContext.SaveChangesAsync();

            seedContext.UserRoles.Add(new IdentityUserRole<int>
            {
                UserId = lector.Id,
                RoleId = lectorRole.Id
            });

            var studentProfile = new StudentCourseProfile(student.Id, course.Id, period.Id);
            studentProfile.AwardAssignmentBonus(42, System.DateTime.UtcNow, 0);

            var lectorProfile = new StudentCourseProfile(lector.Id, course.Id, period.Id);
            lectorProfile.AwardAssignmentBonus(42, System.DateTime.UtcNow, 0);

            seedContext.StudentCourseProfiles.AddRange(studentProfile, lectorProfile);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new StudentCourseProfileDbRepository(context);

        Assert.That(await repo.IsUserLectorAsync(lector.Id), Is.True);
        Assert.That(await repo.IsUserLectorAsync(student.Id), Is.False);

        var leaderboardProfiles = await repo.GetForLeaderboardByCourseAndPeriodAsync(course.Id, period.Id);
        Assert.That(leaderboardProfiles.Select(p => p.UserId), Is.EqualTo(new[] { student.Id }));

        var courseProfiles = await repo.GetByCourseAndPeriodAsync(course.Id, period.Id);
        Assert.That(courseProfiles.Select(p => p.UserId), Is.EqualTo(new[] { student.Id }));

        var completionCount = await repo.CountAssignmentCompletionsAsync(42, course.Id, period.Id);
        Assert.That(completionCount, Is.EqualTo(1));
    }
}
