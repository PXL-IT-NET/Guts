using System;
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
        var otherCourse = await Seeder.SeedCourseAsync("KEEP1", "Keep Course");
        var user = await Seeder.SeedUserAsync("Jane", "Doe");

        await using (var seedContext = CreateDbContext())
        {
            var classGroup = new CourseClassGroup("1TIN-B", course.Id, period.Id);
            var otherClassGroup = new CourseClassGroup("1TIN-B", otherCourse.Id, period.Id);
            seedContext.CourseClassGroups.AddRange(classGroup, otherClassGroup);
            seedContext.CourseGamificationConfigs.AddRange(
                new CourseGamificationConfig(course.Id),
                new CourseGamificationConfig(otherCourse.Id));
            await seedContext.SaveChangesAsync();

            var profile = new StudentCourseProfile(user.Id, course.Id, period.Id);
            profile.Enroll(true, "Hero2", "Mage", classGroup.Id, false);
            profile.AwardPassedTestXp(1, DateTime.UtcNow);

            var otherProfile = new StudentCourseProfile(user.Id, otherCourse.Id, period.Id);
            otherProfile.Enroll(true, "Hero2", "Mage", otherClassGroup.Id, false);

            seedContext.StudentCourseProfiles.AddRange(profile, otherProfile);
            await seedContext.SaveChangesAsync();
        }

        await Seeder.SeedChapterAsync(course.Id, period.Id, "Ch01", "Course Chapter");

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
            Assert.That(verifyContext.CourseGamificationConfigs.Any(gc => gc.CourseId == course.Id), Is.False);
            Assert.That(verifyContext.Topics.Any(t => t.CourseId == course.Id), Is.False);

            Assert.That(verifyContext.Courses.Any(c => c.Id == otherCourse.Id), Is.True);
            Assert.That(verifyContext.StudentCourseProfiles.Any(sp => sp.CourseId == otherCourse.Id), Is.True);
            Assert.That(verifyContext.CourseClassGroups.Any(cg => cg.CourseId == otherCourse.Id), Is.True);
            Assert.That(verifyContext.CourseGamificationConfigs.Any(gc => gc.CourseId == otherCourse.Id), Is.True);
        }
    }
}
