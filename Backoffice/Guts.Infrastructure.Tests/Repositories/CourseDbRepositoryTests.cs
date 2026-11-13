using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.GamificationAggregate.Configuration;
using Guts.Domain.GamificationAggregate.Profiles;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class CourseDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task DeleteAsync_ShouldCascadeToStudentCourseProfilesAndClassGroups()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("DEL1");
        var user = await Seeder.SeedUserAsync("Jane", "Doe");

        await using (var seedContext = CreateDbContext())
        {
            var classGroup = new CourseClassGroup("1TIN-B", course.Id, period.Id);
            seedContext.CourseClassGroups.Add(classGroup);
            await seedContext.SaveChangesAsync();

            var profile = new StudentCourseProfile(user.Id, course.Id, period.Id);
            profile.Enroll(true, "Hero2", "Mage", classGroup.Id, false);
            seedContext.StudentCourseProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var repo = new CourseDbRepository(context);
            var existing = await repo.GetByIdAsync(course.Id);
            await repo.DeleteAsync(existing);
        }

        await using (var verifyContext = CreateDbContext())
        {
            Assert.That(verifyContext.Courses.Any(c => c.Id == course.Id), Is.False);
            Assert.That(verifyContext.StudentCourseProfiles.Any(sp => sp.CourseId == course.Id), Is.False);
            Assert.That(verifyContext.CourseClassGroups.Any(cg => cg.CourseId == course.Id), Is.False);
        }
    }
}
