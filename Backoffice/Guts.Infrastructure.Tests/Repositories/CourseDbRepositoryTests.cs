using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class CourseDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetAllAsync_ShouldReturnCoursesOrderedByName()
    {
        await Seeder.SeedCourseAsync("C1", "Course B");
        await Seeder.SeedCourseAsync("C2", "Course A");
        await Seeder.SeedCourseAsync("C3", "Course C");

        await using var context = CreateDbContext();
        var repo = new CourseDbRepository(context);

        var all = await repo.GetAllAsync();
        Assert.That(all.Select(c => c.Name), Is.EqualTo(new[] { "Course A", "Course B", "Course C" }));
    }

    [Test]
    public async Task GetSingleAsync_ShouldFindCourseCaseInsensitively_OrThrowDataNotFoundException()
    {
        await Seeder.SeedCourseAsync("dotNet1", ".NET Essentials");

        await using var context = CreateDbContext();
        var repo = new CourseDbRepository(context);

        var found = await repo.GetSingleAsync("DOTNET1");
        Assert.That(found.Name, Is.EqualTo(".NET Essentials"));

        Assert.That(async () => await repo.GetSingleAsync("NonExistent"), Throws.InstanceOf<DataNotFoundException>());
    }
}
