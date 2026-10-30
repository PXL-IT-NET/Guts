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
        Assert.That(topicUsers.Select(u => u.FirstName), Is.EqualTo(new[] { "Alice", "Bob" }));

        var courseUsers = await repo.GetUsersOfCourseForCurrentPeriodAsync(course.Id);
        Assert.That(courseUsers.Select(u => u.FirstName), Is.EqualTo(new[] { "Alice", "Bob" }));
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
}
