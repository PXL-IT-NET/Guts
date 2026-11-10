using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
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
            teams: ("Team Alpha", [user1.Id, user2.Id]));
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
        Assert.That(loaded.Peers, Has.Count.EqualTo(2));
        Assert.That(loaded.PeerAssessments, Has.Count.EqualTo(1));
        Assert.That(loaded.PeerAssessments.First().Subject, Is.Not.Null);

        await Assert.ThatAsync(async () => await repo.LoadAsync(99999, teamId),
            Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task UpdateAsync_WhenPeerIsExcluded_ShouldPersistPeersAndDeleteExcludedPeerAssessments()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var user1 = await Seeder.SeedUserAsync("Alice");
        var user2 = await Seeder.SeedUserAsync("Bob");
        var user3 = await Seeder.SeedUserAsync("Charlie");
        var project = await Seeder.SeedProjectAsync(
            course.Id,
            period.Id,
            teams: ("Team Alpha", [user1.Id, user2.Id, user3.Id]));
        var teamId = project.Teams.First().Id;

        var projectAssessment = await Seeder.SeedProjectAssessmentAsync(project.Id);
        await Seeder.SeedProjectTeamAssessmentAsync(projectAssessment.Id, teamId);

        // User 1 and User 2 evaluate everyone including User 3; User 3 evaluates User 1
        await using (var context = CreateDbContext())
        {
            var repo = new ProjectTeamAssessmentDbRepository(context);
            var loaded = await repo.LoadAsync(projectAssessment.Id, teamId);
            foreach (var evaluatorId in new[] { user1.Id, user2.Id })
            {
                foreach (var subjectId in new[] { user1.Id, user2.Id, user3.Id })
                {
                    loaded.AddOrUpdatePeerAssessment(evaluatorId, subjectId, Domain.ValueObjects.AssessmentScore.Average, Domain.ValueObjects.AssessmentScore.Average, Domain.ValueObjects.AssessmentScore.Average, "");
                }
            }
            loaded.AddOrUpdatePeerAssessment(user3.Id, user1.Id, Domain.ValueObjects.AssessmentScore.Average, Domain.ValueObjects.AssessmentScore.Average, Domain.ValueObjects.AssessmentScore.Average, "");
            await repo.UpdateAsync(loaded);
        }

        // Teacher excludes User 3 -> User 3 is removed from Peers and their PeerAssessments are deleted, auto-completing for User 1 & User 2
        await using (var context = CreateDbContext())
        {
            var repo = new ProjectTeamAssessmentDbRepository(context);
            var loaded = await repo.LoadAsync(projectAssessment.Id, teamId);
            loaded.SetPeerInclusion(user3.Id, false);
            await repo.UpdateAsync(loaded);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new ProjectTeamAssessmentDbRepository(context);
            var loaded = await repo.LoadAsync(projectAssessment.Id, teamId);
            Assert.That(loaded.IsComplete, Is.True);
            Assert.That(loaded.PeerAssessments, Has.Count.EqualTo(4));
            Assert.That(loaded.Peers.Select(p => p.Id), Is.EquivalentTo([user1.Id, user2.Id]));
            Assert.That(context.PeerAssessments.Count(), Is.EqualTo(4));
        }
    }
}
