using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ProjectDbRepositoryTests : RepositoryTestsBase
{
    private int _courseId;
    private string _courseCode;
    private int _periodId;
    private int _user1Id;

    [SetUp]
    public async Task SetUpSeedData()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var user1 = await Seeder.SeedUserAsync("Alice");
        var user2 = await Seeder.SeedUserAsync("Bob");

        _courseId = course.Id;
        _courseCode = course.Code;
        _periodId = period.Id;
        _user1Id = user1.Id;

        await Seeder.SeedProjectAsync(
            _courseId,
            _periodId,
            "Proj1",
            "Project 1",
            "Comp1",
            new[] { "PT1" },
            ("Team 01", new[] { user1.Id }),
            ("Team 02", new[] { user2.Id }));
    }

    [Test]
    public async Task GetSingleAsync_ByCourseCode_And_ByCourseId_ShouldReturnProjectOrThrow()
    {
        await using var context = CreateDbContext();
        var repo = new ProjectDbRepository(context);

        var byCourseCode = await repo.GetSingleAsync(_courseCode, "Proj1", _periodId);
        Assert.That(byCourseCode.Description, Is.EqualTo("Project 1"));
        Assert.That(async () => await repo.GetSingleAsync(_courseCode, "Missing", _periodId),
            Throws.InstanceOf<DataNotFoundException>());

        var byCourseId = await repo.GetSingleAsync(_courseId, "Proj1", _periodId);
        Assert.That(byCourseId.Description, Is.EqualTo("Project 1"));
        Assert.That(async () => await repo.GetSingleAsync(_courseId, "Missing", _periodId),
            Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task GetByCourseIdAsync_And_LoadWithAssignmentsAndTeamsAsync_ShouldLoadNavigationProperties()
    {
        await using var context = CreateDbContext();
        var repo = new ProjectDbRepository(context);

        var byCourseList = await repo.GetByCourseIdAsync(_courseId, _periodId);
        Assert.That(byCourseList, Has.Count.EqualTo(1));
        Assert.That(byCourseList[0].Assignments, Has.Count.EqualTo(1));

        var loaded = await repo.LoadWithAssignmentsAndTeamsAsync(_courseId, "Proj1", _periodId);
        Assert.That(loaded.Teams, Has.Count.EqualTo(2));
        Assert.That(loaded.Assignments.First().Tests, Has.Count.EqualTo(1));
        Assert.That(async () => await repo.LoadWithAssignmentsAndTeamsAsync(_courseId, "Missing", _periodId),
            Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task LoadWithAssignmentsAndTeamsOfUserAsync_ShouldFilterTeamsForUser()
    {
        await using var context = CreateDbContext();
        var repo = new ProjectDbRepository(context);

        var loadedForUser1 = await repo.LoadWithAssignmentsAndTeamsOfUserAsync(_courseId, "Proj1", _periodId, _user1Id);
        Assert.That(loadedForUser1.Teams, Has.Count.EqualTo(1));
        Assert.That(loadedForUser1.Teams.First().Name, Is.EqualTo("Team 01"));

        Assert.That(async () => await repo.LoadWithAssignmentsAndTeamsOfUserAsync(_courseId, "Missing", _periodId, _user1Id),
            Throws.InstanceOf<DataNotFoundException>());
    }
}
