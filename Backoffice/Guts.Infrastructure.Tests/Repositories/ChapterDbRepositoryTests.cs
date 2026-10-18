using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ChapterDbRepositoryTests : RepositoryTestsBase
{
    private int _courseId;
    private string _courseCode;
    private int _periodId;

    [SetUp]
    public async Task SetUpSeedData()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        _courseId = course.Id;
        _courseCode = course.Code;
        _periodId = period.Id;

        await Seeder.SeedChapterAsync(_courseId, _periodId, "Ch02", "Chapter 2", "Ex01", "T1");
        await Seeder.SeedChapterAsync(_courseId, _periodId, "Ch01", "Chapter 1", assignmentCode: null);
    }

    [Test]
    public async Task GetSingleAsync_ShouldReturnChapter_OrThrowDataNotFoundException()
    {
        await using var context = CreateDbContext();
        var repo = new ChapterDbRepository(context);

        var single = await repo.GetSingleAsync(_courseCode, "Ch01", _periodId);
        Assert.That(single.Description, Is.EqualTo("Chapter 1"));

        Assert.That(async () => await repo.GetSingleAsync(_courseCode, "NotFound", _periodId),
            Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task LoadWithAssignmentsAsync_And_LoadWithAssignmentsAndTestsAsync_ShouldIncludeNavigationProperties()
    {
        await using var context = CreateDbContext();
        var repo = new ChapterDbRepository(context);

        var withAssignments = await repo.LoadWithAssignmentsAsync(_courseId, "Ch02", _periodId);
        Assert.That(withAssignments.Assignments, Has.Count.EqualTo(1));

        var withTests = await repo.LoadWithAssignmentsAndTestsAsync(_courseId, "Ch02", _periodId);
        Assert.That(withTests.Assignments.First().Tests, Has.Count.EqualTo(1));

        Assert.That(async () => await repo.LoadWithAssignmentsAsync(_courseId, "NotFound", _periodId),
            Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task GetByCourseIdAsync_ShouldReturnChaptersOrderedByCodeWithAssignments()
    {
        await using var context = CreateDbContext();
        var repo = new ChapterDbRepository(context);

        var byCourse = await repo.GetByCourseIdAsync(_courseId, _periodId);
        Assert.That(byCourse.Select(c => c.Code.ToString()), Is.EqualTo(new[] { "Ch01", "Ch02" }));
    }
}
