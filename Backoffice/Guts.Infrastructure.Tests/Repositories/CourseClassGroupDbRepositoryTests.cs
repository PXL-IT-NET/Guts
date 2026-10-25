using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class CourseClassGroupDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetByNameAsync_ShouldFindGroupCaseInsensitivelyAndTrimmed()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");

        await using (var seedContext = CreateDbContext())
        {
            var classGroup = new CourseClassGroup("1TIN-A", course.Id, period.Id);
            seedContext.CourseClassGroups.Add(classGroup);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new CourseClassGroupDbRepository(context);

        var found = await repo.GetByNameAsync(course.Id, period.Id, " 1tin-a ");
        Assert.That(found, Is.Not.Null);
        Assert.That(found.Name, Is.EqualTo("1TIN-A"));
        Assert.That(found.CourseId, Is.EqualTo(course.Id));
        Assert.That(found.PeriodId, Is.EqualTo(period.Id));
    }

    [Test]
    public async Task GetByNameAsync_WhenNotFound_ShouldReturnNull()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");

        await using var context = CreateDbContext();
        var repo = new CourseClassGroupDbRepository(context);

        var found = await repo.GetByNameAsync(course.Id, period.Id, "NonExistent");
        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task GetByCourseAndPeriodAsync_ShouldReturnGroupsOrderedByName()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("NET1");

        await using (var seedContext = CreateDbContext())
        {
            seedContext.CourseClassGroups.AddRange(
                new CourseClassGroup("1TIN-C", course.Id, period.Id),
                new CourseClassGroup("1TIN-A", course.Id, period.Id),
                new CourseClassGroup("1TIN-B", course.Id, period.Id)
            );
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new CourseClassGroupDbRepository(context);

        var list = await repo.GetByCourseAndPeriodAsync(course.Id, period.Id);
        Assert.That(list.Select(cg => cg.Name), Is.EqualTo(new[] { "1TIN-A", "1TIN-B", "1TIN-C" }));
    }
}
