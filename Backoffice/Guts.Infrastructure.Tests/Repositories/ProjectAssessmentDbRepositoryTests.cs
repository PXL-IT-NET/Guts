using System.Threading.Tasks;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ProjectAssessmentDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task FindByProjectIdAsync_ShouldReturnAssessmentsOfProject()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var project = await Seeder.SeedProjectAsync(course.Id, period.Id);
        var assessment = await Seeder.SeedProjectAssessmentAsync(project.Id, "Sprint 1 Assessment");

        await using var context = CreateDbContext();
        var repo = new ProjectAssessmentDbRepository(context);

        var assessments = await repo.FindByProjectIdAsync(project.Id);
        Assert.That(assessments, Has.Count.EqualTo(1));
        Assert.That(assessments[0].Id, Is.EqualTo(assessment.Id));
        Assert.That(assessments[0].Description, Is.EqualTo("Sprint 1 Assessment"));
    }
}

