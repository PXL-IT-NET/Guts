using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;
using Guts.Infrastructure.Repositories;
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
}
