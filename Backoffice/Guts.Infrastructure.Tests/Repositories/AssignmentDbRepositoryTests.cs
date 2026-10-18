using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class AssignmentDbRepositoryTests : RepositoryTestsBase
{
    private int _topicId;
    private int _assignmentId;

    [SetUp]
    public async Task SetUpSeedData()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("CS1");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id, "Ch1", "Chapter 1", "Ex1", "Test_A", "Test_B");

        _topicId = chapter.Id;
        _assignmentId = chapter.Assignments.First().Id;
    }

    [Test]
    public async Task GetSingleAsync_ShouldLoadWithTestCodeHashes_OrThrowWhenNotFound()
    {
        await using var context = CreateDbContext();
        var repo = new AssignmentDbRepository(context);

        var single = await repo.GetSingleAsync(_topicId, "Ex1");
        Assert.That(single.Id, Is.EqualTo(_assignmentId));
        Assert.That(single.TestCodeHashes, Has.Count.EqualTo(1));
        Assert.That(async () => await repo.GetSingleAsync(_topicId, "Missing"), Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task GetSingleWithTestsAsync_And_GetSingleWithTestsAndCourseAsync_ShouldLoadExpectedRelations()
    {
        await using var context = CreateDbContext();
        var repo = new AssignmentDbRepository(context);

        var withTests = await repo.GetSingleWithTestsAsync(_assignmentId);
        Assert.That(withTests.Tests, Has.Count.EqualTo(2));
        Assert.That(async () => await repo.GetSingleWithTestsAsync(99999), Throws.InstanceOf<DataNotFoundException>());

        var withTestsAndCourse = await repo.GetSingleWithTestsAndCourseAsync(_assignmentId);
        Assert.That(withTestsAndCourse.Tests, Has.Count.EqualTo(2));
        Assert.That(withTestsAndCourse.Topic.Course.Code, Is.EqualTo("CS1"));
        Assert.That(async () => await repo.GetSingleWithTestsAndCourseAsync(99999), Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task GetByTopicWithTests_ShouldReturnAssignmentsWithTests()
    {
        await using var context = CreateDbContext();
        var repo = new AssignmentDbRepository(context);

        var byTopic = await repo.GetByTopicWithTests(_topicId);
        Assert.That(byTopic, Has.Count.EqualTo(1));
        Assert.That(byTopic[0].Tests, Has.Count.EqualTo(2));
    }
}
