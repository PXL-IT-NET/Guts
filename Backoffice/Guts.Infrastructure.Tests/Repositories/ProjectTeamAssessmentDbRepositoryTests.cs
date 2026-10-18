using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ProjectTeamAssessmentDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task LoadAsync_ShouldLoadWithTeamUsersAndPeerAssessments_OrThrowWhenNotFound()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var user1 = await Seeder.SeedUserAsync("Alice");
        var user2 = await Seeder.SeedUserAsync("Bob");
        var project = await Seeder.SeedProjectAsync(
            course.Id,
            period.Id,
            teams: ("Team Alpha", new[] { user1.Id, user2.Id }));
        var teamId = project.Teams.First().Id;

        var projectAssessment = await Seeder.SeedProjectAssessmentAsync(project.Id);
        var teamAssessment = await Seeder.SeedProjectTeamAssessmentAsync(
            projectAssessment.Id,
            teamId,
            (user1.Id, user2.Id));

        await using var context = CreateDbContext();
        var repo = new ProjectTeamAssessmentDbRepository(context);

        var loaded = await repo.LoadAsync(projectAssessment.Id, teamId);
        Assert.That(loaded.Id, Is.EqualTo(teamAssessment.Id));
        Assert.That(loaded.Team.TeamUsers, Has.Count.EqualTo(2));
        Assert.That(loaded.PeerAssessments, Has.Count.EqualTo(1));

        Assert.That(async () => await repo.LoadAsync(99999, teamId),
            Throws.InstanceOf<DataNotFoundException>());
    }
}
