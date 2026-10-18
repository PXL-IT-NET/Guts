using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Common;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ProjectTeamDbRepositoryTests : RepositoryTestsBase
{
    private int _projectId;
    private int _teamId;
    private int _user1Id;
    private int _user2Id;

    [SetUp]
    public async Task SetUpSeedData()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var user1 = await Seeder.SeedUserAsync("Alice");
        var user2 = await Seeder.SeedUserAsync("Bob");

        _user1Id = user1.Id;
        _user2Id = user2.Id;

        var project = await Seeder.SeedProjectAsync(
            course.Id,
            period.Id,
            teams: ("Team Alpha", new[] { _user1Id, _user2Id }));

        _projectId = project.Id;
        _teamId = project.Teams.First().Id;
    }

    [Test]
    public async Task GetByProjectWithUsersAsync_And_GetByUserAsync_ShouldReturnExpectedTeams()
    {
        await using var context = CreateDbContext();
        var repo = new ProjectTeamDbRepository(context);

        var byProject = await repo.GetByProjectWithUsersAsync(_projectId);
        Assert.That(byProject, Has.Count.EqualTo(1));
        Assert.That(byProject[0].TeamUsers, Has.Count.EqualTo(2));

        var byUser = await repo.GetByUserAsync(_user1Id);
        Assert.That(byUser, Has.Count.EqualTo(1));
        Assert.That(byUser[0].Id, Is.EqualTo(_teamId));
    }

    [Test]
    public async Task LoadByIdAsync_ShouldValidateIdAndLoadRelations()
    {
        await using var context = CreateDbContext();
        var repo = new ProjectTeamDbRepository(context);

        var loaded = await repo.LoadByIdAsync(_teamId);
        Assert.That(loaded.Project, Is.Not.Null);
        Assert.That(loaded.TeamUsers, Has.Count.EqualTo(2));

        Assert.That(async () => await repo.LoadByIdAsync(0), Throws.InstanceOf<ContractException>());
        Assert.That(async () => await repo.LoadByIdAsync(99999), Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task AddUserToTeam_And_RemoveUserFromTeam_ShouldUpdateMembersAndCascadePeerAssessments()
    {
        var user3 = await Seeder.SeedUserAsync("Charlie");
        var projectAssessment = await Seeder.SeedProjectAssessmentAsync(_projectId);
        await Seeder.SeedProjectTeamAssessmentAsync(projectAssessment.Id, _teamId, (_user1Id, _user2Id));

        await using (var context = CreateDbContext())
        {
            var repo = new ProjectTeamDbRepository(context);
            await repo.AddUserToTeam(_teamId, user3.Id);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new ProjectTeamDbRepository(context);
            var loaded = await repo.LoadByIdAsync(_teamId);
            Assert.That(loaded.TeamUsers, Has.Count.EqualTo(3));

            await repo.RemoveUserFromTeam(_teamId, _user1Id);

            Assert.That(async () => await repo.RemoveUserFromTeam(_teamId, 99999),
                Throws.InstanceOf<ContractException>());
        }

        await using (var context = CreateDbContext())
        {
            var repo = new ProjectTeamDbRepository(context);
            var loaded = await repo.LoadByIdAsync(_teamId);
            Assert.That(loaded.TeamUsers, Has.Count.EqualTo(2));
            Assert.That(context.PeerAssessments.ToList(), Is.Empty);
        }
    }
}
