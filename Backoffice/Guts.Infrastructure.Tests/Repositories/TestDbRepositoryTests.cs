using System.Linq;
using System.Threading.Tasks;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class TestDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task FindByAssignmentId_ShouldReturnTestsOfAssignment()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id, "Ch1", "Chapter 1", "Ex1", "Test_A", "Test_B");
        var assignmentId = chapter.Assignments.First().Id;

        await using var context = CreateDbContext();
        var repo = new TestDbRepository(context);

        var tests = await repo.FindByAssignmentId(assignmentId);
        Assert.That(tests.Select(t => t.TestName), Is.EquivalentTo(new[] { "Test_A", "Test_B" }));
    }
}
