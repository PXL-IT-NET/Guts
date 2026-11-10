using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Domain.AssignmentAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class AssignmentDbRepositoryTests : RepositoryTestsBase
{
    private int _courseId;
    private int _periodId;
    private int _topicId;
    private int _assignmentId;

    [SetUp]
    public async Task SetUpSeedData()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("CS1");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id, "Ch1", "Chapter 1", "Ex1", "Test_A", "Test_B");

        _courseId = course.Id;
        _periodId = period.Id;
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
    public async Task UpdateAsync_WhenNewTestCodeHashAddedToAssignment_ShouldPersistHashToDatabase()
    {
        // Arrange & Act: load assignment, add a new test code hash via aggregate method, and update
        await using (var context = CreateDbContext())
        {
            var repo = new AssignmentDbRepository(context);
            var assignment = await repo.GetSingleAsync(_topicId, "Ex1");
            Assert.That(assignment.TestCodeHashes, Has.Count.EqualTo(1));

            var result = assignment.ValidateOrRegisterTestCodeHash("NEW_VALID_HASH", isLector: true);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HashWasAdded, Is.True);

            await repo.UpdateAsync(assignment);
        }

        // Assert: verify that the new hash is persisted and readable in a fresh DbContext
        await using (var verifyContext = CreateDbContext())
        {
            var verifyRepo = new AssignmentDbRepository(verifyContext);
            var updated = await verifyRepo.GetSingleAsync(_topicId, "Ex1");
            Assert.That(updated.TestCodeHashes, Has.Count.EqualTo(2));
            Assert.That(updated.TestCodeHashes.Any(h => h.Hash == "NEW_VALID_HASH"), Is.True);
        }
    }

    [Test]
    public async Task AddAsync_WithTestCodeHashes_ShouldPersistAssignmentAndHashes()
    {
        int newAssignmentId;
        await using (var context = CreateDbContext())
        {
            var repo = new AssignmentDbRepository(context);
            var newAssignment = new Assignment
            {
                Code = "Ex2",
                Description = "Exercise 2",
                TopicId = _topicId
            };
            newAssignment.AddTestCodeHash("INITIAL_HASH_1");
            newAssignment.AddTestCodeHash("INITIAL_HASH_2");

            var created = await repo.AddAsync(newAssignment);
            newAssignmentId = created.Id;
            Assert.That(newAssignmentId, Is.GreaterThan(0));
        }

        await using (var verifyContext = CreateDbContext())
        {
            var verifyRepo = new AssignmentDbRepository(verifyContext);
            var loaded = await verifyRepo.GetSingleAsync(_topicId, "Ex2");
            Assert.That(loaded.Id, Is.EqualTo(newAssignmentId));
            Assert.That(loaded.TestCodeHashes, Has.Count.EqualTo(2));
            Assert.That(loaded.TestCodeHashes.Select(h => h.Hash), Is.EquivalentTo(new[] { "INITIAL_HASH_1", "INITIAL_HASH_2" }));
        }
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

    [Test]
    public async Task GetCourseTestIdsAsync_ShouldReturnOnlyTestIdsForCourseAndPeriod()
    {
        await using var context = CreateDbContext();
        var repo = new AssignmentDbRepository(context);

        var testIds = await repo.GetCourseTestIdsAsync(_courseId, _periodId);
        Assert.That(testIds, Has.Count.EqualTo(2));

        var emptyIds = await repo.GetCourseTestIdsAsync(99999, _periodId);
        Assert.That(emptyIds, Is.Empty);
    }
}
