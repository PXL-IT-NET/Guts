using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ExamPartDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task LoadWithAssignmentEvaluationsAsync_And_DeleteAsync_ShouldWork()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("EX_C");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id, "Ch1", "Chapter 1", "Ex1", "T1", "T2");
        var (_, examPart) = await Seeder.SeedExamWithPartAndEvaluationAsync(course.Id, period.Id, chapter.Assignments.First());

        await using (var context = CreateDbContext())
        {
            var repo = new ExamPartDbRepository(context);

            var part = await repo.LoadWithAssignmentEvaluationsAsync(examPart.Id);
            Assert.That(part.AssignmentEvaluations, Has.Count.EqualTo(1));

            Assert.That(async () => await repo.LoadWithAssignmentEvaluationsAsync(99999),
                Throws.InstanceOf<DataNotFoundException>());

            await repo.DeleteAsync(part);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new ExamPartDbRepository(context);
            Assert.That(async () => await repo.LoadWithAssignmentEvaluationsAsync(examPart.Id),
                Throws.InstanceOf<DataNotFoundException>());
        }
    }
}

