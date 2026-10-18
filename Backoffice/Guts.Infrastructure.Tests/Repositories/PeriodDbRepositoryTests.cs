using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class PeriodDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task GetAllAsync_ShouldReturnPeriodsOrderedByFromDate()
    {
        await Seeder.SeedPeriodAsync(DateTime.Today.AddDays(10), DateTime.Today.AddDays(20), "Later");
        await Seeder.SeedPeriodAsync(DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-10), "Earlier");

        await using var context = CreateDbContext();
        var repo = new PeriodDbRepository(context);

        var periods = await repo.GetAllAsync();
        Assert.That(periods.Select(p => p.Description), Is.EqualTo(new[] { "Earlier", "Later" }));
    }

    [Test]
    public async Task GetPeriodAsync_ShouldReturnById_OrActivePeriod_OrThrowWhenNotFound()
    {
        var past = await Seeder.SeedPeriodAsync(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-10), "Past");

        await using (var context = CreateDbContext())
        {
            var repo = new PeriodDbRepository(context);
            Assert.That(async () => await repo.GetPeriodAsync(null), Throws.InstanceOf<DataNotFoundException>());
        }

        var active = await Seeder.SeedPeriodAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(10), "Active");

        await using (var context = CreateDbContext())
        {
            var repo = new PeriodDbRepository(context);

            var byId = await repo.GetPeriodAsync(past.Id);
            Assert.That(byId.Description, Is.EqualTo("Past"));

            var current = await repo.GetPeriodAsync(null);
            Assert.That(current.Id, Is.EqualTo(active.Id));

            Assert.That(async () => await repo.GetPeriodAsync(99999), Throws.InstanceOf<DataNotFoundException>());
        }
    }
}
