using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;
using Guts.Infrastructure.Tests.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests;

[TestFixture]
internal class DbSetExtensionsTests : RepositoryTestsBase
{
    [Test]
    public async Task AddIfNotExists_ShouldOnlyAddEntityWhenIdentifierDoesNotExist()
    {
        await using (var context = CreateDbContext())
        {
            context.Courses.AddIfNotExists(c => c.Code, new Course { Code = "C1", Name = "First" });
            await context.SaveChangesAsync();

            context.Courses.AddIfNotExists(c => c.Code, new Course { Code = "C1", Name = "Duplicate" });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var courses = context.Courses.ToList();
            Assert.That(courses, Has.Count.EqualTo(1));
            Assert.That(courses[0].Name, Is.EqualTo("First"));
        }
    }
}

