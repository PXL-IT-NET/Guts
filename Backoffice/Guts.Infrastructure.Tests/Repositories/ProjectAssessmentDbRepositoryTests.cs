using System.Linq;
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

    [Test]
    public async Task GetOpenAssessmentsAsync_ShouldReturnOnlyAssessmentsWhereNowIsBetweenOpenAndDeadline()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync();
        var project = await Seeder.SeedProjectAsync(course.Id, period.Id);

        var baselineUtc = System.DateTime.UtcNow;

        // Past assessment relative to executionTime, but future relative to baselineUtc
        var pastAssessment = new Domain.TopicAggregate.ProjectAggregate.ProjectAssessment.Factory()
            .CreateNew(project.Id, "Past Assessment", baselineUtc.AddHours(1), baselineUtc.AddHours(2));

        // Open assessment relative to executionTime
        var openAssessment = new Domain.TopicAggregate.ProjectAggregate.ProjectAssessment.Factory()
            .CreateNew(project.Id, "Open Assessment", baselineUtc.AddHours(2), baselineUtc.AddHours(4));

        // Future assessment relative to executionTime
        var futureAssessment = new Domain.TopicAggregate.ProjectAggregate.ProjectAssessment.Factory()
            .CreateNew(project.Id, "Future Assessment", baselineUtc.AddHours(4), baselineUtc.AddHours(6));

        await using (var seedContext = CreateDbContext())
        {
            seedContext.Set<Domain.TopicAggregate.ProjectAggregate.ProjectAssessment>().AddRange(
                (Domain.TopicAggregate.ProjectAggregate.ProjectAssessment)pastAssessment,
                (Domain.TopicAggregate.ProjectAggregate.ProjectAssessment)openAssessment,
                (Domain.TopicAggregate.ProjectAggregate.ProjectAssessment)futureAssessment);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repo = new ProjectAssessmentDbRepository(context);

        var executionTime = baselineUtc.AddHours(3);
        var openAssessments = await repo.GetOpenAssessmentsAsync(executionTime);
        Assert.That(openAssessments.Any(a => a.Description == "Open Assessment"), Is.True);
        Assert.That(openAssessments.Any(a => a.Description == "Future Assessment"), Is.False);
        Assert.That(openAssessments.Any(a => a.Description == "Past Assessment"), Is.False);
    }
}

