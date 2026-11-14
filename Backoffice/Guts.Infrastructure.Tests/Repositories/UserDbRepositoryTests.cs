using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class UserDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetUsersOfTopicAsync_And_GetUsersOfCourseForCurrentPeriodAsync_ShouldReturnOrderedUsers()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var userB = await Seeder.SeedUserAsync("Bob", "Jones");
        var userA = await Seeder.SeedUserAsync("Alice", "Smith");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id);
        var assignment = chapter.Assignments.First();
        var testId = assignment.Tests.First().Id;

        await Seeder.SeedTestRunAsync(assignment.Id, userB.Id, DateTime.UtcNow, (testId, true));
        await Seeder.SeedTestRunAsync(assignment.Id, userA.Id, DateTime.UtcNow, (testId, true));

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var topicUsers = await repo.GetUsersOfTopicAsync(chapter.Id);
        Assert.That(topicUsers.Select(u => u.FirstName), Is.EqualTo(["Alice", "Bob"]));

        var courseUsers = await repo.GetUsersOfCourseForCurrentPeriodAsync(course.Id);
        Assert.That(courseUsers.Select(u => u.FirstName), Is.EqualTo(["Alice", "Bob"]));
    }

    [Test]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        var user = await Seeder.SeedUserAsync("Charlie", "Brown");

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var result = await repo.GetByIdAsync(user.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(user.Id));
        Assert.That(result.FirstName, Is.EqualTo("Charlie"));
        Assert.That(result.LastName, Is.EqualTo("Brown"));
    }

    [Test]
    public async Task SearchUsersAndCourseLinks_ShouldIdentifyLinkedAndOrphanedUsers()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("USR1");
        var linkedUser = await Seeder.SeedUserAsync("Linked", "Student", "linked@student.pxl.be");
        var OrphanedUser = await Seeder.SeedUserAsync("Orphaned", "Student", "Orphaned@student.pxl.be");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id);
        var assignment = chapter.Assignments.First();
        var testId = assignment.Tests.First().Id;
        await Seeder.SeedTestRunAsync(assignment.Id, linkedUser.Id, DateTime.UtcNow, (testId, true));

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var linked = await repo.GetCourseLinksAsync(linkedUser.Id);
        var Orphaned = await repo.GetCourseLinksAsync(OrphanedUser.Id);
        var OrphanedResults = await repo.SearchUsersAsync("Orphaned", null, orphanedOnly: true);

        Assert.That(linked.HasAnyLinks, Is.True);
        Assert.That(Orphaned.HasAnyLinks, Is.False);
        Assert.That(OrphanedResults.TotalCount, Is.EqualTo(1));
        Assert.That(OrphanedResults.Items.Select(u => u.User.Id), Is.EquivalentTo([OrphanedUser.Id]));

        var linkedSearch = await repo.SearchUsersAsync("linked@student.pxl.be", null, orphanedOnly: false);
        Assert.That(linkedSearch.Items, Has.Count.EqualTo(1));
        Assert.That(linkedSearch.Items[0].LinkedCourses, Has.Count.EqualTo(1));
        Assert.That(linkedSearch.Items[0].LinkedCourses[0].Id, Is.EqualTo(course.Id));
        Assert.That(linkedSearch.Items[0].LinkedCourses[0].Code, Is.EqualTo(course.Code));
    }

    [Test]
    public async Task SearchUsersAsync_ShouldPaginateUsingSkipAndTakeAndSupportAll()
    {
        string prefix = "PagedTest_" + Guid.NewGuid().ToString("N")[..6];
        await Seeder.SeedUserAsync("Alice", prefix + "_A", prefix + "a@student.pxl.be");
        await Seeder.SeedUserAsync("Bob", prefix + "_B", prefix + "b@student.pxl.be");
        await Seeder.SeedUserAsync("Charlie", prefix + "_C", prefix + "c@student.pxl.be");

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var page1 = await repo.SearchUsersAsync(prefix, null, orphanedOnly: false, page: 1, pageSize: 2);
        var page2 = await repo.SearchUsersAsync(prefix, null, orphanedOnly: false, page: 2, pageSize: 2);
        var all = await repo.SearchUsersAsync(prefix, null, orphanedOnly: false, page: 1, pageSize: null);

        Assert.That(page1.TotalCount, Is.EqualTo(3));
        Assert.That(page1.Items.Select(x => x.User.FirstName), Is.EqualTo(["Alice", "Bob"]));
        Assert.That(page2.TotalCount, Is.EqualTo(3));
        Assert.That(page2.Items.Select(x => x.User.FirstName), Is.EqualTo(["Charlie"]));
        Assert.That(all.TotalCount, Is.EqualTo(3));
        Assert.That(all.Items.Select(x => x.User.FirstName), Is.EqualTo(["Alice", "Bob", "Charlie"]));
    }

    [Test]
    public async Task DeleteRangeAsync_ShouldRemoveMultipleUsers()
    {
        var user1 = await Seeder.SeedUserAsync("Del1", "User", $"del1_{Guid.NewGuid():N}@student.pxl.be");
        var user2 = await Seeder.SeedUserAsync("Del2", "User", $"del2_{Guid.NewGuid():N}@student.pxl.be");

        await using var context = CreateDbContext();
        var repo = new UserDbRepository(context);

        var loaded = await repo.GetWithRolesAndLinksByIdsAsync([user1.Id, user2.Id]);
        Assert.That(loaded, Has.Count.EqualTo(2));

        await repo.DeleteRangeAsync(loaded.Select(x => x.User).ToList());

        Assert.That(await repo.GetByIdAsync(user1.Id), Is.Null);
        Assert.That(await repo.GetByIdAsync(user2.Id), Is.Null);
    }
}
