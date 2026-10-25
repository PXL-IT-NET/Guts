using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class CourseGamificationConfigDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetByCourseIdAsync_ExistingConfig_ShouldReturnConfig()
    {
        var (course, _) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");

        await using (var seedContext = CreateDbContext())
        {
            var config = new CourseGamificationConfig(course.Id)
            {
                IsGamificationEnabled = true,
                IsWeeklyStreakEnabled = true
            };
            seedContext.CourseGamificationConfigs.Add(config);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new CourseGamificationConfigDbRepository(context);

        var found = await repo.GetByCourseIdAsync(course.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found.CourseId, Is.EqualTo(course.Id));
        Assert.That(found.IsGamificationEnabled, Is.True);
    }

    [Test]
    public async Task GetByCourseIdAsync_NonExistingConfig_ShouldReturnNull()
    {
        await using var context = CreateDbContext();
        var repo = new CourseGamificationConfigDbRepository(context);

        var found = await repo.GetByCourseIdAsync(99999);
        Assert.That(found, Is.Null);
    }
}

