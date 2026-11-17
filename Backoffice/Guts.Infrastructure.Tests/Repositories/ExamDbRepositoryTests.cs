using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Domain.ExamAggregate;
using Guts.Infrastructure.Repositories;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests.Repositories;

[TestFixture]
internal class ExamDbRepositoryTests : RepositoryTestsBase
{
    [Test]
    public async Task AddGetUpdateDelete_And_GetByCourseAndPeriodAsync_ManageFullExamGraph()
    {
        var (course, period) = await Seeder.SeedCourseAndActivePeriodAsync("EXAM1");
        var lector = await Seeder.SeedUserAsync("Bob", "Lector", "bob@pxl.be");

        var factory = new ExamFactory();
        var now = DateTime.UtcNow;
        var exam = factory.CreateNew(
            course.Id,
            period.Id,
            lector.Id,
            new ExamMetadata("Midterm", "# Read me", "CODE-99", ExamTemplateCatalog.DotNet10ClassLibNUnit),
            now);

        var q2 = exam.AddQuestion("Q2", "Second", 2, ExamTemplateCatalog.DotNet10ClassLibNUnit);
        q2.UpsertFile(new ExamFilePath("Program.cs"), "class Program {}", ExamFileRole.Editable);

        var q1 = exam.AddQuestion("Q1", "First", 1, ExamTemplateCatalog.DotNet10Console);
        q1.UpsertFile(new ExamFilePath("Solver.cs"), "class Solver {}", ExamFileRole.Editable);
        q1.UpsertFile(new ExamFilePath("SolverTests.cs"), "class SolverTests {}", ExamFileRole.PublicTest);

        int examId;
        await using (var context = CreateDbContext())
        {
            var repo = new ExamDbRepository(context);
            var added = await repo.AddAsync(exam);
            examId = added.Id;
            Assert.That(examId, Is.GreaterThan(0));
        }

        await using (var context = CreateDbContext())
        {
            var repo = new ExamDbRepository(context);
            var loaded = await repo.GetByIdWithQuestionsAsync(examId);

            Assert.That(loaded.Title, Is.EqualTo("Midterm"));
            Assert.That(loaded.AccessCode.Value, Is.EqualTo("CODE-99"));
            Assert.That(loaded.Questions.Select(q => q.OrderIndex), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(loaded.Questions.First().Files, Has.Count.EqualTo(2));

            loaded.Open(DateTime.UtcNow);
            await repo.UpdateAsync(loaded);

            var list = await repo.GetByCourseAndPeriodAsync(course.Id, period.Id);
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list[0].Status, Is.EqualTo(ExamStatus.Open));

            await repo.DeleteAsync(loaded);
        }

        await using (var context = CreateDbContext())
        {
            var repo = new ExamDbRepository(context);
            await Assert.ThatAsync(
                () => repo.GetByIdWithQuestionsAsync(examId),
                Throws.InstanceOf<DataNotFoundException>());
        }
    }
}
