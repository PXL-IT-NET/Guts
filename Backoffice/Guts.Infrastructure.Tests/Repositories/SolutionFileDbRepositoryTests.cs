using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.ValueObjects;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class SolutionFileDbRepositoryTests : RepositoryTestsBase
{
    private int _assignmentId;
    private int _teamId;
    private int _user1Id;
    private int _user2Id;
    private DateTime _t1;
    private DateTime _t2;

    [SetUp]
    public async Task SetUpSeedData()
    {
        _t1 = new DateTime(2026, 10, 10, 10, 0, 0, DateTimeKind.Utc);
        _t2 = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var user1 = await Seeder.SeedUserAsync("Alice");
        var user2 = await Seeder.SeedUserAsync("Bob");

        _user1Id = user1.Id;
        _user2Id = user2.Id;

        var project = await Seeder.SeedProjectAsync(
            course.Id,
            period.Id,
            teams: ("Team 01", new[] { _user1Id, _user2Id }));

        _assignmentId = project.Assignments.First().Id;
        _teamId = project.Teams.First().Id;

        await using var context = CreateDbContext();
        var repo = new SolutionFileDbRepository(context);
        await repo.AddAsync(TestDataSeeder.CreateSolutionFile(_assignmentId, _user1Id, "Folder/Calculator.cs", _t1, "v1"));
        await repo.AddAsync(TestDataSeeder.CreateSolutionFile(_assignmentId, _user1Id, "Folder/Calculator.cs", _t2, "v2"));
        await repo.AddAsync(TestDataSeeder.CreateSolutionFile(_assignmentId, _user2Id, "Folder/Calculator.cs", _t1, "u2_v1"));
    }

    [Test]
    public async Task GetLatestForUserAsync_And_GetAllLatestOfAssignmentAsync_ShouldReturnLatestVersions()
    {
        var filePath = new FilePath("Folder/Calculator.cs");

        await using var context = CreateDbContext();
        var repo = new SolutionFileDbRepository(context);

        var latestUser1 = await repo.GetLatestForUserAsync(_assignmentId, _user1Id, filePath);
        Assert.That(latestUser1.Content, Is.EqualTo("v2"));

        var allLatest = await repo.GetAllLatestOfAssignmentAsync(_assignmentId);
        Assert.That(allLatest, Has.Count.EqualTo(2));
        Assert.That(allLatest.Select(f => f.Content), Is.EquivalentTo(new[] { "v2", "u2_v1" }));
    }

    [Test]
    public async Task GetAllLatestOfAssignmentForUserAsync_And_ForTeamAsync_ShouldRespectDateCutoff()
    {
        await using var context = CreateDbContext();
        var repo = new SolutionFileDbRepository(context);

        var user1AtT1 = await repo.GetAllLatestOfAssignmentForUserAsync(_assignmentId, _user1Id, _t1);
        Assert.That(user1AtT1, Has.Count.EqualTo(1));
        Assert.That(user1AtT1[0].Content, Is.EqualTo("v1"));

        var user1Latest = await repo.GetAllLatestOfAssignmentForUserAsync(_assignmentId, _user1Id, null);
        Assert.That(user1Latest, Has.Count.EqualTo(1));
        Assert.That(user1Latest[0].Content, Is.EqualTo("v2"));

        var teamLatest = await repo.GetAllLatestOfAssignmentForTeamAsync(_assignmentId, _teamId, null);
        Assert.That(teamLatest, Has.Count.EqualTo(1));
        Assert.That(teamLatest[0].Content, Is.EqualTo("v2"));
    }
}

