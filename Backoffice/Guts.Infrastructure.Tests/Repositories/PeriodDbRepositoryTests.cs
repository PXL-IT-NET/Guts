using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Domain.PeriodAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class PeriodDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task DeleteAsync_ShouldCascadeToStudentCourseProfilesAndClassGroups()
    {
        var (course, _) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");
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

            var classGroup = new CourseClassGroup("1TIN-A", course.Id, pastPeriodId);
            seedContext.CourseClassGroups.Add(classGroup);
            await seedContext.SaveChangesAsync();

            var profile = new StudentCourseProfile(user.Id, course.Id, pastPeriodId);
            profile.Enroll(true, "Hero", "Wizard", classGroup.Id, false);
            seedContext.StudentCourseProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

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
        }
    }
}
