using System;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Domain.DeletionJobAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class DeletionJobDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task AddGetUpdate_And_GetActiveAsync_ShouldManageDeletionJobs()
    {
        var factory = new DeletionJobFactory();
        var now = new DateTime(2026, 11, 15, 13, 30, 0, DateTimeKind.Utc);
        var coursePending = factory.CreateNew(DeletionTargetType.Course, 1, "C1", now);
        var periodInProgress = factory.CreateNew(DeletionTargetType.Period, 2, "P1", now.AddSeconds(5));
        periodInProgress.Start(50, now.AddSeconds(6), "Deleting test results");
        var completedJob = factory.CreateNew(DeletionTargetType.Course, 3, "C2", now.AddSeconds(1));
        completedJob.Start(10, now.AddSeconds(2), "Deleting");
        completedJob.Complete(now.AddSeconds(3));
        var failedJob = factory.CreateNew(DeletionTargetType.Period, 4, "P2", now.AddSeconds(2));
        failedJob.Fail("Boom", now.AddSeconds(3));

        await using (var context = CreateDbContext())
        {
            var repo = new DeletionJobDbRepository(context);
            await repo.AddAsync(periodInProgress);
            await repo.AddAsync(coursePending);
            await repo.AddAsync(completedJob);
            await repo.AddAsync(failedJob);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new DeletionJobDbRepository(context);

            var loaded = await repo.GetByIdAsync(coursePending.Id);
            Assert.That(loaded.TargetLabel, Is.EqualTo("C1"));

            loaded.Start(20, now.AddSeconds(10), "Deleting solution files");
            await repo.UpdateAsync(loaded);

            var allActive = await repo.GetActiveAsync();
            Assert.That(allActive, Has.Count.EqualTo(2));
            Assert.That(allActive[0].Id, Is.EqualTo(coursePending.Id));
            Assert.That(allActive[1].Id, Is.EqualTo(periodInProgress.Id));

            var courseActive = await repo.GetActiveAsync(DeletionTargetType.Course);
            Assert.That(courseActive, Has.Count.EqualTo(1));
            Assert.That(courseActive[0].Id, Is.EqualTo(coursePending.Id));

            await Assert.ThatAsync(
                () => repo.GetByIdAsync(Guid.NewGuid()),
                Throws.InstanceOf<DataNotFoundException>());
        }
    }
}
