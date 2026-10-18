using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class TestRunDbRepositoryTests : RepositoryTestsBase
{
    private int _projectId;
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
            testNames: new[] { "Test1" },
            teams: ("Team 01", new[] { _user1Id, _user2Id }));

        _projectId = project.Id;
        var assignment = project.Assignments.First();
        _assignmentId = assignment.Id;
        var testId = assignment.Tests.First().Id;
        _teamId = project.Teams.First().Id;

        await Seeder.SeedTestRunAsync(_assignmentId, _user1Id, _t1, (testId, false));
        await Seeder.SeedTestRunAsync(_assignmentId, _user1Id, _t2, (testId, true));
        await Seeder.SeedTestRunAsync(_assignmentId, _user2Id, _t2, (testId, true));

        await using var context = CreateDbContext();
        context.SolutionFiles.Add(TestDataSeeder.CreateSolutionFile(_assignmentId, _user1Id, "A.cs", _t1, "code"));
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task GetUserTestRuns_GetLastTestRunOfAllUsers_And_GetTeamTestRuns_ShouldReturnMatchingRuns()
    {
        await using var context = CreateDbContext();
        var repo = new TestRunDbRepository(context);

        var userRuns = await repo.GetUserTestRunsForAssignmentAsync(_assignmentId, _user1Id, null);
        Assert.That(userRuns, Has.Count.EqualTo(2));

        var lastRuns = await repo.GetLastTestRunForAssignmentOfAllUsersAsync(_assignmentId);
        Assert.That(lastRuns, Has.Count.EqualTo(2));

        var teamRuns = await repo.GetTeamTestRunsForAssignmentAsync(_assignmentId, _teamId, null);
        Assert.That(teamRuns, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task RemoveAllTopicTestRunsOfUserAsync_ShouldRemoveTestRunsResultsAndSolutionFilesForUser()
    {
        await using (var context = CreateDbContext())
        {
            var repo = new TestRunDbRepository(context);
            await repo.RemoveAllTopicTestRunsOfUserAsync(_projectId, _user1Id);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TestRunDbRepository(context);
            var user1Runs = await repo.GetUserTestRunsForAssignmentAsync(_assignmentId, _user1Id, null);
            Assert.That(user1Runs, Is.Empty);
            Assert.That(context.SolutionFiles.Where(sf => sf.UserId == _user1Id).ToList(), Is.Empty);
        }
    }
}

