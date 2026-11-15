using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.PeriodAggregate;
using Guts.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class PeriodDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task DeleteAsync_ShouldCascadeToStudentCourseProfilesAndClassGroups()
    {
        var (course, activePeriod) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
        var user = await Seeder.SeedUserAsync("John", "Doe");

        int pastPeriodId;
        await using (var seedContext = CreateDbContext())
        {
            var factory = new Period.Factory();
            var pastPeriod = factory.CreateNew(
                "Past 2020",
                DateTime.Today.AddYears(-2),
                DateTime.Today.AddYears(-1),
                []);
            seedContext.Periods.Add(pastPeriod);
            await seedContext.SaveChangesAsync();
            pastPeriodId = pastPeriod.Id;

            var pastClassGroup = new CourseClassGroup("1TIN-A", course.Id, pastPeriodId);
            var activeClassGroup = new CourseClassGroup("1TIN-A", course.Id, activePeriod.Id);
            seedContext.CourseClassGroups.AddRange(pastClassGroup, activeClassGroup);
            await seedContext.SaveChangesAsync();

            var pastProfile = new StudentCourseProfile(user.Id, course.Id, pastPeriodId);
            pastProfile.Enroll(true, "Hero", "Wizard", pastClassGroup.Id, false);
            pastProfile.AwardPassedTestXp(1, DateTime.UtcNow);

            var activeProfile = new StudentCourseProfile(user.Id, course.Id, activePeriod.Id);
            activeProfile.Enroll(true, "Hero", "Wizard", activeClassGroup.Id, false);

            seedContext.StudentCourseProfiles.AddRange(pastProfile, activeProfile);
            await seedContext.SaveChangesAsync();
        }

        await Seeder.SeedChapterAsync(course.Id, pastPeriodId, "Ch01", "Past Chapter");

        await using (var context = CreateDbContext())
        {
            var repo = new PeriodDbRepository(context);
            var period = await repo.GetPeriodAsync(pastPeriodId);
            await repo.DeleteAsync(period);
        }

        await using (var verifyContext = CreateDbContext())
        {
            Assert.That(verifyContext.Periods.Any(p => p.Id == pastPeriodId), Is.False);
            Assert.That(verifyContext.StudentCourseProfiles.Any(sp => sp.PeriodId == pastPeriodId), Is.False);
            Assert.That(verifyContext.CourseClassGroups.Any(cg => cg.PeriodId == pastPeriodId), Is.False);
            Assert.That(verifyContext.Topics.Any(t => t.PeriodId == pastPeriodId), Is.False);

            Assert.That(verifyContext.Periods.Any(p => p.Id == activePeriod.Id), Is.True);
            Assert.That(verifyContext.StudentCourseProfiles.Any(sp => sp.PeriodId == activePeriod.Id), Is.True);
            Assert.That(verifyContext.CourseClassGroups.Any(cg => cg.PeriodId == activePeriod.Id), Is.True);
        }
    }

    [Test]
    public async Task QueueDeletionAsync_ShouldMarkPeriodDeletingPersistJobAndExcludeFromNormalReads()
    {
        var activePeriod = await Seeder.SeedPeriodAsync(DateTime.Today.AddDays(-5), DateTime.Today.AddDays(5), "Current");
        var pastPeriod = await Seeder.SeedPeriodAsync(DateTime.Today.AddYears(-2), DateTime.Today.AddYears(-1), "Past");
        var job = new DeletionJobFactory().CreateNew(
            DeletionTargetType.Period,
            pastPeriod.Id,
            pastPeriod.Description,
            DateTime.UtcNow);

        await using (var context = CreateDbContext())
        {
            var repo = new PeriodDbRepository(context);
            var loaded = await repo.GetPeriodAsync(pastPeriod.Id);
            loaded.MarkAsDeleting();
            await repo.QueueDeletionAsync(loaded, job);
        }

        await using (var verifyContext = CreateDbContext())
        {
            var repo = new PeriodDbRepository(verifyContext);
            var all = await repo.GetAllAsync();
            Assert.That(all.Select(p => p.Id), Is.EqualTo([activePeriod.Id]));
            await Assert.ThatAsync(() => repo.GetPeriodAsync(pastPeriod.Id), Throws.InstanceOf<DataNotFoundException>());
            Assert.That(await verifyContext.DeletionJobs.AnyAsync(j => j.Id == job.Id), Is.True);
        }
    }
}
