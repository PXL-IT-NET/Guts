using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Common;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class TopicDbRepositoryTests : RepositoryTestsBase
{
    private int _courseId;
    private string _courseCode;
    private int _periodId;

    [SetUp]
    public async Task SetUpSeedData()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        _courseId = course.Id;
        _courseCode = course.Code;
        _periodId = period.Id;

        await Seeder.SeedChapterAsync(_courseId, _periodId, "Ch01", "Chapter 1", "Ex01", "Test1");
    }

    [Test]
    public async Task GetSingleAsync_ShouldReturnTopic_OrThrowDataNotFoundException()
    {
        await using var context = CreateDbContext();
        var repo = new TopicDbRepository(context);

        var topic = await repo.GetSingleAsync(_courseCode, "Ch01", _periodId);
        Assert.That(topic.Description, Is.EqualTo("Chapter 1"));

        Assert.That(async () => await repo.GetSingleAsync(_courseCode, "Missing", _periodId),
            Throws.InstanceOf<DataNotFoundException>());
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateDescription_OrThrowContractExceptionWhenNotFound()
    {
        await using (var context = CreateDbContext())
        {
            var repo = new TopicDbRepository(context);
            await repo.UpdateAsync(_courseId, "Ch01", _periodId, "Updated Chapter 1");

            Assert.That(async () => await repo.UpdateAsync(_courseId, "Missing", _periodId, "Desc"),
                Throws.InstanceOf<ContractException>());
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TopicDbRepository(context);
            var topic = await repo.GetSingleAsync(_courseCode, "Ch01", _periodId);
            Assert.That(topic.Description, Is.EqualTo("Updated Chapter 1"));
        }
    }

    [Test]
    public async Task GetByCourseWithAssignmentsAndTestsAsync_ShouldIncludeAssignmentsAndTests()
    {
        await using var context = CreateDbContext();
        var repo = new TopicDbRepository(context);

        var topics = await repo.GetByCourseWithAssignmentsAndTestsAsync(_courseId, _periodId);
        Assert.That(topics, Has.Count.EqualTo(1));
        Assert.That(topics[0].Assignments, Has.Count.EqualTo(1));
        Assert.That(topics[0].Assignments.First().Tests, Has.Count.EqualTo(1));
    }
}
