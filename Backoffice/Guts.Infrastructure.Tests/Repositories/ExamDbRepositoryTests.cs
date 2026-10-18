using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ExamDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task LoadDeepAsync_And_FindWithPartsAndEvaluationsAsync_ShouldLoadHierarchy()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("EX_C");
        var chapter = await Seeder.SeedChapterAsync(course.Id, period.Id, "Ch1", "Chapter 1", "Ex1", "T1", "T2");
        var (exam, _) = await Seeder.SeedExamWithPartAndEvaluationAsync(course.Id, period.Id, chapter.Assignments.First());

        await using var context = CreateDbContext();
        var repo = new ExamDbRepository(context);

        var deepExam = await repo.LoadDeepAsync(exam.Id);
        Assert.That(deepExam.Name, Is.EqualTo("Midterm Exam"));
        Assert.That(deepExam.Parts, Has.Count.EqualTo(1));
        var part = deepExam.Parts.First();
        Assert.That(part.AssignmentEvaluations, Has.Count.EqualTo(1));
        var eval = part.AssignmentEvaluations.First();
        Assert.That(eval.Assignment.Tests, Has.Count.EqualTo(2));
        Assert.That(eval.Assignment.Topic, Is.Not.Null);

        Assert.That(async () => await repo.LoadDeepAsync(99999), Throws.InstanceOf<DataNotFoundException>());

        var foundList = await repo.FindWithPartsAndEvaluationsAsync(course.Id, period.Id);
        Assert.That(foundList, Has.Count.EqualTo(1));
        Assert.That(foundList[0].Parts.First().AssignmentEvaluations, Has.Count.EqualTo(1));
    }
}

