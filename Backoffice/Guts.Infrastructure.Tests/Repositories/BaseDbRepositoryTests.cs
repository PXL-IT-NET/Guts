using System;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Domain.CourseAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class BaseDbRepositoryTests : RepositoryTestsBase
{
    private sealed class TestableCourseDbRepository : BaseDbRepository<Course, Course>
    {
        public TestableCourseDbRepository(GutsContext context) : base(context)
        {
        }
    }

    [Test]
    public async Task AddAsync_GetByIdAsync_UpdateAsync_DeleteAsync_ShouldPersistChanges()
    {
        int courseId;

        await using (var context = CreateDbContext())
        {
            var repo = new TestableCourseDbRepository(context);
            var added = await repo.AddAsync(new Course { Code = "CS101", Name = "Intro to C#" });
            Assert.That(added.Id, Is.GreaterThan(0));
            courseId = added.Id;
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TestableCourseDbRepository(context);
            var fetched = await repo.GetByIdAsync(courseId);
            Assert.That(fetched.Code, Is.EqualTo("CS101"));
            Assert.That(fetched.Name, Is.EqualTo("Intro to C#"));

            fetched.Name = "Advanced C#";
            await repo.UpdateAsync(fetched);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TestableCourseDbRepository(context);
            var updated = await repo.GetByIdAsync(courseId);
            Assert.That(updated.Name, Is.EqualTo("Advanced C#"));

            await repo.DeleteAsync(updated);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TestableCourseDbRepository(context);
            Assert.That(async () => await repo.GetByIdAsync(courseId), Throws.InstanceOf<DataNotFoundException>());
        }
    }

    [Test]
    public async Task AddWithPositiveId_And_UpdateWithZeroId_ShouldThrowArgumentException()
    {
        await using var context = CreateDbContext();
        var repo = new TestableCourseDbRepository(context);

        Assert.That(async () => await repo.AddAsync(new Course { Id = 5, Code = "C1", Name = "N1" }),
            Throws.InstanceOf<ArgumentException>());

        Assert.That(async () => await repo.UpdateAsync(new Course { Id = 0, Code = "C1", Name = "N1" }),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public async Task GetAllAsync_DeleteByIdAsync_And_DeleteBulkAsync_ShouldManageEntities()
    {
        var c1 = await Seeder.SeedCourseAsync("C1", "Course 1");
        await Seeder.SeedCourseAsync("C2", "Course 2");
        await Seeder.SeedCourseAsync("C3", "Course 3");

        await using (var context = CreateDbContext())
        {
            var repo = new TestableCourseDbRepository(context);
            var all = await repo.GetAllAsync();
            Assert.That(all, Has.Count.EqualTo(3));

            await repo.DeleteByIdAsync(c1.Id);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TestableCourseDbRepository(context);
            var remaining = await repo.GetAllAsync();
            Assert.That(remaining, Has.Count.EqualTo(2));

            await repo.DeleteBulkAsync(remaining);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new TestableCourseDbRepository(context);
            var all = await repo.GetAllAsync();
            Assert.That(all, Is.Empty);
        }
    }
}

