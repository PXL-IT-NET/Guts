using System.Linq;
using System.Threading.Tasks;
using Guts.Infrastructure.Migrations;
using Guts.Infrastructure.Repositories;
using Guts.Infrastructure.Tests.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Migrations;

[TestFixture]
internal class AddProjectTeamAssessmentPeersMigrationTests : RepositoryTestsBase
{
    private class TestAddProjectTeamAssessmentPeers : AddProjectTeamAssessmentPeers
    {
        public void ApplyUp(MigrationBuilder builder) => Up(builder);
        public void ApplyDown(MigrationBuilder builder) => Down(builder);
    }

    [Test]
    public async Task Up_And_Down_ShouldCreateTableAndBackfillExistingPeersFromTeamUsersAndPeerAssessments()
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
        var teamAssessment = await Seeder.SeedProjectTeamAssessmentAsync(
            projectAssessment.Id,
            teamId,
            (user3.Id, user1.Id));

        // Simulate pre-migration database state:
        // 1. User 3 left TeamUsers in the past, while their PeerAssessment remained in PeerAssessments.
        // 2. Drop ProjectTeamAssessmentPeers table via Down(...) so we can run Up(...) from scratch.
        await using (var setupContext = CreateDbContext())
        {
            await setupContext.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM ProjectTeamUsers WHERE ProjectTeamId = {teamId} AND UserId = {user3.Id}");

            var downBuilder = new MigrationBuilder(setupContext.Database.ProviderName);
            new TestAddProjectTeamAssessmentPeers().ApplyDown(downBuilder);
            var sqlGenerator = setupContext.GetService<IMigrationsSqlGenerator>();
            foreach (var command in sqlGenerator.Generate(downBuilder.Operations))
            {
                await setupContext.Database.ExecuteSqlRawAsync(command.CommandText);
            }
        }

        // Act: Run Up(...) migration
        await using (var migrationContext = CreateDbContext())
        {
            var upBuilder = new MigrationBuilder(migrationContext.Database.ProviderName);
            new TestAddProjectTeamAssessmentPeers().ApplyUp(upBuilder);
            var sqlGenerator = migrationContext.GetService<IMigrationsSqlGenerator>();
            foreach (var command in sqlGenerator.Generate(upBuilder.Operations))
            {
                await migrationContext.Database.ExecuteSqlRawAsync(command.CommandText);
            }
        }

        // Assert: ProjectTeamAssessmentPeers contains User 1, User 2 (from ProjectTeamUsers) and User 3 (from PeerAssessments)
        await using (var verifyContext = CreateDbContext())
        {
            var repo = new ProjectTeamAssessmentDbRepository(verifyContext);
            var loaded = await repo.LoadAsync(projectAssessment.Id, teamId);
            Assert.That(loaded.Id, Is.EqualTo(teamAssessment.Id));
            Assert.That(loaded.Peers.Select(p => p.Id), Is.EquivalentTo([user1.Id, user2.Id, user3.Id]));
        }
    }
}
