using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class TestResultDbRepositoryTests : RepositoryTestsBase
{
    private int _projectId;
    private int _assignmentId;
    private int _test1Id;
    private int _test2Id;
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
            testNames: new[] { "Test1", "Test2" },
            teams: ("Team 01", new[] { _user1Id, _user2Id }));

        _projectId = project.Id;
        var assignment = project.Assignments.First();
        _assignmentId = assignment.Id;
        var tests = assignment.Tests.ToList();
        _test1Id = tests[0].Id;
        _test2Id = tests[1].Id;
        _teamId = project.Teams.First().Id;

        await Seeder.SeedTestRunAsync(_assignmentId, _user1Id, _t1, (_test1Id, false), (_test2Id, true));
        await Seeder.SeedTestRunAsync(_assignmentId, _user1Id, _t2, (_test1Id, true));
        await Seeder.SeedTestRunAsync(_assignmentId, _user2Id, _t2, (_test2Id, false));
    }

    [Test]
    public async Task GetLastTestResultsOfUser_And_OfAllUsers_ShouldReturnLatestResultsPerUser()
    {
        await using var context = CreateDbContext();
        var repo = new TestResultDbRepository(context);

        var user1Latest = await repo.GetLastTestResultsOfUser(_assignmentId, _user1Id, null);
        Assert.That(user1Latest, Has.Count.EqualTo(2));
        Assert.That(user1Latest.Single(r => r.TestId == _test1Id).Passed, Is.True);

        var user1AtT1 = await repo.GetLastTestResultsOfUser(_assignmentId, _user1Id, _t1);
        Assert.That(user1AtT1.Single(r => r.TestId == _test1Id).Passed, Is.False);

        var allUsersLatest = await repo.GetLastTestResultsOfAllUsers(_assignmentId, null);
        Assert.That(allUsersLatest, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetLastTestResultsOfTeam_OfAllTeams_And_OfAssignmentsAsync_ShouldReturnLatestResults()
    {
        await using var context = CreateDbContext();
        var repo = new TestResultDbRepository(context);

        var teamLatest = await repo.GetLastTestResultsOfTeam(_projectId, _assignmentId, _teamId, null);
        Assert.That(teamLatest, Has.Count.EqualTo(2));

        var allTeamsLatest = await repo.GetLastTestResultsOfAllTeams(_projectId, _assignmentId, null);
        Assert.That(allTeamsLatest, Has.Count.EqualTo(2));

        var ofAssignments = await repo.GetLastTestResultsOfAssignmentsAsync(new[] { _assignmentId }, null);
        Assert.That(ofAssignments, Has.Count.EqualTo(3));
        Assert.That(ofAssignments.All(r => r.Test != null), Is.True);
        Assert.That(ofAssignments.All(r => r.User != null), Is.True);
    }
}
